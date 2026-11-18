using InterviewDrill.Core.Persistence;
using InterviewDrill.Core.Scoring;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed class SessionHistoryService(
    ISessionHistoryRepository repository,
    VerdictDeterminer? verdictDeterminer = null) : ISessionHistoryService
{
    private readonly VerdictDeterminer verdictDeterminer = verdictDeterminer ?? new VerdictDeterminer();

    public async Task<IReadOnlyList<InterviewSessionSummary>> GetHistoryAsync(
        string userId,
        CancellationToken ct = default)
    {
        var sessions = await repository.GetByUserAsync(userId, ct);

        return sessions
            .OrderByDescending(session => session.StartedAt)
            .Select(Map)
            .ToList();
    }

    private InterviewSessionSummary Map(SessionHistorySourceRecord session)
    {
        var overallScore = session.EvaluationScores.Count == 0
            ? 0.0
            : Math.Round(session.EvaluationScores.Average(), 2);

        return new InterviewSessionSummary(
            session.SessionId,
            session.StartedAt,
            session.EndedAt,
            session.Status.ToString(),
            overallScore,
            verdictDeterminer.Determine(overallScore),
            session.StudyGuideId,
            session.StudyGuideCreatedAt);
    }
}
