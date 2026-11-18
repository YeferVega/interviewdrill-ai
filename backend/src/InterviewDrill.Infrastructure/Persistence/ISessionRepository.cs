using InterviewDrill.Core.Contracts;
using InterviewDrill.Infrastructure.Persistence.Entities;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed record SessionStateRecord(
    string Id,
    DateTimeOffset StartedAt,
    InterviewSessionStatus Status,
    DateTimeOffset? EndedAt,
    DateTimeOffset? LastAskedAt,
    string? LastQuestionId,
    string? LastQuestionText,
    Difficulty? LastQuestionDifficulty,
    string? LastSkillTarget,
    int QuestionCount,
    int AnsweredQuestionCount,
    IReadOnlyList<string> AnsweredQuestionIds);

public interface ISessionRepository
{
    Task<SessionStateRecord?> GetStateAsync(string sessionId, CancellationToken ct = default);

    Task<int> SetCompletedAsync(string sessionId, DateTimeOffset endedAt, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
