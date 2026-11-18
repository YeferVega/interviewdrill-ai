using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Llm;

namespace InterviewDrill.Agents;

public sealed class FallbackArchitectureEvaluator(
    IArchitectureEvaluator primary,
    HeuristicArchitectureEvaluator fallback) : IArchitectureEvaluator
{
    public async Task<ArchitectureEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default)
    {
        try
        {
            return await primary.EvaluateAsync(response, ct);
        }
        catch (Exception ex) when (IsFallbackable(ex))
        {
            return await fallback.EvaluateAsync(response, ct);
        }
    }

    internal static bool IsFallbackable(Exception ex)
    {
        return ex is LlmUnavailableException or EvaluationParsingException;
    }
}

public sealed class FallbackSecurityRigorJudge(
    ISecurityRigorJudge primary,
    HeuristicSecurityRigorJudge fallback) : ISecurityRigorJudge
{
    public async Task<SecurityEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default)
    {
        try
        {
            return await primary.EvaluateAsync(response, ct);
        }
        catch (Exception ex) when (FallbackArchitectureEvaluator.IsFallbackable(ex))
        {
            return await fallback.EvaluateAsync(response, ct);
        }
    }
}
