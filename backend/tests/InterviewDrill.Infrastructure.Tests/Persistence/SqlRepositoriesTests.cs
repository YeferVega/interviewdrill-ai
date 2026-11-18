using InterviewDrill.Core.Security;
using InterviewDrill.Infrastructure.Persistence;
using InterviewDrill.Infrastructure.Persistence.Entities;
using InterviewDrill.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InterviewDrill.Infrastructure.Tests.Persistence;

public sealed class SqlRepositoriesTests : IDisposable
{
    private readonly InterviewDrillDbContext _context;

    public SqlRepositoriesTests()
    {
        var options = new DbContextOptionsBuilder<InterviewDrillDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new InterviewDrillDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task SqlQuestionRepository_ExistsNormalizedHash_FindsByUserJdHash()
    {
        var repo = new SqlQuestionRepository(_context);

        _context.Questions.Add(new QuestionEntity
        {
            SessionId = "s1",
            UserId = "u1",
            JobDescriptionHash = "jd",
            NormalizedQuestionHash = "abc",
            Text = "¿Qué es RAG?",
            SkillTarget = "RAG"
        });
        await _context.SaveChangesAsync();

        Assert.True(await repo.ExistsNormalizedHashAsync("u1", "jd", "abc"));
        Assert.False(await repo.ExistsNormalizedHashAsync("u1", "jd", "no-existe"));
        Assert.False(await repo.ExistsNormalizedHashAsync("u2", "jd", "abc"));
    }

    [Fact]
    public async Task SqlQuestionRepository_ExistsSemanticDuplicate_EmptyEmbedding_Throws()
    {
        var repo = new SqlQuestionRepository(_context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => repo.ExistsSemanticDuplicateAsync("u1", "jd", Array.Empty<float>()));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.5)]
    public async Task SqlQuestionRepository_ExistsSemanticDuplicate_InvalidThreshold_Throws(double threshold)
    {
        var repo = new SqlQuestionRepository(_context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repo.ExistsSemanticDuplicateAsync("u1", "jd", new float[] { 1, 0 }, threshold));
    }

    [Fact]
    public async Task SqlQuestionRepository_ExistsSemanticDuplicate_MatchesStoredEmbedding()
    {
        var repo = new SqlQuestionRepository(_context);

        _context.Questions.Add(new QuestionEntity
        {
            SessionId = "s1",
            UserId = "u1",
            JobDescriptionHash = "jd",
            NormalizedQuestionHash = "h1",
            Text = "¿Cómo escalar HNSW?",
            SkillTarget = "HNSW",
            Embedding = ToBytes(new float[] { 1, 0, 0, 0 })
        });
        await _context.SaveChangesAsync();

        // vector idéntico → similitud coseno 1 >= 0.92 → es duplicado semántico
        Assert.True(await repo.ExistsSemanticDuplicateAsync("u1", "jd", new float[] { 1, 0, 0, 0 }));
        // vector ortogonal → similitud 0 → no duplicado
        Assert.False(await repo.ExistsSemanticDuplicateAsync("u1", "jd", new float[] { 0, 1, 0, 0 }));
        // otro usuario no ve los embeddings
        Assert.False(await repo.ExistsSemanticDuplicateAsync("u2", "jd", new float[] { 1, 0, 0, 0 }));
    }

    [Fact]
    public async Task SqlQuestionRepository_AddAsync_PersistsQuestionEntity()
    {
        var repo = new SqlQuestionRepository(_context);
        var q = new QuestionEntity
        {
            SessionId = "s9",
            UserId = "u9",
            JobDescriptionHash = "jdx",
            NormalizedQuestionHash = "hh",
            Text = "Pregunta",
            SkillTarget = "Kafka"
        };

        await repo.AddAsync(q);
        var changes = await repo.SaveChangesAsync();

        Assert.True(changes >= 0);
        Assert.Contains(_context.Questions.AsNoTracking(), x => x.Id == q.Id);
    }

