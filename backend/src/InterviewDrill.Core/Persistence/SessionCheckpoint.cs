using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Core.Persistence;

public sealed record SessionCheckpoint(
    string SessionId,
    DateTimeOffset StartedAt,
    string Status,
    DateTimeOffset? EndedAt,
    int QuestionCount,
    int AnsweredQuestionCount,
    IReadOnlyList<string> AnsweredQuestionIds,
    string? LastQuestionId,
    string? LastQuestionText,
    Difficulty? LastQuestionDifficulty,
    string? LastSkillTarget,
    DateTimeOffset? LastAskedAt);

public interface ISessionCheckpointService
{
    Task<SessionCheckpoint?> LoadCheckpointAsync(string sessionId, CancellationToken ct = default);

    Task<int> CompleteSessionAsync(string sessionId, DateTimeOffset endedAt, CancellationToken ct = default);
}
