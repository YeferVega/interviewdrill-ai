using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Core.InterviewFlow;

public sealed record QuestionGenerationRequest(
    string SessionId,
    JobProfile Profile,
    string SkillTarget,
    Difficulty Difficulty,
    QuestionType Type,
    string? LastQuestionText,
    int Attempt,
    string? Language = null);

public interface IInterviewerQuestionGenerator
{
    Task<InterviewQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken ct = default);
}

public sealed record InterviewerSessionState(
    string SessionId,
    string UserId,
    string JobDescriptionHash,
    JobProfile Profile,
    Difficulty CurrentDifficulty,
    DateTimeOffset StartedAt,
    string? LastQuestionId,
    string? LastSkillTarget,
    string? LastQuestionText,
    IReadOnlyDictionary<string, int> AskedSkillsBySkill,
    IReadOnlyDictionary<string, int> FailuresBySkill,
    string? Language = null);

public interface IInterviewerSessionStore
{
    Task<InterviewerSessionState?> LoadAsync(string sessionId, CancellationToken ct = default);
    Task SaveAsync(InterviewerSessionState state, CancellationToken ct = default);
    Task MarkCompletedAsync(string sessionId, DateTimeOffset endedAt, CancellationToken ct = default);
}

public interface IInterviewAnswerRepository
{
    Task AddAnswerAsync(
        string sessionId,
        string questionId,
        string sanitizedAnswerText,
        EvaluationPayload evaluation,
        CancellationToken ct = default);

    Task<IReadOnlyList<EvaluationPayload>> GetSessionEvaluationsAsync(
        string sessionId,
        CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IStudyGuideRepository
{
    Task SaveAsync(string sessionId, string markdown, CancellationToken ct = default);

    Task<string?> GetBySessionAsync(string sessionId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
