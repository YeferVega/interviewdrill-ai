using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;

namespace InterviewDrill.Agents.Tests;

/// <summary>
/// Spec 021: interview output language ("es"/"en") plumbing across agents.
/// Null language must keep the historical behavior so nothing regresses.
/// </summary>
public sealed class LanguageSupportTests
{
    private static JobProfile Profile() => new()
    {
        Role = "Senior Backend Engineer",
        Seniority = Seniority.Senior,
        RequiredSkills = [new WeightedSkill { Name = "RAG", Weight = 0.9 }],
        NiceToHaveSkills = ["Kafka"],
        Responsibilities = ["diseñar sistemas de producción"],
        RawTextHash = "hash-jd-021"
    };

    [Theory]
    [InlineData("ES", "es")]
    [InlineData("es-ES", "es")]
    [InlineData("English", "en")]
    [InlineData("en-US", "en")]
    [InlineData("klingon", null)]
    [InlineData(null, null)]
    public void Normalize_AcceptsAliasesAndRejectsUnknown(string? input, string? expected)
    {
        Assert.Equal(expected, InterviewLanguages.Normalize(input));
    }

    [Fact]
    public async Task HeuristicGenerator_NullOrEsLanguage_ReturnsSpanishQuestion()
    {
        var generator = new HeuristicInterviewerQuestionGenerator();

        var nullable = await generator.GenerateAsync(Request(language: null));
        var spanish = await generator.GenerateAsync(Request(language: "es"));

        Assert.Contains("¿", nullable.Text, StringComparison.Ordinal);
        Assert.Contains("¿", spanish.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HeuristicGenerator_EnglishLanguage_ReturnsEnglishQuestion()
    {
        var generator = new HeuristicInterviewerQuestionGenerator();

        var question = await generator.GenerateAsync(Request(language: "en"));

        Assert.DoesNotContain("¿", question.Text, StringComparison.Ordinal);
        Assert.Matches("(?i)\\b(explain|design|describe|which|how|a team)\\b", question.Text);
    }

    [Fact]
    public async Task ScoringSynthesizer_SpanishLanguage_LocalizesAdvice()
    {
        var sut = new ScoringSynthesizer();

        var spanish = await sut.SynthesizeAsync(Architecture(40), Security(30), CancellationToken.None, "es");
        var english = await sut.SynthesizeAsync(Architecture(40), Security(30));

        Assert.Contains("La respuesta necesita más profundidad técnica", spanish.ImprovementAdvice, StringComparison.Ordinal);
        Assert.Contains("The answer needs more technical depth", english.ImprovementAdvice, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StudyGuideGenerator_EnglishLanguage_WritesEnglishGuide()
    {
        var generator = new StudyGuideGenerator();
        var evaluations = new[] { PayloadWithMissed("HNSW") };

        var english = await generator.GenerateAsync(Profile(), evaluations, CancellationToken.None, "en");
        var spanish = await generator.GenerateAsync(Profile(), evaluations);

        Assert.Contains("# Study guide for", english, StringComparison.Ordinal);
        Assert.Contains("# Guía de estudio para", spanish, StringComparison.Ordinal);
        Assert.Contains("RAG, embeddings and vector search", english, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InterviewerAgent_StartSession_PersistsLanguageAndPassesItToGenerator()
    {
        var generator = new CapturingQuestionGenerator();
        var registry = new AlwaysNewQuestionRegistry();
        var store = new InMemoryInterviewerSessionStore();
        var agent = new InterviewerAgent(generator, registry, store);

        await agent.StartSessionAsync("session-1", "user-1", Profile(), CancellationToken.None, "EN");

        Assert.Equal("en", generator.LastRequest!.Language);

        var next = await agent.NextQuestionAsync("session-1", "user-1", PayloadWithMissed("HNSW"));

        Assert.NotNull(next);
        Assert.Equal("en", generator.LastRequest!.Language);
    }

    [Fact]
    public async Task InterviewerAgent_NullLanguage_KeepsStoredSessionLanguage()
    {
        var generator = new CapturingQuestionGenerator();
        var store = new InMemoryInterviewerSessionStore();
        var agent = new InterviewerAgent(generator, new AlwaysNewQuestionRegistry(), store);

        await agent.StartSessionAsync("session-2", "user-1", Profile(), CancellationToken.None, "es");
        await agent.NextQuestionAsync("session-2", "user-1", PayloadWithMissed("HNSW"));

        Assert.Equal("es", generator.LastRequest!.Language);
    }

    [Fact]
    public async Task Processor_SubmissionLanguageFlowsToJudgesAndSynthesizer()
    {
        var capturedResponses = new List<CandidateResponse>();
        var evaluator = new CapturingEvaluator(capturedResponses);
        var synthesizerCalls = new List<string?>();
        var captureSynthesizer = new CapturingSynthesizer(synthesizerCalls);

        var store = new InMemoryInterviewerSessionStore();
        await store.SaveAsync(new InterviewerSessionState(
            "session-3",
            "user-1",
            "hash-jd-021",
            Profile(),
            Difficulty.Hard,
            DateTimeOffset.UtcNow,
            "question-1",
            "RAG",
            "¿Qué es RAG?",
            new Dictionary<string, int> { ["RAG"] = 1 },
            new Dictionary<string, int>()));

        var processor = new InterviewAnswerProcessor(
            store,
            new FakePiiSanitizer(),
            evaluator,
            new CapturingEvaluator(capturedResponses),
            captureSynthesizer,
            new NoNextQuestionInterviewer(),
            new RecordingAnswerRepository(),
            new StudyGuideGenerator(),
            new InMemoryStudyGuideRepository());

        await processor.SubmitAnswerAsync(
            new AnswerSubmission("session-3", "user-1", "Uso HNSW y Polly.", "es"));

        Assert.All(capturedResponses, response => Assert.Equal("es", response.Language));
        Assert.Equal(["es"], synthesizerCalls);
    }

    private static QuestionGenerationRequest Request(string? language) => new(
        "session-1",
        Profile(),
        "RAG",
        Difficulty.Hard,
        QuestionType.FailureScenario,
        LastQuestionText: null,
        Attempt: 0,
        Language: language);

    private static ArchitectureEvaluation Architecture(double score) => new()
    {
        TechnicalDepth = score,
        CoveredKeywords = [],
        MissedConcepts = ["HNSW"],
        Strengths = [],
        DetectedPatterns = [],
        AntiPatterns = []
    };

    private static SecurityEvaluation Security(double score) => new()
    {
        ProductionReadiness = score,
        CoveredKeywords = [],
        MissedConcepts = [],
        Strengths = [],
        SecurityConcerns = [],
        ResilienceScore = score
    };

    private static EvaluationPayload PayloadWithMissed(string concept) => new()
    {
        OverallScore = 40,
        TechnicalDepth = 40,
        ProductionReadiness = 40,
        Verdict = Verdict.Fail,
        CoveredKeywords = [],
        MissedConcepts = [concept],
        Strengths = [],
        ImprovementAdvice = "practice"
    };

    private sealed class CapturingQuestionGenerator : IInterviewerQuestionGenerator
    {
        public QuestionGenerationRequest? LastRequest { get; private set; }

        public Task<InterviewQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(new InterviewQuestion
            {
                Id = Guid.NewGuid().ToString(),
                Text = $"question for {request.SkillTarget}",
                SkillTarget = request.SkillTarget,
                Difficulty = request.Difficulty,
                Type = request.Type
            });
        }
    }

    private sealed class AlwaysNewQuestionRegistry : IQuestionRegistry
    {
        public Task<bool> WasAlreadyAskedAsync(string userId, string jdHash, string questionText, CancellationToken ct = default)
            => Task.FromResult(false);

        public Task<bool> RegisterAsync(string sessionId, string userId, string jdHash, InterviewQuestion question, CancellationToken ct = default)
            => Task.FromResult(true);
    }

    private sealed class CapturingEvaluator(List<CandidateResponse> captured) : IArchitectureEvaluator, ISecurityRigorJudge
    {
        public Task<ArchitectureEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default)
        {
            captured.Add(response);
            return Task.FromResult(new ArchitectureEvaluation
            {
                TechnicalDepth = 50,
                CoveredKeywords = [],
                MissedConcepts = [],
                Strengths = [],
                DetectedPatterns = [],
                AntiPatterns = []
            });
        }

        Task<SecurityEvaluation> ISecurityRigorJudge.EvaluateAsync(CandidateResponse response, CancellationToken ct)
        {
            captured.Add(response);
            return Task.FromResult(new SecurityEvaluation
            {
                ProductionReadiness = 50,
                CoveredKeywords = [],
                MissedConcepts = [],
                Strengths = [],
                SecurityConcerns = [],
                ResilienceScore = 50
            });
        }
    }

    private sealed class CapturingSynthesizer(List<string?> languageCalls) : IScoringSynthesizer
    {
        public Task<EvaluationPayload> SynthesizeAsync(
            ArchitectureEvaluation architectureEval,
            SecurityEvaluation securityEval,
            CancellationToken ct = default,
            string? language = null)
        {
            languageCalls.Add(language);
            return Task.FromResult(PayloadWithMissed("x"));
        }
    }

    private sealed class NoNextQuestionInterviewer : IInterviewerAgent
    {
        public Task<InterviewQuestion> StartSessionAsync(
            string sessionId,
            string userId,
            JobProfile profile,
            CancellationToken ct = default,
            string? language = null) => throw new NotSupportedException();

        public Task<InterviewQuestion?> NextQuestionAsync(
            string sessionId,
            string userId,
            EvaluationPayload lastEvaluation,
            CancellationToken ct = default) => Task.FromResult<InterviewQuestion?>(null);
    }

    private sealed class RecordingAnswerRepository : IInterviewAnswerRepository
    {
        public Task AddAnswerAsync(
            string sessionId,
            string questionId,
            string sanitizedAnswerText,
            EvaluationPayload evaluation,
            CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<EvaluationPayload>> GetSessionEvaluationsAsync(
            string sessionId,
            CancellationToken ct = default) => Task.FromResult<IReadOnlyList<EvaluationPayload>>([]);

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
    }
}
