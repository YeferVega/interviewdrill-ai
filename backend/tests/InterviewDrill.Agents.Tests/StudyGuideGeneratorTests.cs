using System.Text.RegularExpressions;
using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents.Tests;

public sealed class StudyGuideGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_PrioritizesFrequentHighWeightMissedTopic()
    {
        var generator = new StudyGuideGenerator();

        var guide = await generator.GenerateAsync(
            Profile(),
            [
                Evaluation(40, missed: ["RAG", "HNSW", "PII sanitization"]),
                Evaluation(60, missed: ["PII sanitization", "guardrails"])
            ]);

        Assert.Contains("RAG, embeddings y búsqueda vectorial", guide);
        Assert.Contains("Seguridad de datos y LLM guardrails", guide);
        Assert.Contains("Promedio de la entrevista:", guide);
        Assert.True(guide.IndexOf("RAG, embeddings", StringComparison.Ordinal) < guide.IndexOf("Seguridad de datos", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_LimitsToSevenTopics()
    {
        var generator = new StudyGuideGenerator();
        var missed = new[]
        {
            "RAG", "HNSW", "PII", "guardrails", "Kafka", "Polly", "OpenTelemetry",
            "Semantic Kernel", "CI/CD", "React", "encryption", "latency"
        };

        var guide = await generator.GenerateAsync(
            Profile(),
            [Evaluation(35, missed: missed)]);

        var topicCount = Regex.Matches(guide, @"^### \d+\.", RegexOptions.Multiline).Count;

        Assert.Equal(7, topicCount);
    }

    [Fact]
    public async Task GenerateAsync_NoMissedConcepts_GeneratesAdvancedStudyGuide()
    {
        var generator = new StudyGuideGenerator();

        var guide = await generator.GenerateAsync(
            Profile(),
            [Evaluation(90, missed: [], strengths: ["Clear architecture"])])
            ;

        Assert.Contains("Promedio alto", guide);
        Assert.Contains("Clear architecture", guide);
    }

    private static JobProfile Profile() => new()
    {
        Role = "AI Backend Engineer",
        Seniority = Seniority.Senior,
        RequiredSkills =
        [
            new WeightedSkill { Name = "RAG", Weight = 0.95 },
            new WeightedSkill { Name = "Kafka", Weight = 0.7 }
        ],
        Responsibilities = ["Build production AI services"],
        RawTextHash = "sha256:test",
        ProfileSource = "test"
    };

    private static EvaluationPayload Evaluation(double score, IReadOnlyList<string>? missed = null, IReadOnlyList<string>? strengths = null) =>
        new()
        {
            OverallScore = score,
            TechnicalDepth = score,
            ProductionReadiness = score,
            Verdict = score >= 80 ? Verdict.Pass : score >= 60 ? Verdict.NeedsPractice : Verdict.Fail,
            CoveredKeywords = [],
            MissedConcepts = missed ?? [],
            Strengths = strengths ?? [],
            ImprovementAdvice = "Practice."
        };
}
