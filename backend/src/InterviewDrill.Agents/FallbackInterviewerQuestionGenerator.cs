using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Core.Llm;

namespace InterviewDrill.Agents;

public sealed class FallbackInterviewerQuestionGenerator(
    IInterviewerQuestionGenerator primary,
    IInterviewerQuestionGenerator fallback) : IInterviewerQuestionGenerator
{
    public async Task<InterviewQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            return await primary.GenerateAsync(request, ct);
        }
        catch (Exception ex) when (IsFallbackable(ex))
        {
            return await fallback.GenerateAsync(request, ct);
        }
    }

    private static bool IsFallbackable(Exception ex)
    {
        return ex is LlmUnavailableException or InterviewQuestionParsingException;
    }
}
