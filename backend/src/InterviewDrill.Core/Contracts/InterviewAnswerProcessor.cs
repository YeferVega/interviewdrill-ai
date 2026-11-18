using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Core.InterviewFlow;

public sealed record AnswerSubmission(
    string SessionId,
    string UserId,
    string AnswerText,
    string? Language = null);

public sealed record InterviewAnswerResult(
    EvaluationPayload Feedback,
    InterviewQuestion? NextQuestion,
    bool InterviewCompleted,
    string? StudyGuideMarkdown = null);

public interface IInterviewAnswerProcessor
{
    Task<InterviewAnswerResult> SubmitAnswerAsync(AnswerSubmission submission, CancellationToken ct = default);
}
