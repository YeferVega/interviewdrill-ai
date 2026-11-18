using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Core.Persistence;

public sealed record InterviewSessionSummary(
    string SessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string Status,
    double OverallScore,
    Verdict OverallVerdict,
    string? StudyGuideId,
    DateTimeOffset? StudyGuideCreatedAt);

public interface ISessionHistoryService
{
    Task<IReadOnlyList<InterviewSessionSummary>> GetHistoryAsync(
        string userId,
        CancellationToken ct = default);
}
