namespace InterviewDrill.Core.Persistence;

public enum InterviewSessionRecordStatus
{
    InProgress,
    Completed,
    Aborted
}

public sealed record SessionHistorySourceRecord(
    string SessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    InterviewSessionRecordStatus Status,
    IReadOnlyList<double> EvaluationScores,
    string? StudyGuideId,
    DateTimeOffset? StudyGuideCreatedAt);

public interface ISessionHistoryRepository
{
    Task<IReadOnlyList<SessionHistorySourceRecord>> GetByUserAsync(
        string userId,
        CancellationToken ct = default);
}
