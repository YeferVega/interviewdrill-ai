using System.Text.Json;
using InterviewDrill.Core.Persistence;
using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed class SqlSessionHistoryRepository(InterviewDrillDbContext context) : ISessionHistoryRepository
{
    public async Task<IReadOnlyList<SessionHistorySourceRecord>> GetByUserAsync(
        string userId,
        CancellationToken ct = default)
    {
        var sessions = await context.InterviewSessions
            .AsNoTracking()
            .Include(s => s.Questions)
                .ThenInclude(q => q.Answers)
            .Include(s => s.StudyGuide)
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        return sessions
            .Select(session => new SessionHistorySourceRecord(
                session.Id,
                session.StartedAt,
                session.EndedAt,
                ToHistoryStatus(session.Status),
                ExtractScores(session),
                session.StudyGuide?.Id,
                session.StudyGuide?.CreatedAt))
            .ToList();
    }

    private static InterviewSessionRecordStatus ToHistoryStatus(InterviewSessionStatus status) => status switch
    {
        InterviewSessionStatus.Completed => InterviewSessionRecordStatus.Completed,
        InterviewSessionStatus.Aborted => InterviewSessionRecordStatus.Aborted,
        _ => InterviewSessionRecordStatus.InProgress
    };

    private static IReadOnlyList<double> ExtractScores(InterviewSessionEntity session)
    {
        var scores = new List<double>();

        foreach (var answer in session.Questions.SelectMany(q => q.Answers))
        {
            try
            {
                using var document = JsonDocument.Parse(answer.EvaluationPayloadJson);

                if (document.RootElement.TryGetProperty("overallScore", out var overallScore) &&
                    overallScore.ValueKind == JsonValueKind.Number)
                {
                    scores.Add(overallScore.GetDouble());
                }
            }
            catch (JsonException)
            {
                // Invalid evaluation payloads are skipped from session history scoring.
                // Schema validation should reject these earlier in the evaluation pipeline.
            }
        }

        return scores;
    }
}
