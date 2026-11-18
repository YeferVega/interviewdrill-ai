using InterviewDrill.Core.Persistence;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed class SessionCheckpointService(ISessionRepository repository) : ISessionCheckpointService
{
    public async Task<SessionCheckpoint?> LoadCheckpointAsync(string sessionId, CancellationToken ct = default)
    {
        var state = await repository.GetStateAsync(sessionId, ct);

        if (state is null)
        {
            return null;
        }

        return new SessionCheckpoint(
            state.Id,
            state.StartedAt,
            state.Status.ToString(),
            state.EndedAt,
            state.QuestionCount,
            state.AnsweredQuestionCount,
            state.AnsweredQuestionIds,
            state.LastQuestionId,
            state.LastQuestionText,
            state.LastQuestionDifficulty,
            state.LastSkillTarget,
            state.LastAskedAt);
    }

    public async Task<int> CompleteSessionAsync(string sessionId, DateTimeOffset endedAt, CancellationToken ct = default)
    {
        return await repository.SetCompletedAsync(sessionId, endedAt, ct);
    }
}
