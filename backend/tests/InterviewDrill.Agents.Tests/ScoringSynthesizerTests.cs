using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents.Tests;

public sealed class ScoringSynthesizerTests
{
    [Theory]
    [InlineData(80, 80, 80, Verdict.Pass)]
    [InlineData(79.99, 79.99, 79.99, Verdict.NeedsPractice)]
    [InlineData(59.99, 59.99, 59.99, Verdict.Fail)]
    [InlineData(100, 100, 100, Verdict.Pass)]
    public async Task Synthesize_CalculatesWeightedScoreAndVerdict(
        double technical,
        double production,
        double overall,
        Verdict expected)
    {
        var sut = new ScoringSynthesizer();

        var result = await sut.SynthesizeAsync(
            ArchitectureEvaluation(technical),
            SecurityEvaluation(production));

        Assert.Equal(expected, result.Verdict);
        Assert.Equal(overall, result.OverallScore);
    }

    [Fact]
    public async Task Synthesize_MergesUniqueKeywordsAndAdvice()
    {
        var sut = new ScoringSynthesizer();

        var result = await sut.SynthesizeAsync(
            new ArchitectureEvaluation
            {
                TechnicalDepth = 50,
                CoveredKeywords = ["RAG", "Kafka"],
                MissedConcepts = ["HNSW"],
                Strengths = ["Concrete"],
                AntiPatterns = []
            },
            new SecurityEvaluation
            {
                ProductionReadiness = 40,
                CoveredKeywords = ["Kafka", "Polly"],
                MissedConcepts = ["PII", "Guardrails"],
                Strengths = ["Resilient"],
                SecurityConcerns = ["Missing PII"],
                ResilienceScore = 50
            });

        Assert.Equal(46, result.OverallScore);
        Assert.Equal(Verdict.Fail, result.Verdict);
        Assert.Contains("Kafka", result.CoveredKeywords);
        Assert.Equal(3, result.CoveredKeywords.Count);
        Assert.Contains("HNSW", result.MissedConcepts);
        Assert.Contains("Missing PII", result.ImprovementAdvice);
    }

    private static ArchitectureEvaluation ArchitectureEvaluation(double score) => new()
    {
        TechnicalDepth = score,
        CoveredKeywords = [],
        MissedConcepts = [],
        Strengths = [],
        DetectedPatterns = [],
        AntiPatterns = []
    };

    private static SecurityEvaluation SecurityEvaluation(double score) => new()
    {
        ProductionReadiness = score,
        CoveredKeywords = [],
        MissedConcepts = [],
        Strengths = [],
        SecurityConcerns = [],
        ResilienceScore = score
    };
}
