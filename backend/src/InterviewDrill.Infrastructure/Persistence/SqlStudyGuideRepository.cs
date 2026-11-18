using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Core.Security;
using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InterviewDrill.Infrastructure.Persistence;

/// <summary>
/// Repositorio SQL para guías de estudio.
/// Sanitiza PII antes de persistir el Markdown generado.
/// </summary>
public sealed class SqlStudyGuideRepository(InterviewDrillDbContext context, IPiiSanitizer piiSanitizer) : IStudyGuideRepository
{
    public async Task SaveAsync(string sessionId, string markdown, CancellationToken ct = default)
    {
        var sanitized = piiSanitizer.Sanitize(markdown);

        var guide = await context.StudyGuides.SingleOrDefaultAsync(g => g.SessionId == sessionId, ct);

        if (guide is null)
        {
            context.StudyGuides.Add(new StudyGuideEntity
            {
                SessionId = sessionId,
                Markdown = sanitized.Text
            });
            return;
        }

        guide.Markdown = sanitized.Text;
    }

    public async Task<string?> GetBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        return await context.StudyGuides
            .AsNoTracking()
            .Where(g => g.SessionId == sessionId)
            .Select(g => g.Markdown)
            .SingleOrDefaultAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return context.SaveChangesAsync(ct);
    }
}
