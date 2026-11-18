using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Agents.Tests;

public sealed class HeuristicEvaluatorTests
{
    [Fact]
    public async Task ArchitectureEvaluator_EmptyAnswer_ReturnsZero()
    {
        var evaluator = new HeuristicArchitectureEvaluator(new PassThroughPiiSanitizer());

        var result = await evaluator.EvaluateAsync(new CandidateResponse
        {
            SessionId = "session",
            QuestionId = "question",
            Text = "",
            SkillTarget = "RAG"
        });

        Assert.Equal(0, result.TechnicalDepth);
        Assert.Contains("RAG", result.MissedConcepts);
    }

    [Fact]
    public async Task ArchitectureEvaluator_TargetedConcreteAnswer_ScoresHigher()
    {
        var evaluator = new HeuristicArchitectureEvaluator(new PassThroughPiiSanitizer());

        var weak = await evaluator.EvaluateAsync(new CandidateResponse
        {
            SessionId = "session",
            QuestionId = "question",
            Text = "Usa un modelo.",
            SkillTarget = "RAG"
        });

        var strong = await evaluator.EvaluateAsync(new CandidateResponse
        {
            SessionId = "session",
            QuestionId = "question",
            Text = """
                Usar RAG con embeddings, vector search HNSW y guardrails de relevancia.
                Agregar observabilidad, pruebas de calidad y monitoreo de latencia P95.
                """,
            SkillTarget = "RAG"
        });

        Assert.True(strong.TechnicalDepth > weak.TechnicalDepth);
        Assert.Contains("RAG", strong.CoveredKeywords);
    }

    [Fact]
    public async Task SecurityJudge_EmptyAnswer_ReturnsZero()
    {
        var judge = new HeuristicSecurityRigorJudge(new PassThroughPiiSanitizer());

        var result = await judge.EvaluateAsync(new CandidateResponse
        {
            SessionId = "session",
            QuestionId = "question",
            Text = "   "
        });

        Assert.Equal(0, result.ProductionReadiness);
        Assert.NotEmpty(result.MissedConcepts);
    }

    [Fact]
    public async Task SecurityJudge_ProductionControls_ScoresHigher()
    {
        var judge = new HeuristicSecurityRigorJudge(new PassThroughPiiSanitizer());

        var result = await judge.EvaluateAsync(new CandidateResponse
        {
            SessionId = "session",
            QuestionId = "question",
            Text = """
                Antes del LLM sanitizo PII, valido guardrails de relevancia, manejo rate limiting con retry,
                Polly circuit breaker, observabilidad, P95, semantic cache y feature flags con rollback.
                """
        });

        Assert.True(result.ProductionReadiness >= 80);
        Assert.True(result.ResilienceScore >= 60);
    }

    private sealed class PassThroughPiiSanitizer : IPiiSanitizer
    {
        public PiiSanitizationResult Sanitize(string? input, bool mask = true)
        {
            return new PiiSanitizationResult(input ?? string.Empty, []);
        }

        public void AddPattern(PiiPatternDefinition definition)
        {
        }
    }
}
