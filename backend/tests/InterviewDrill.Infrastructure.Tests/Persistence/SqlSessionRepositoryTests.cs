using InterviewDrill.Core.Contracts;
using InterviewDrill.Infrastructure.Persistence;
using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InterviewDrill.Infrastructure.Tests.Persistence;

public sealed class SqlSessionRepositoryTests : IDisposable
{
    private readonly InterviewDrillDbContext _context;

    public SqlSessionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<InterviewDrillDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new InterviewDrillDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetStateAsync_UnknownSession_ReturnsNull()
    {
        var repo = new SqlSessionRepository(_context);

        Assert.Null(await repo.GetStateAsync("no-existe"));
    }

    [Fact]
    public async Task GetStateAsync_ComputesQuestionAndAnswerStats()
    {
        SeedAggregate("s1", new[] { "q1", "q2", "q3" });

        _context.Answers.Add(new AnswerEntity { QuestionId = "q1", Text = "r1", EvaluationPayloadJson = "{}" });
        _context.Answers.Add(new AnswerEntity { QuestionId = "q3", Text = "r3", EvaluationPayloadJson = "{}" });
        await _context.SaveChangesAsync();

        var repo = new SqlSessionRepository(_context);
        var state = await repo.GetStateAsync("s1");

        Assert.NotNull(state);
        Assert.Equal("s1", state!.Id);
        Assert.Equal(InterviewSessionStatus.InProgress, state.Status);
        Assert.Equal(3, state.QuestionCount);
        Assert.Equal(2, state.AnsweredQuestionCount);
        Assert.Equal("q3", state.LastQuestionId);
        Assert.Equal(Difficulty.Hard, state.LastQuestionDifficulty);
        Assert.Equal(["q1", "q3"], state.AnsweredQuestionIds.OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task SetCompletedAsync_SetsStatusAndEndedAt()
    {
        SeedAggregate("s1", new[] { "q1" });

        var repo = new SqlSessionRepository(_context);
        var ended = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

        var changed = await repo.SetCompletedAsync("s1", ended);
        var session = await _context.InterviewSessions.SingleAsync(s => s.Id == "s1");

        Assert.True(changed >= 0);
        Assert.Equal(InterviewSessionStatus.Completed, session.Status);
        Assert.Equal(ended, session.EndedAt);
    }

    [Fact]
    public async Task SetCompletedAsync_UnknownSession_ReturnsZero()
    {
        var repo = new SqlSessionRepository(_context);

        Assert.Equal(0, await repo.SetCompletedAsync("ghost", DateTimeOffset.UtcNow));
    }

    private void SeedAggregate(string sessionId, IEnumerable<string> questionIds)
    {
        var user = new UserEntity { Id = "u1", BrowserToken = "tok" };
        var jd = new JobDescriptionEntity { UserId = "u1", RawTextHash = "jd", ParsedProfileJson = "{}" };
        var session = new InterviewSessionEntity { Id = sessionId, UserId = "u1", JobDescriptionId = jd.Id };

        _context.Users.Add(user);
        _context.JobDescriptions.Add(jd);
        _context.InterviewSessions.Add(session);

        var index = 0;
        foreach (var qid in questionIds)
        {
            _context.Questions.Add(new QuestionEntity
            {
                Id = qid,
                SessionId = sessionId,
                UserId = "u1",
                JobDescriptionHash = "jd",
                NormalizedQuestionHash = "nq-" + qid,
                Text = "Pregunta " + (index + 1),
                Difficulty = Difficulty.Hard,
                SkillTarget = "Kafka",
                AskedAt = new DateTimeOffset(2026, 1, 1, 0, 0, index, TimeSpan.Zero)
            });
            index++;
        }

        _context.SaveChanges();
    }
}
