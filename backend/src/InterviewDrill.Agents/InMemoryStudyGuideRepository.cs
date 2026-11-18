using System.Collections.Concurrent;
using InterviewDrill.Core.InterviewFlow;

namespace InterviewDrill.Agents;

public sealed class InMemoryStudyGuideRepository : IStudyGuideRepository
{
    private sealed record StoredGuide(string Id, string Markdown, DateTimeOffset CreatedAt);

    private readonly ConcurrentDictionary<string, StoredGuide> _guides = new(StringComparer.Ordinal);

    public Task SaveAsync(string sessionId, string markdown, CancellationToken ct = default)
    {
        if (!_guides.TryGetValue(sessionId, out var existing))
        {
            _guides[sessionId] = new StoredGuide(Guid.NewGuid().ToString(), markdown, DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }

        _guides[sessionId] = existing with { Markdown = markdown };
        return Task.CompletedTask;
    }

    public Task<string?> GetBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        _guides.TryGetValue(sessionId, out var guide);
        return Task.FromResult(guide?.Markdown);
    }

    public (string Id, DateTimeOffset CreatedAt)? GetInfo(string sessionId)
    {
        return _guides.TryGetValue(sessionId, out var guide)
            ? (guide.Id, guide.CreatedAt)
            : null;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}
