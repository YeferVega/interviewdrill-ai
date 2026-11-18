using System.Collections.Concurrent;
using InterviewDrill.Core.InterviewFlow;

namespace InterviewDrill.Agents;

public sealed class InMemoryInterviewerSessionStore : IInterviewerSessionStore
{
    private readonly ConcurrentDictionary<string, InterviewerSessionState> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _completed = new(StringComparer.Ordinal);

    public Task<InterviewerSessionState?> LoadAsync(string sessionId, CancellationToken ct = default)
    {
        _sessions.TryGetValue(sessionId, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(InterviewerSessionState state, CancellationToken ct = default)
    {
        _sessions[state.SessionId] = state;
        return Task.CompletedTask;
    }

    public Task MarkCompletedAsync(string sessionId, DateTimeOffset endedAt, CancellationToken ct = default)
    {
        _completed[sessionId] = endedAt;
        return Task.CompletedTask;
    }

    public DateTimeOffset? GetCompletedAt(string sessionId)
    {
        return _completed.TryGetValue(sessionId, out var value) ? value : null;
    }

    public IReadOnlyList<InterviewerSessionState> GetAllStates()
    {
        return _sessions.Values.ToList();
    }
}
