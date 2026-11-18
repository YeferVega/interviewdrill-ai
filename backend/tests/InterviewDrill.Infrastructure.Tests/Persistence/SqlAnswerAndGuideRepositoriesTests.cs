using System.Text.Json;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Persistence;
using InterviewDrill.Infrastructure.Persistence;
using InterviewDrill.Infrastructure.Persistence.Entities;
using InterviewDrill.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InterviewDrill.Infrastructure.Tests.Persistence;

public sealed class SqlAnswerAndGuideRepositoriesTests : IDisposable
{
    private readonly InterviewDrillDbContext _context;

    public SqlAnswerAndGuideRepositoriesTests()
    {
        var options = new DbContextOptionsBuilder<InterviewDrillDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new InterviewDrillDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    private void SeedAggregate(string sessionId, string questionId, string userId = "u1")
    {
        // Ensure only one UserEntity/JobDescriptionEntity per user is created across seeds.
        var user = _context.Users.Local.SingleOrDefault(u => u.Id == userId)
                   ?? new UserEntity { Id = userId, BrowserToken = "t" };

        var jd = _context.JobDescriptions.Local.SingleOrDefault(j => j.UserId == userId && j.RawTextHash == "jd-hash")
                 ?? new JobDescriptionEntity { UserId = userId, RawTextHash = "jd-hash", ParsedProfileJson = "{}" };

        if (_context.Users.Local.SingleOrDefault(u => u.Id == userId) is null)
        {
            _context.Users.Add(user);
        }

        if (_context.JobDescriptions.Local.SingleOrDefault(j => j.Id == jd.Id) is null)
        {
            _context.JobDescriptions.Add(jd);
        }

        var session = new InterviewSessionEntity { Id = sessionId, UserId = userId, JobDescriptionId = jd.Id };
        var question = new QuestionEntity
        {
            Id = questionId,
            SessionId = sessionId,
            UserId = userId,
            JobDescriptionHash = "jd-hash",
            NormalizedQuestionHash = "nq-" + questionId,
            Text = "¿Qué es RAG?",
            SkillTarget = "RAG"
        };

        _context.InterviewSessions.Add(session);
        _context.Questions.Add(question);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AddAnswerAsync_UnknownQuestion_ThrowsInvalidOperation()
    {
        var repo = new SqlInterviewAnswerRepository(_context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.AddAnswerAsync("s1", "no-existe", "resp", Payload(80)));
    }

    [Fact]
    public async Task AddAnswerAsync_StoresSerializedPayload()
    {
        SeedAggregate("s1", "q1");
        var repo = new SqlInterviewAnswerRepository(_context);

        await repo.AddAnswerAsync("s1", "q1", "respuesta limpia", Payload(80));
        await repo.SaveChangesAsync();

        var answer = await _context.Answers.AsNoTracking().SingleAsync();
        Assert.Equal("respuesta limpia", answer.Text);
        using var doc = JsonDocument.Parse(answer.EvaluationPayloadJson);
        Assert.Equal(80, doc.RootElement.GetProperty("overallScore").GetDouble());
    }

    [Fact]
    public async Task GetSessionEvaluationsAsync_ReturnsOnlyCurrentSessionPayloads()
    {
        SeedAggregate("s1", "q1");
        SeedAggregate("s2", "q2");
        var repo = new SqlInterviewAnswerRepository(_context);

        await repo.AddAnswerAsync("s1", "q1", "r1", Payload(70));
        await repo.AddAnswerAsync("s1", "q1", "r1b", Payload(85));
        await repo.AddAnswerAsync("s2", "q2", "r2", Payload(50));
        await repo.SaveChangesAsync();

        var s1 = await repo.GetSessionEvaluationsAsync("s1");
        var s2 = await repo.GetSessionEvaluationsAsync("s2");

        Assert.Equal(2, s1.Count);
        Assert.All(s1, e => Assert.Contains(e.OverallScore, new[] { 70.0, 85.0 }));
        Assert.Single(s2);
        Assert.Equal(50, s2[0].OverallScore);
    }

    [Fact]
    public async Task GetSessionEvaluationsAsync_SkipsInvalidPayloads()
    {
        SeedAggregate("s1", "q1");

        _context.Answers.Add(new AnswerEntity
        {
            QuestionId = "q1",
            Text = "r",
            EvaluationPayloadJson = "{ esto no es json valido"
        });
        await _context.SaveChangesAsync();

        var repo = new SqlInterviewAnswerRepository(_context);
        var evaluations = await repo.GetSessionEvaluationsAsync("s1");

        Assert.Empty(evaluations);
    }

    [Fact]
    public async Task SqlStudyGuideRepository_SavesSanitizedAndUpdatesExisting()
    {
        SeedAggregate("s1", "q1");
        var repo = new SqlStudyGuideRepository(_context, new RegexPiiSanitizer());

        await repo.SaveAsync("s1", "Email alice@example.com es sensible");
        await repo.SaveChangesAsync();

        var first = await repo.GetBySessionAsync("s1");
        Assert.NotNull(first);
        Assert.Contains("[EMAIL]", first);
        Assert.DoesNotContain("alice@example.com", first);

        var savedId = (await _context.StudyGuides.SingleAsync()).Id;

        await repo.SaveAsync("s1", "Sin PII esta vez");
        await repo.SaveChangesAsync();

        var second = await repo.GetBySessionAsync("s1");
        var nowId = (await _context.StudyGuides.SingleAsync()).Id;

        Assert.Equal("Sin PII esta vez", second);
        Assert.Equal(savedId, nowId);
    }

    [Fact]
    public async Task SqlStudyGuideRepository_GetBySession_UnknownSession_ReturnsNull()
    {
        var repo = new SqlStudyGuideRepository(_context, new RegexPiiSanitizer());

        Assert.Null(await repo.GetBySessionAsync("no-existe"));
    }

    [Fact]
    public async Task SqlSessionHistoryRepository_GetByUser_MapsStatusesAndScores()
    {
        SeedAggregate("s1", "q1");
        SeedAggregate("s2", "q2");

        _context.Answers.Add(new AnswerEntity
        {
            QuestionId = "q1",
            Text = "r",
            EvaluationPayloadJson = """{"overallScore":72}"""
        });
        _context.Answers.Add(new AnswerEntity
        {
            QuestionId = "q2",
            Text = "r",
            EvaluationPayloadJson = "{ invalid json }"
        });

        var session2 = await _context.InterviewSessions.SingleAsync(s => s.Id == "s2");
        session2.Status = InterviewSessionStatus.Completed;
        session2.EndedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

        _context.StudyGuides.Add(new StudyGuideEntity { SessionId = "s2", Markdown = "guía" });

        await _context.SaveChangesAsync();

        var repo = new SqlSessionHistoryRepository(_context);
        var history = await repo.GetByUserAsync("u1");

        var s1 = history.Single(h => h.SessionId == "s1");
        var s2 = history.Single(h => h.SessionId == "s2");

        Assert.Equal(InterviewSessionRecordStatus.InProgress, s1.Status);
        Assert.Single(s1.EvaluationScores);
        Assert.Equal(72, s1.EvaluationScores[0]);
        Assert.Null(s1.StudyGuideId);

        Assert.Equal(InterviewSessionRecordStatus.Completed, s2.Status);
        Assert.Empty(s2.EvaluationScores); // payload inválido -> se ignora
        Assert.NotNull(s2.StudyGuideId);
    }

    [Fact]
    public async Task SqlSessionHistoryRepository_GetByUser_UnknownUser_ReturnsEmpty()
    {
        var repo = new SqlSessionHistoryRepository(_context);

        Assert.Empty(await repo.GetByUserAsync("ghost"));
    }

    private static EvaluationPayload Payload(double score) => new()
    {
        OverallScore = score,
        TechnicalDepth = score,
        ProductionReadiness = score,
        Verdict = Verdict.Pass,
        CoveredKeywords = ["RAG"],
        MissedConcepts = [],
        Strengths = ["claridad"],
        ImprovementAdvice = "n/a"
    };
}
