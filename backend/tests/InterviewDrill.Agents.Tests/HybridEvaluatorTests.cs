using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Agents.Tests;

public sealed class HybridEvaluatorTests
{
    private const string ArchitectureJson = """
        {
          "reasoning": "The candidate mentions concrete architecture and production controls.",
          "clarity": 4,
          "technicalAccuracy": 4,
          "architectureDepth": 5,
          "communication": 4,
          "coveredKeywords": ["RAG", "vector database"],
          "missedConcepts": ["relevance threshold"],
          "strengths": ["Clear reasoning"],
          "detectedAntiPatterns": ["DelegatingMathToLLM"]
        }
        """;

    private const string SecurityJson = """
        {
          "reasoning": "The answer has PII and resilience, but lacks rollback details.",
          "piiProtection": 5,
          "relevanceGuardrails": 4,
          "observability": 3,
          "resilience": 5,
          "deploymentSafety": 2,
          "coveredKeywords": ["PII", "Polly"],
          "missedConcepts": ["rollback"],
          "strengths": ["Good privacy thinking"],
          "securityConcerns": ["Deployment safety is weak"]
        }
        """;

    [Fact]
    public async Task HybridArchitectureEvaluator_MergesJudgeRubricWithDeterministicSignals()
    {
        var sanitizer = new PassThroughPiiSanitizer();
        var preFilter = new HeuristicArchitectureEvaluator(sanitizer);
        var evaluator = new HybridArchitectureEvaluator(preFilter, new LlmArchitectureJudge(new FixedLlm(ArchitectureJson), sanitizer));

        var response = Candidate("RAG");
        var result = await evaluator.EvaluateAsync(response);

        Assert.InRange(result.TechnicalDepth, 0, 100);
        Assert.Contains("vector database", result.CoveredKeywords);
        Assert.Contains("DelegatingMathToLLM", result.AntiPatterns);
    }

    [Fact]
    public async Task FallbackArchitectureEvaluator_UsesHeuristicWhenLlmIsUnavailable()
    {
        var sanitizer = new PassThroughPiiSanitizer();
        var heuristic = new HeuristicArchitectureEvaluator(sanitizer);
        var fallback = new FallbackArchitectureEvaluator(
            new HybridArchitectureEvaluator(heuristic, new LlmArchitectureJudge(new FixedLlm(_ => throw new LlmUnavailableException()), sanitizer)),
            heuristic);

        var result = await fallback.EvaluateAsync(Candidate("RAG"));

        Assert.True(result.TechnicalDepth >= 0);
        Assert.Contains("RAG", result.CoveredKeywords);
    }

    [Fact]
    public async Task FallbackArchitectureEvaluator_UsesHeuristicWhenJudgeScoresAreInvalid()
    {
        var sanitizer = new PassThroughPiiSanitizer();
        var heuristic = new HeuristicArchitectureEvaluator(sanitizer);
        var fallback = new FallbackArchitectureEvaluator(
            new HybridArchitectureEvaluator(
                heuristic,
                new LlmArchitectureJudge(new FixedLlm("""{"reasoning":"bad","clarity":9,"technicalAccuracy":1,"architectureDepth":1,"communication":1,"coveredKeywords":[],"missedConcepts":[],"strengths":[],"detectedAntiPatterns":[]}"""), sanitizer)),
            heuristic);

        var result = await fallback.EvaluateAsync(Candidate("RAG"));

        Assert.True(result.TechnicalDepth >= 0);
    }

    [Fact]
    public async Task HybridSecurityJudge_MergesRubricWithPreFilter()
    {
        var sanitizer = new PassThroughPiiSanitizer();
        var preFilter = new HeuristicSecurityRigorJudge(sanitizer);
        var judge = new HybridSecurityRigorJudge(preFilter, new LlmSecurityJudge(new FixedLlm(SecurityJson), sanitizer));

        var response = Candidate("Kafka");
        var result = await judge.EvaluateAsync(response);

        Assert.InRange(result.ProductionReadiness, 0, 100);
        Assert.InRange(result.ResilienceScore, 0, 100);
        Assert.Contains("rollback", result.MissedConcepts);
    }

    [Fact]
    public async Task FallbackSecurityJudge_UsesHeuristicWhenLlmFails()
    {
        var sanitizer = new PassThroughPiiSanitizer();
        var heuristic = new HeuristicSecurityRigorJudge(sanitizer);
        var fallback = new FallbackSecurityRigorJudge(
            new HybridSecurityRigorJudge(heuristic, new LlmSecurityJudge(new FixedLlm(_ => throw new LlmUnavailableException()), sanitizer)),
            heuristic);

        var result = await fallback.EvaluateAsync(Candidate("Kafka"));

        Assert.True(result.ProductionReadiness >= 0);
    }

    private static CandidateResponse Candidate(string skill) => new()
    {
        SessionId = "session",
        QuestionId = "question",
        SkillTarget = skill,
        Text = "Usa RAG con embeddings, HNSW, Polly retry, circuit breaker, observabilidad, PII sanitization y guardrails."
    };

    private sealed class FixedLlm : ILlmCompletionClient
    {
        private readonly string? _staticResponse;
        private readonly Func<LlmCompletionRequest, string>? _factory;

        public FixedLlm(string response)
        {
            _staticResponse = response;
        }

        public FixedLlm(Func<LlmCompletionRequest, string> factory)
        {
            _factory = factory;
        }

        public Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
        {
            return Task.FromResult(_factory is null
                ? _staticResponse!
                : _factory(request));
        }
    }

    private sealed class PassThroughPiiSanitizer : IPiiSanitizer
    {
        public PiiSanitizationResult Sanitize(string? input, bool mask = true) => new(input ?? string.Empty, []);

        public void AddPattern(PiiPatternDefinition definition)
        {
        }
    }
}
