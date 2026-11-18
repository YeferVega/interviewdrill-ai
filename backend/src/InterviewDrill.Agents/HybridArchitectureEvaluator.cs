using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents;

public sealed class HybridArchitectureEvaluator(
    HeuristicArchitectureEvaluator preFilter,
    LlmArchitectureJudge judge) : IArchitectureEvaluator
{
    public async Task<ArchitectureEvaluation> EvaluateAsync(
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
                [llm.Clarity] = 0.20,
                [llm.TechnicalAccuracy] = 0.30,
                [llm.ArchitectureDepth] = 0.30,
                [llm.Communication] = 0.20
            });

        var technicalDepth = AnchoredRubricMath.CombineDeterministicAndJudge(
            deterministic.TechnicalDepth,
            judgeScore);

        return new ArchitectureEvaluation
        {
            TechnicalDepth = technicalDepth,
            CoveredKeywords = Merge(deterministic.CoveredKeywords, llm.CoveredKeywords),
            MissedConcepts = Merge(deterministic.MissedConcepts, llm.MissedConcepts),
            Strengths = Merge(deterministic.Strengths, llm.Strengths),
            DetectedPatterns = Merge(deterministic.DetectedPatterns, llm.CoveredKeywords),
            AntiPatterns = Merge(deterministic.AntiPatterns, llm.DetectedAntiPatterns)
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