    [Fact]
    public async Task EfInterviewerSessionStore_SaveAsync_CreatesAggregateUserJdAndSession()
    {
        var store = new EfInterviewerSessionStore(_context, new RegexPiiSanitizer());
        var started = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var state = new InterviewDrill.Core.InterviewFlow.InterviewerSessionState(
            "s1", "u1", "jd-hash",
            Profile: new InterviewDrill.Core.Contracts.JobProfile
            {
                Role = "Senior Backend",
                Seniority = InterviewDrill.Core.Contracts.Seniority.Senior,
                RequiredSkills = [new InterviewDrill.Core.Contracts.WeightedSkill { Name = "Kafka", Weight = 0.9 }],
                RawTextHash = "jd-hash"
            },
            CurrentDifficulty: InterviewDrill.Core.Contracts.Difficulty.Hard,
            StartedAt: started,
            LastQuestionId: null,
            LastSkillTarget: null,
            LastQuestionText: null,
            AskedSkillsBySkill: new Dictionary<string, int>(),
            FailuresBySkill: new Dictionary<string, int>());

        await store.SaveAsync(state);

        Assert.NotNull(await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == "u1"));
        Assert.NotNull(await _context.JobDescriptions.AsNoTracking().SingleOrDefaultAsync(j => j.UserId == "u1" && j.RawTextHash == "jd-hash"));
        Assert.NotNull(await _context.InterviewSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == "s1"));
    }

    [Fact]
    public async Task EfInterviewerSessionStore_SaveAsync_UpdatesExistingProfileJson()
    {
        var store = new EfInterviewerSessionStore(_context, new RegexPiiSanitizer());
        var state = BuildState("s1", "u1", "hashA");

        await store.SaveAsync(state);

        var updated = state with { Profile = state.Profile with { Role = "Otro rol" } };
        await store.SaveAsync(updated);

        var jd = await _context.JobDescriptions.AsNoTracking().SingleAsync();
        Assert.Contains("Otro rol", jd.ParsedProfileJson);
    }

    [Fact]
    public async Task EfInterviewerSessionStore_SaveAsync_SanitizesPiiBeforePersisting()
    {
        var store = new EfInterviewerSessionStore(_context, new RegexPiiSanitizer());
        var state = BuildState("s1", "u1", "hashA") with
        {
            LastSkillTarget = "contacto alice@example.com",
            LastQuestionText = "¿Cómo manejar un PII de bob@corp.com?"
        };

        await store.SaveAsync(state);

        var session = await _context.InterviewSessions.AsNoTracking().SingleAsync(s => s.Id == "s1");
        Assert.NotNull(session.InterviewerStateJson);
        Assert.DoesNotContain("alice@example.com", session.InterviewerStateJson);
        // El LastSkillTarget debe ir enmascarado
        Assert.Contains("[EMAIL]", session.InterviewerStateJson);
    }

    [Fact]
    public async Task EfInterviewerSessionStore_LoadAsync_DeserializesPersistedState()
    {
        var store = new EfInterviewerSessionStore(_context, new RegexPiiSanitizer());
        var state = BuildState("s1", "u1", "hashA") with { LastSkillTarget = "RAG" };

        await store.SaveAsync(state);

        var loaded = await store.LoadAsync("s1");

        Assert.NotNull(loaded);
        Assert.Equal("s1", loaded!.SessionId);
        Assert.Equal("RAG", loaded.LastSkillTarget);
    }

    [Fact]
    public async Task EfInterviewerSessionStore_LoadAsync_UnknownSession_ReturnsNull()
    {
        var store = new EfInterviewerSessionStore(_context, new RegexPiiSanitizer());

        Assert.Null(await store.LoadAsync("no-existe"));
    }

    [Fact]
    public async Task EfInterviewerSessionStore_MarkCompletedAsync_SetsStatusAndEndedAt()
    {
        var store = new EfInterviewerSessionStore(_context, new RegexPiiSanitizer());
        var state = BuildState("s1", "u1", "hashA");
        await store.SaveAsync(state);

        var ended = state.StartedAt.AddMinutes(15);
        await store.MarkCompletedAsync("s1", ended);

        var session = await _context.InterviewSessions.AsNoTracking().SingleAsync(s => s.Id == "s1");
        Assert.Equal(InterviewSessionStatus.Completed, session.Status);
        Assert.Equal(ended, session.EndedAt);
    }

    [Fact]
    public async Task EfInterviewerSessionStore_MarkCompletedAsync_UnknownSession_Throws()
    {
        var store = new EfInterviewerSessionStore(_context, new RegexPiiSanitizer());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.MarkCompletedAsync("no-existe", DateTimeOffset.UtcNow));
    }

    private static InterviewDrill.Core.InterviewFlow.InterviewerSessionState BuildState(
        string sessionId, string userId, string jdHash) => new(
            sessionId, userId, jdHash,
            new InterviewDrill.Core.Contracts.JobProfile
            {
                Role = "Senior Backend",
                Seniority = InterviewDrill.Core.Contracts.Seniority.Senior,
                RequiredSkills = [new InterviewDrill.Core.Contracts.WeightedSkill { Name = "Kafka", Weight = 0.9 }],
                RawTextHash = jdHash
            },
            InterviewDrill.Core.Contracts.Difficulty.Hard,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            null, null, null,
            new Dictionary<string, int>(),
            new Dictionary<string, int>());

    private static byte[] ToBytes(float[] floats)
    {
        var bytes = new byte[floats.Length * 4];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
