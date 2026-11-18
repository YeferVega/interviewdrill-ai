using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Agents.Tests;

public sealed class LlmArchitectureEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_ReturnsEvaluationFromJudge()
    {
        var llm = new FixedLlmClient("""
        {
          "reasoning": "El candidato menciona conceptos clave de arquitectura.",
          "clarity": 5,
          "technicalAccuracy": 5,
          "architectureDepth": 5,
          "communication": 5,
          "coveredKeywords": ["KernelFunction", "HNSW"],
          "missedConcepts": ["Polly"],
          "strengths": ["Menciona HNSW"],
          "detectedAntiPatterns": ["DelegatingMathToLLM"]
        }
        """);

        var sanitizer = new FakePiiSanitizer();
        var heuristic = new HeuristicArchitectureEvaluator(sanitizer);
        var judge = new LlmArchitectureJudge(llm, sanitizer);
        var evaluator = new HybridArchitectureEvaluator(heuristic, judge);

        var response = new CandidateResponse
        {
            SessionId = "session-1",
            QuestionId = "q-1",
            Text = "Usaría HNSW para búsqueda vectorial y Semantic Kernel para orquestación.",
            SkillTarget = "RAG"
        };

        var result = await evaluator.EvaluateAsync(response, CancellationToken.None);

        Assert.True(result.TechnicalDepth > 0);
        Assert.NotEmpty(result.CoveredKeywords);
        Assert.NotEmpty(result.AntiPatterns);
    }

    private sealed class FixedLlmClient : ILlmCompletionClient
    {
        private readonly string _response;

        public FixedLlmClient(string response) => _response = response;

        public Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
        {
            return Task.FromResult(_response);
        }
    }
}