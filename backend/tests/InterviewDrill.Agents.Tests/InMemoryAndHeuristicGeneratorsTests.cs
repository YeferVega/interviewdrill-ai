using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Persistence;

namespace InterviewDrill.Agents.Tests;

public sealed class InMemoryRepositoriesTests
{
    private static JobProfile Profile() => new()
    {
        Role = "Senior Backend Engineer",
        Seniority = Seniority.Senior,
        RequiredSkills = [new WeightedSkill { Name = "RAG", Weight = 0.9 }],
        Responsibilities = ["diseñar sistemas de producción"],
        RawTextHash = "hash-jd"
    };

    private static InterviewQuestion Q(string id, string text) => new()
    {
        Id = id,
        Text = text,
        SkillTarget = "RAG",
        Difficulty = Difficulty.Medium
    };

    [Fact]
    public async Task InMemoryQuestionRegistry_FirstRegistration_ReturnsTrue_AndDuplicate_ReturnsFalse()
    {
        var registry = new InMemoryQuestionRegistry();

        var first = await registry.RegisterAsync("s1", "u1", "jd1", Q("q1", "¿Qué es RAG?"));
        var second = await registry.RegisterAsync("s1", "u1", "jd1", Q("q2", "¿Qué es RAG?"));

        Assert.True(first);
        Assert.False(second);
        Assert.True(await registry.WasAlreadyAskedAsync("u1", "jd1", "¿Qué es RAG?"));
    }

    [Fact]
    public async Task InMemoryQuestionRegistry_NormalizesCaseAndWhitespace()
    {
        var registry = new InMemoryQuestionRegistry();

        await registry.RegisterAsync("s1", "u1", "jd1", Q("q1", "¿Cómo   Escalar   HNSW?"));
        var dup = await registry.RegisterAsync("s1", "u1", "jd1", Q("q2", "¿cómo escalar  hnsw?"));

        Assert.False(dup);
    }

    [Fact]
    public async Task InMemoryQuestionRegistry_IsIsolatedPerUserAndJd()
    {
        var registry = new InMemoryQuestionRegistry();

        await registry.RegisterAsync("s1", "u1", "jd1", Q("q1", "¿Qué es RAG?"));

        Assert.False(await registry.WasAlreadyAskedAsync("u2", "jd1", "¿Qué es RAG?"));
        Assert.False(await registry.WasAlreadyAskedAsync("u1", "jd2", "¿Qué es RAG?"));
        Assert.True(await registry.WasAlreadyAskedAsync("u1", "jd1", "¿Qué es RAG?"));

        var sameTextDifferentUser = await registry.RegisterAsync("s2", "u2", "jd1", Q("q9", "¿Qué es RAG?"));
        Assert.True(sameTextDifferentUser);
    }

    [Fact]
    public async Task InMemoryInterviewAnswerRepository_ReturnsOnlyEvaluationsForSession()
    {
        var repo = new InMemoryInterviewAnswerRepository();
        var e1 = Payload(70);
        var e2 = Payload(85);

        await repo.AddAnswerAsync("s1", "q1", "ans", e1);
        await repo.AddAnswerAsync("s2", "q1", "ans", e2);

        var s1 = await repo.GetSessionEvaluationsAsync("s1");
        var s2 = await repo.GetSessionEvaluationsAsync("s2");

        Assert.Single(s1);
        Assert.Equal(70, s1[0].OverallScore);
        Assert.Single(s2);

        Assert.Equal(0, await repo.SaveChangesAsync());
    }

    [Fact]
    public async Task InMemoryStudyGuideRepository_SaveThenGet_AndUpdatePreservesId()
    {
        var repo = new InMemoryStudyGuideRepository();

        await repo.SaveAsync("s1", "v1");
        var info1 = repo.GetInfo("s1");
        Assert.NotNull(info1);

        await repo.SaveAsync("s1", "v2");
        var md = await repo.GetBySessionAsync("s1");
        var info2 = repo.GetInfo("s1");

        Assert.Equal("v2", md);
        Assert.Equal(info1.Value.Id, info2!.Value.Id);
        Assert.Null(repo.GetInfo("nope"));
        Assert.Null(await repo.GetBySessionAsync("nope"));
        Assert.Equal(0, await repo.SaveChangesAsync());
    }

    [Fact]
    public async Task InMemorySessionHistoryRepository_FiltersByUserAndMarksCompleted()
    {
        var store = new InMemoryInterviewerSessionStore();
        var answers = new InMemoryInterviewAnswerRepository();
        var guides = new InMemoryStudyGuideRepository();
        var history = new InMemorySessionHistoryRepository(store, answers, guides);
        var started = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await store.SaveAsync(new InterviewerSessionState("s1", "u1", "jd", Profile(), Difficulty.Medium, started, null, null, null, new Dictionary<string, int>(), new Dictionary<string, int>()));
        await store.SaveAsync(new InterviewerSessionState("s2", "u2", "jd", Profile(), Difficulty.Medium, started, null, null, null, new Dictionary<string, int>(), new Dictionary<string, int>()));

        await answers.AddAnswerAsync("s1", "q1", "ans", Payload(88));
        await answers.AddAnswerAsync("s1", "q2", "ans", Payload(72));

        var end = started.AddMinutes(20);
        await store.MarkCompletedAsync("s1", end);
        await guides.SaveAsync("s1", "# Guía");

        var u1 = await history.GetByUserAsync("u1");
        var rec = Assert.Single(u1);
        Assert.Equal("s1", rec.SessionId);
        Assert.Equal(InterviewSessionRecordStatus.Completed, rec.Status);
        Assert.Equal(new double[] { 72, 88 }, rec.EvaluationScores.OrderBy(x => x).ToList());
        Assert.NotNull(rec.StudyGuideId);

        var u2 = await history.GetByUserAsync("u2");
        Assert.Equal(InterviewSessionRecordStatus.InProgress, Assert.Single(u2).Status);

        Assert.Empty(await history.GetByUserAsync("ghost"));
    }

