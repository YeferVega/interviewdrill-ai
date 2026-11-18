using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents;

public sealed class HybridSecurityRigorJudge(
    HeuristicSecurityRigorJudge preFilter,
    LlmSecurityJudge judge) : ISecurityRigorJudge
{
    public async Task<SecurityEvaluation> EvaluateAsync(
        CandidateResponse response,
        CancellationToken ct = default)
    {
        var deterministic = await preFilter.EvaluateAsync(response, ct);

        if (string.IsNullOrWhiteSpace(response.Text))
        {
            return deterministic;
        }

        var llm = await judge.JudgeAsync(response, deterministic, ct);

        var judgeScore = AnchoredRubricMath.WeightedNormalized(
            new Dictionary<double, double>
            {
                [llm.PiiProtection] = 0.25,
                [llm.RelevanceGuardrails] = 0.20,
                [llm.Observability] = 0.15,
                [llm.Resilience] = 0.25,
                [llm.DeploymentSafety] = 0.15
            });

        var productionReadiness = AnchoredRubricMath.CombineDeterministicAndJudge(
            deterministic.ProductionReadiness,
            judgeScore);

        var resilienceJudge = AnchoredRubricMath.Normalize(llm.Resilience);
        var resilienceScore = Math.Clamp(Math.Round((deterministic.ResilienceScore * 0.35) + (resilienceJudge * 0.65), 2), 0, 100);

        return new SecurityEvaluation
        {
            ProductionReadiness = productionReadiness,
            ResilienceScore = resilienceScore,
            CoveredKeywords = Merge(deterministic.CoveredKeywords, llm.CoveredKeywords),
            MissedConcepts = Merge(deterministic.MissedConcepts, llm.MissedConcepts),
            Strengths = Merge(deterministic.Strengths, llm.Strengths),
            SecurityConcerns = Merge(deterministic.SecurityConcerns, llm.SecurityConcerns)
        };
    }

    private static IReadOnlyList<string> Merge(params IEnumerable<string>[] sources)
    {
        return sources
            .SelectMany(x => x)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
