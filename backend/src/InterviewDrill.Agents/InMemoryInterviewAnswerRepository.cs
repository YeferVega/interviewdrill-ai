using System.Collections.Concurrent;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;

namespace InterviewDrill.Agents;

public sealed class InMemoryInterviewAnswerRepository : IInterviewAnswerRepository
{
    private readonly ConcurrentBag<(string SessionId, string QuestionId, string AnswerText, EvaluationPayload Evaluation)> _answers = [];

    public Task AddAnswerAsync(
        string sessionId,
        string questionId,
        string sanitizedAnswerText,
        EvaluationPayload evaluation,
        CancellationToken ct = default)
    {
        _answers.Add((sessionId, questionId, sanitizedAnswerText, evaluation));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EvaluationPayload>> GetSessionEvaluationsAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        IReadOnlyList<EvaluationPayload> evaluations = _answers
            .Where(a => string.Equals(a.SessionId, sessionId, StringComparison.Ordinal))
            .Select(a => a.Evaluation)
            .ToList();

        return Task.FromResult(evaluations);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}
