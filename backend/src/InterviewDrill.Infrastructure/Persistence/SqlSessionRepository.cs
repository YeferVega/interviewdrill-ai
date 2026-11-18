using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed class SqlSessionRepository(InterviewDrillDbContext context) : ISessionRepository
{
    public async Task<SessionStateRecord?> GetStateAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await context.InterviewSessions
            .AsNoTracking()
            .Include(s => s.Questions)
            .ThenInclude(q => q.Answers)
            .SingleOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
        {
            return null;
        }

        var questions = session.Questions
            .OrderBy(q => q.AskedAt)
            .ToList();

        var lastQuestion = questions.LastOrDefault();

        return new SessionStateRecord(
            session.Id,
            session.StartedAt,
            session.Status,
            session.EndedAt,
            lastQuestion?.AskedAt,
            lastQuestion?.Id,
            lastQuestion?.Text,
            lastQuestion?.Difficulty,
            lastQuestion?.SkillTarget,
            questions.Count,
            questions.Count(q => q.Answers.Count > 0),
            questions.Where(q => q.Answers.Count > 0).Select(q => q.Id).ToList());
    }

    public async Task<int> SetCompletedAsync(string sessionId, DateTimeOffset endedAt, CancellationToken ct = default)
    {
        var session = await context.InterviewSessions
            .SingleOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
        {
            return 0;
        }

        session.Status = InterviewSessionStatus.Completed;
        session.EndedAt ??= endedAt;

        return await context.SaveChangesAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return context.SaveChangesAsync(ct);
    }
}
