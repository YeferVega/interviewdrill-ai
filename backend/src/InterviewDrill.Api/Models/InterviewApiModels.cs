using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Api;

public sealed record StartInterviewRequest(string UserId, JobProfile Profile, string? Language = null);

public sealed record SubmitAnswerRequest(string UserId, string AnswerText, string? Language = null);

public sealed record InterviewStartResponse(string SessionId, InterviewQuestion Question);

public sealed record InterviewAnswerResponse(
    EvaluationPayload Feedback,
    InterviewQuestion? NextQuestion,
    bool InterviewCompleted,
    string? StudyGuideMarkdown);
