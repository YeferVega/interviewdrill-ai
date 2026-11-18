using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Persistence;
using InterviewDrill.Infrastructure.Persistence;
using InterviewDrill.Infrastructure.Persistence.Entities;

namespace InterviewDrill.Infrastructure.Tests.Persistence;

public sealed class SessionCheckpointServiceTests
{
    [Fact]
    public async Task LoadCheckpointAsync_ReturnsNullForUnknownSession()
    {
        var repository = new FakeSessionRepository();
        var service = new SessionCheckpointService(repository);

        var checkpoint = await service.LoadCheckpointAsync("missing");

        Assert.Null(checkpoint);
    }

    [Fact]
    public async Task LoadCheckpointAsync_ReturnsLastQuestionForResume()
    {
        var repository = new FakeSessionRepository
        {
            State = new SessionStateRecord(
                "session-1",
                DateTimeOffset.UtcNow.AddMinutes(-10),
                InterviewSessionStatus.InProgress,
                null,
                DateTimeOffset.UtcNow.AddMinutes(-2),
                "question-2",
                "¿Cómo reduces latencia P95?",
                Difficulty.Hard,
                "Observability",
                QuestionCount: 2,
                AnsweredQuestionCount: 1,
                AnsweredQuestionIds: ["question-1"])
        };

        var service = new SessionCheckpointService(repository);
        var checkpoint = await service.LoadCheckpointAsync("session-1");

        Assert.NotNull(checkpoint);
        Assert.Equal("session-1", checkpoint.SessionId);
        Assert.Equal("InProgress", checkpoint.Status);
        Assert.Equal("question-2", checkpoint.LastQuestionId);
        Assert.Equal("¿Cómo reduces latencia P95?", checkpoint.LastQuestionText);
        Assert.Equal(Difficulty.Hard, checkpoint.LastQuestionDifficulty);
        Assert.Single(checkpoint.AnsweredQuestionIds);
    }

    [Fact]
    public async Task CompleteSessionAsync_DelegatesToRepository()
    {
        var repository = new FakeSessionRepository();
        var service = new SessionCheckpointService(repository);

        var count = await service.CompleteSessionAsync("session-1", DateTimeOffset.UtcNow);

        Assert.Equal(1, count);
        Assert.Equal("session-1", repository.CompletedSessionId);
    }

    private sealed class FakeSessionRepository : ISessionRepository
    {
        public SessionStateRecord? State { get; init; }

        public string? CompletedSessionId { get; private set; }

        public Task<SessionStateRecord?> GetStateAsync(string sessionId, CancellationToken ct = default)
        {
            return Task.FromResult(sessionId == State?.Id ? State : null);
        }

        public Task<int> SetCompletedAsync(string sessionId, DateTimeOffset endedAt, CancellationToken ct = default)
        {
            CompletedSessionId = sessionId;
            return Task.FromResult(1);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return Task.FromResult(0);
        }
    }
}
