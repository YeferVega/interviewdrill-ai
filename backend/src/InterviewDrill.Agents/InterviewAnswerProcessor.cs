using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Agents;

public sealed class InterviewAnswerProcessor(
    IInterviewerSessionStore sessionStore,
    IPiiSanitizer piiSanitizer,
    IArchitectureEvaluator architectureEvaluator,
    ISecurityRigorJudge securityJudge,
    IScoringSynthesizer scoringSynthesizer,
    IInterviewerAgent interviewerAgent,
    IInterviewAnswerRepository answerRepository,
    IStudyGuideGenerator studyGuideGenerator,
    IStudyGuideRepository studyGuideRepository) : IInterviewAnswerProcessor
{
    public async Task<InterviewAnswerResult> SubmitAnswerAsync(
        AnswerSubmission submission,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(submission.AnswerText))
        {
            throw new AnswerRequiredException();
        }

        var state = await sessionStore.LoadAsync(submission.SessionId, ct)
            ?? throw new UnknownInterviewSessionException(submission.SessionId);

        if (!string.Equals(state.UserId, submission.UserId, StringComparison.Ordinal))
        {
            throw new UnknownInterviewSessionException(submission.SessionId);
        }

        var sanitized = piiSanitizer.Sanitize(submission.AnswerText);

        // The submission language wins; otherwise use the one stored when the session started.
        var language = InterviewLanguages.Normalize(submission.Language) ?? state.Language;

        var response = new CandidateResponse
        {
            SessionId = submission.SessionId,
            QuestionId = state.LastQuestionId ?? $"question:{submission.SessionId}:current",
            Text = sanitized.Text,
            SkillTarget = state.LastSkillTarget,
            Language = language
        };

        var architecture = await architectureEvaluator.EvaluateAsync(response, ct);
        var security = await securityJudge.EvaluateAsync(response, ct);
        var feedback = await scoringSynthesizer.SynthesizeAsync(architecture, security, ct, language);

        await answerRepository.AddAnswerAsync(
            submission.SessionId,
            state.LastQuestionId ?? $"question:{submission.SessionId}:current",
            sanitized.Text,
            feedback,
            ct);

        await answerRepository.SaveChangesAsync(ct);

        var nextQuestion = await interviewerAgent.NextQuestionAsync(
            submission.SessionId,
            submission.UserId,
            feedback,
            ct);

        string? studyGuideMarkdown = null;

        if (nextQuestion is null)
        {
            var evaluations = await answerRepository.GetSessionEvaluationsAsync(submission.SessionId, ct);
            studyGuideMarkdown = await studyGuideGenerator.GenerateAsync(state.Profile, evaluations, ct, language);
            await studyGuideRepository.SaveAsync(submission.SessionId, studyGuideMarkdown, ct);
            await studyGuideRepository.SaveChangesAsync(ct);
            await sessionStore.MarkCompletedAsync(submission.SessionId, DateTimeOffset.UtcNow, ct);
        }

        return new InterviewAnswerResult(
            feedback,
            nextQuestion,
            nextQuestion is null,
            studyGuideMarkdown);
    }
}
