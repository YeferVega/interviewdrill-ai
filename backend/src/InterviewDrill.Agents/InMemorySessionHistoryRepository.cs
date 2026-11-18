using InterviewDrill.Core.Persistence;

namespace InterviewDrill.Agents;

public sealed class InMemorySessionHistoryRepository(
    InMemoryInterviewerSessionStore sessionStore,
    InMemoryInterviewAnswerRepository answers,
    InMemoryStudyGuideRepository studyGuides) : ISessionHistoryRepository
{
    public async Task<IReadOnlyList<SessionHistorySourceRecord>> GetByUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        var records = new List<SessionHistorySourceRecord>();

        foreach (var state in sessionStore.GetAllStates())
        {
            if (!string.Equals(state.UserId, userId, StringComparison.Ordinal))
            {
                continue;
            }

            var evaluations = await answers.GetSessionEvaluationsAsync(state.SessionId, ct);
            var guideInfo = studyGuides.GetInfo(state.SessionId);
            var endedAt = sessionStore.GetCompletedAt(state.SessionId);

            records.Add(new SessionHistorySourceRecord(
                state.SessionId,
                state.StartedAt,
                endedAt,
                endedAt.HasValue ? InterviewSessionRecordStatus.Completed : InterviewSessionRecordStatus.InProgress,
                evaluations.Select(e => e.OverallScore).ToList(),
                guideInfo?.Id,
                guideInfo?.CreatedAt));
        }

        return records;
    }
}
