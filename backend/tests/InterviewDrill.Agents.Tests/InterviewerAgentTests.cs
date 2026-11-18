using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;

namespace InterviewDrill.Agents.Tests;

public sealed class InterviewerAgentTests
{
    [Fact]
    public async Task StartSessionAsync_UsesHighestWeightedSkillAndSeniorDifficulty()
    {
        var generator = new FakeInterviewerQuestionGenerator();
        var registry = new FakeQuestionRegistry();
        var store = new InMemoryInterviewerSessionStore();
        var agent = new InterviewerAgent(generator, registry, store);

        var question = await agent.StartSessionAsync(
            "session-1",
            "user-1",
            CreateProfile(Seniority.Senior));

        Assert.Equal("RAG", question.SkillTarget);
        Assert.Equal(Difficulty.Hard, question.Difficulty);
        Assert.Single(registry.Registered);
    }

    [Fact]
    public async Task NextQuestionAsync_AfterPass_IncreasesDifficultyAndSelectsUnaskedSkill()
    {
        var generator = new FakeInterviewerQuestionGenerator();
        var registry = new FakeQuestionRegistry();
        var store = new InMemoryInterviewerSessionStore();
        var agent = new InterviewerAgent(generator, registry, store);

        await agent.StartSessionAsync("session-1", "user-1", CreateProfile(Seniority.Senior));

        var question = await agent.NextQuestionAsync(
            "session-1",
            "user-1",
            Evaluation(Verdict.Pass));

        Assert.Equal("Kafka", question!.SkillTarget);
        Assert.Equal(Difficulty.Expert, question.Difficulty);
    }

    [Fact]
    public async Task NextQuestionAsync_AfterTwoFails_ChangesToNextSkill()
    {
        var generator = new FakeInterviewerQuestionGenerator();
        var registry = new FakeQuestionRegistry();
        var store = new InMemoryInterviewerSessionStore();
        var agent = new InterviewerAgent(generator, registry, store);

        await agent.StartSessionAsync("session-1", "user-1", CreateProfile(Seniority.Senior));
        var followUp = await agent.NextQuestionAsync("session-1", "user-1", Evaluation(Verdict.Fail));
        var changed = await agent.NextQuestionAsync("session-1", "user-1", Evaluation(Verdict.Fail));

        Assert.Equal("RAG", followUp!.SkillTarget);
        Assert.Equal(QuestionType.FollowUp, followUp.Type);
        Assert.Equal("Kafka", changed!.SkillTarget);
    }

    [Fact]
    public async Task StartSessionAsync_RegeneratesWhenQuestionIsDuplicate()
    {
        var generator = new FakeInterviewerQuestionGenerator(alwaysDuplicateUntilThirdAttempt: true);
        var registry = new FakeQuestionRegistry();
        registry.RejectTexts.Add(FakeInterviewerQuestionGenerator.DuplicateText);
        var store = new InMemoryInterviewerSessionStore();
        var agent = new InterviewerAgent(generator, registry, store);

        var question = await agent.StartSessionAsync("session-1", "user-1", CreateProfile(Seniority.Mid));

        Assert.NotEqual(FakeInterviewerQuestionGenerator.DuplicateText, question.Text);
        Assert.Equal(3, generator.Calls);
        Assert.Equal(2, registry.RejectedAttempts);
        Assert.Single(registry.Registered);
    }

    [Fact]
    public async Task NextQuestionAsync_ReturnsNullAtEightAskedQuestions()
    {
        var generator = new FakeInterviewerQuestionGenerator();
        var registry = new FakeQuestionRegistry();
        var store = new InMemoryInterviewerSessionStore();
        var agent = new InterviewerAgent(generator, registry, store);

        var profile = CreateProfile(Seniority.Senior);
        await store.SaveAsync(
            new InterviewerSessionState(
                "session-limit",
                "user-1",
                profile.RawTextHash,
                profile,
                Difficulty.Hard,
                DateTimeOffset.UtcNow,
                "question-8",
                "RAG",
                "¿Cómo haces RAG?",
                new Dictionary<string, int> { ["RAG"] = 8 },
                new Dictionary<string, int>()));

        var question = await agent.NextQuestionAsync(
            "session-limit",
            "user-1",
            Evaluation(Verdict.Pass));

        Assert.Null(question);
    }

    private static JobProfile CreateProfile(Seniority seniority) => new()
    {
        Role = "Backend Engineer",
        Seniority = seniority,
        RequiredSkills =
        [
            new WeightedSkill { Name = "RAG", Weight = 0.9 },
            new WeightedSkill { Name = "Kafka", Weight = 0.8 }
        ],
        Responsibilities = ["Design production AI services"],
        RawTextHash = "sha256:test-jd",
        ProfileSource = "llm"
    };

    private static EvaluationPayload Evaluation(Verdict verdict) => new()
    {
        OverallScore = verdict == Verdict.Pass ? 90 : verdict == Verdict.NeedsPractice ? 65 : 45,
        TechnicalDepth = 50,
        ProductionReadiness = 50,
        Verdict = verdict,
        CoveredKeywords = [],
        MissedConcepts = [],
        Strengths = [],
        ImprovementAdvice = "test"
    };

    private sealed class FakeInterviewerQuestionGenerator(bool alwaysDuplicateUntilThirdAttempt = false) : IInterviewerQuestionGenerator
    {
        public const string DuplicateText = "¿Cómo haces RAG resiliente?";

        public int Calls { get; private set; }

        public Task<InterviewQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken ct = default)
        {
            Calls++;

            var text = alwaysDuplicateUntilThirdAttempt && request.Attempt < 2
                ? DuplicateText
                : $"{request.SkillTarget}:{request.Difficulty}:{request.Type}:{request.Attempt}:{Guid.NewGuid():N}";

            return Task.FromResult(new InterviewQuestion
            {
                Id = Guid.NewGuid().ToString(),
                Text = text,
                SkillTarget = request.SkillTarget,
                Difficulty = request.Difficulty,
                Type = request.Type
            });
        }
    }

    private sealed class FakeQuestionRegistry : IQuestionRegistry
    {
        public List<InterviewQuestion> Registered { get; } = [];

        public HashSet<string> RejectTexts { get; } = new(StringComparer.Ordinal);

        public int RejectedAttempts { get; private set; }

        public Task<bool> WasAlreadyAskedAsync(string userId, string jdHash, string questionText, CancellationToken ct = default)
        {
            return Task.FromResult(Registered.Any(q => q.Text == questionText) || RejectTexts.Contains(questionText));
        }

        public Task<bool> RegisterAsync(string sessionId, string userId, string jdHash, InterviewQuestion question, CancellationToken ct = default)
        {
            if (RejectTexts.Contains(question.Text) || Registered.Any(q => q.Text == question.Text))
            {
                RejectedAttempts++;
                return Task.FromResult(false);
            }

            Registered.Add(question);
            return Task.FromResult(true);
        }
    }
}