    private static EvaluationPayload Payload(double score) => new()
    {
        OverallScore = score,
        TechnicalDepth = score,
        ProductionReadiness = score,
        Verdict = Verdict.Pass,
        CoveredKeywords = ["RAG"],
        MissedConcepts = [],
        Strengths = ["claridad"],
        ImprovementAdvice = "n/a"
    };
}

public sealed class HeuristicInterviewerQuestionGeneratorTests
{
    private static JobProfile Profile() => new()
    {
        Role = "Backend Engineer.",
        Seniority = Seniority.Senior,
        RequiredSkills = [new WeightedSkill { Name = "Kafka", Weight = 0.8 }],
        Responsibilities = [],
        RawTextHash = "jd-hash"
    };

    private static QuestionGenerationRequest Req(QuestionType type, Difficulty difficulty = Difficulty.Hard, int attempt = 0) => new(
        "s1", Profile(), "Kafka", difficulty, type, null, attempt);

    private readonly HeuristicInterviewerQuestionGenerator _sut = new();

    [Theory]
    [InlineData(QuestionType.Conceptual)]
    [InlineData(QuestionType.Situational)]
    [InlineData(QuestionType.FailureScenario)]
    [InlineData(QuestionType.FollowUp)]
    public async Task GenerateAsync_ProducesNonEmptyTextWithPlaceholdersFilled(QuestionType type)
    {
        var q = await _sut.GenerateAsync(Req(type));

        Assert.False(string.IsNullOrWhiteSpace(q.Text));
        Assert.DoesNotContain("{skill}", q.Text);
        Assert.DoesNotContain("{role}", q.Text);
        Assert.DoesNotContain("{responsibility}", q.Text);
        Assert.Equal("Kafka", q.SkillTarget);
        Assert.Equal(type, q.Type);
    }

    [Fact]
    public async Task GenerateAsync_FailureScenarioExpert_UsesStrategyTemplates()
    {
        var q = await _sut.GenerateAsync(Req(QuestionType.FailureScenario, Difficulty.Expert));

        Assert.Contains("guardrails", q.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_AttemptIndexWrapsAroundTemplates()
    {
        var a = await _sut.GenerateAsync(Req(QuestionType.Situational, attempt: 0));
        var b = await _sut.GenerateAsync(Req(QuestionType.Situational, attempt: 3));

        Assert.Equal(a.Text, b.Text);
    }

    [Fact]
    public async Task GenerateAsync_NormalizesRoleTrailingPunctuation()
    {
        var q = await _sut.GenerateAsync(Req(QuestionType.Conceptual));

        Assert.DoesNotContain("Engineer. y", q.Text);
        Assert.Contains("Backend Engineer", q.Text);
    }
}

public sealed class FallbackInterviewerQuestionGeneratorTests
{
    private static QuestionGenerationRequest Req() => new(
        "s1",
        new JobProfile { Role = "BE", Seniority = Seniority.Mid, RequiredSkills = [], Responsibilities = [], RawTextHash = "h" },
        "Kafka", Difficulty.Medium, QuestionType.Situational, null, 0);

    [Fact]
    public async Task GenerateAsync_PrimarySucceeds_ReturnsPrimary()
    {
        var sut = new FallbackInterviewerQuestionGenerator(
            new StubGenerator(QuestionType.Situational, "primary"),
            new StubGenerator(QuestionType.Situational, "fallback"));

        var q = await sut.GenerateAsync(Req());

        Assert.Equal("primary", q.Text);
    }

    [Fact]
    public async Task GenerateAsync_PrimaryUnavailable_UsesFallback()
    {
        var sut = new FallbackInterviewerQuestionGenerator(
            new ThrowingGenerator(new LlmUnavailableException()),
            new StubGenerator(QuestionType.Situational, "fallback"));

        var q = await sut.GenerateAsync(Req());

        Assert.Equal("fallback", q.Text);
    }

    [Fact]
    public async Task GenerateAsync_PrimaryParsingError_UsesFallback()
    {
        var sut = new FallbackInterviewerQuestionGenerator(
            new ThrowingGenerator(new InterviewQuestionParsingException("bad")),
            new StubGenerator(QuestionType.Situational, "fallback"));

        var q = await sut.GenerateAsync(Req());

        Assert.Equal("fallback", q.Text);
    }

    [Fact]
    public async Task GenerateAsync_PrimaryFatalError_Propagates()
    {
        var sut = new FallbackInterviewerQuestionGenerator(
            new ThrowingGenerator(new InvalidOperationException("fatal")),
            new StubGenerator(QuestionType.Situational, "fallback"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateAsync(Req()));
    }

    private sealed class StubGenerator(QuestionType type, string text) : IInterviewerQuestionGenerator
    {
        public Task<InterviewQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken ct = default) =>
            Task.FromResult(new InterviewQuestion { Id = Guid.NewGuid().ToString(), Text = text, SkillTarget = request.SkillTarget, Difficulty = request.Difficulty, Type = type });
    }

    private sealed class ThrowingGenerator(Exception exception) : IInterviewerQuestionGenerator
    {
        public Task<InterviewQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken ct = default) =>
            Task.FromException<InterviewQuestion>(exception);
    }
}
