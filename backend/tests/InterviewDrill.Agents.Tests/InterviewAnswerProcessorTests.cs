using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Agents.Tests;

public sealed class InterviewAnswerProcessorTests
{
    [Fact]
    public async Task SubmitAnswerAsync_SanitizesAnswerAndReturnsFeedbackAndNextQuestion()
    {
        var store = new InMemoryInterviewerSessionStore();
        await store.SaveAsync(CreateState());

        var sanitizer = new TrackingPiiSanitizer();
        var architecture = new FixedArchitectureEvaluator(70);
        var security = new FixedSecurityEvaluator(90);
        var answers = new FakeAnswerRepository();
        var processor = new InterviewAnswerProcessor(
            store,
            sanitizer,
            architecture,
            security,
            new ScoringSynthesizer(),
            new FixedInterviewerAgent(nextQuestion: true),
            answers,
            new StudyGuideGenerator(),
            new FakeStudyGuideRepository());

        var result = await processor.SubmitAnswerAsync(
            new AnswerSubmission("session-1", "user-1", "Contact: alice@example.com"));

        Assert.Equal(78, result.Feedback.OverallScore);
        Assert.Equal(Verdict.NeedsPractice, result.Feedback.Verdict);
        Assert.False(result.InterviewCompleted);
        Assert.NotNull(result.NextQuestion);
        Assert.Null(result.StudyGuideMarkdown);
        Assert.Contains(sanitizer.RawTexts, raw => raw.Contains("alice@example.com", StringComparison.Ordinal));
        Assert.Contains("[REDACTED]", architecture.LastResponse?.Text ?? string.Empty);
        Assert.Equal("question-1", architecture.LastResponse?.QuestionId);
        Assert.Equal("RAG", architecture.LastResponse?.SkillTarget);

        var persisted = Assert.Single(answers.Answers);
        Assert.Equal("session-1", persisted.SessionId);
        Assert.Equal("question-1", persisted.QuestionId);
        Assert.Contains("[REDACTED]", persisted.AnswerText);
        Assert.DoesNotContain("alice@example.com", persisted.AnswerText);
        Assert.Equal(78, persisted.Evaluation.OverallScore);
    }

    [Fact]
    public async Task SubmitAnswerAsync_WhenNoNextQuestion_GeneratesAndPersistsStudyGuide()
    {
        var store = new InMemoryInterviewerSessionStore();
        await store.SaveAsync(CreateState());

        var answers = new FakeAnswerRepository();
        answers.Add(sessionId: "session-1", questionId: "question-0", answerText: "old", evaluation: CreateEvaluation(55, missed: ["RAG"]));

        var guides = new FakeStudyGuideRepository();
        var processor = new InterviewAnswerProcessor(
            store,
            new PassThroughPiiSanitizer(),
            new FixedArchitectureEvaluator(40),
            new FixedSecurityEvaluator(50),
            new ScoringSynthesizer(),
            new FixedInterviewerAgent(nextQuestion: false),
            answers,
            new StudyGuideGenerator(),
            guides);

        var result = await processor.SubmitAnswerAsync(
            new AnswerSubmission("session-1", "user-1", "Respuesta técnica"));

        Assert.True(result.InterviewCompleted);
        Assert.NotNull(result.StudyGuideMarkdown);
        Assert.Contains("Guía de estudio", result.StudyGuideMarkdown);
        Assert.Contains("RAG", result.StudyGuideMarkdown);
        Assert.Equal(result.StudyGuideMarkdown, guides.Saved["session-1"]);
        Assert.NotNull(store.GetCompletedAt("session-1"));
    }

    [Fact]
    public async Task SubmitAnswerAsync_RejectsEmptyAnswer()
    {
        var store = new InMemoryInterviewerSessionStore();
        var processor = new InterviewAnswerProcessor(
            store,
            new PassThroughPiiSanitizer(),
            new FixedArchitectureEvaluator(50),
            new FixedSecurityEvaluator(50),
            new ScoringSynthesizer(),
            new FixedInterviewerAgent(nextQuestion: false),
            new FakeAnswerRepository(),
            new StudyGuideGenerator(),
            new FakeStudyGuideRepository());

        await Assert.ThrowsAsync<AnswerRequiredException>(() =>
            processor.SubmitAnswerAsync(new AnswerSubmission("missing", "user-1", "   ")));
    }

    private static InterviewerSessionState CreateState() =>
        new(
            "session-1",
            "user-1",
            "sha256:jd",
            new JobProfile
            {
                Role = "AI Engineer",
                Seniority = Seniority.Senior,
                RequiredSkills = [new WeightedSkill { Name = "RAG", Weight = 0.9 }],
                Responsibilities = ["Build production services"],
                RawTextHash = "sha256:jd",
                ProfileSource = "heuristic"
            },
            Difficulty.Hard,
            DateTimeOffset.UtcNow,
            "question-1",
            "RAG",
            "¿Cómo diseñas un pipeline RAG resiliente?",
            new Dictionary<string, int> { ["RAG"] = 1 },
            new Dictionary<string, int>());

    private static EvaluationPayload CreateEvaluation(double score, params string[] missed) =>
        new()
        {
            OverallScore = score,
            TechnicalDepth = score,
            ProductionReadiness = score,
            Verdict = score >= 80 ? Verdict.Pass : score >= 60 ? Verdict.NeedsPractice : Verdict.Fail,
            CoveredKeywords = [],
            MissedConcepts = missed,
            Strengths = ["Good"],
            ImprovementAdvice = "Practice."
        };

    private sealed class FixedArchitectureEvaluator(double score) : IArchitectureEvaluator
    {
        public CandidateResponse? LastResponse { get; private set; }

        public Task<ArchitectureEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default)
        {
            LastResponse = response;
            return Task.FromResult(new ArchitectureEvaluation
            {
                TechnicalDepth = score,
                CoveredKeywords = ["RAG"],
                MissedConcepts = [],
                Strengths = ["Concrete"],
                DetectedPatterns = ["RAG"],
                AntiPatterns = []
            });
        }
    }

    private sealed class FixedSecurityEvaluator(double score) : ISecurityRigorJudge
    {
        public Task<SecurityEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default)
        {
            return Task.FromResult(new SecurityEvaluation
            {
                ProductionReadiness = score,
                CoveredKeywords = ["Polly"],
                MissedConcepts = [],
                Strengths = ["Resilient"],
                SecurityConcerns = [],
                ResilienceScore = score
            });
        }
    }

    private sealed class FixedInterviewerAgent(bool nextQuestion) : IInterviewerAgent
    {
        public Task<InterviewQuestion> StartSessionAsync(string sessionId, string userId, JobProfile profile, CancellationToken ct = default, string? language = null)
        {
            throw new NotSupportedException();
        }

        public Task<InterviewQuestion?> NextQuestionAsync(string sessionId, string userId, EvaluationPayload lastEvaluation, CancellationToken ct = default)
        {
            return Task.FromResult<InterviewQuestion?>(nextQuestion
                ? new InterviewQuestion
                {
                    Id = "question-2",
                    Text = "next-question",
                    SkillTarget = "Kafka",
                    Difficulty = Difficulty.Expert
                }
                : null);
        }
    }

    private sealed class FakeAnswerRepository : IInterviewAnswerRepository
    {
        public List<(string SessionId, string QuestionId, string AnswerText, EvaluationPayload Evaluation)> Answers { get; } = [];

        public Task AddAnswerAsync(
            string sessionId,
            string questionId,
            string sanitizedAnswerText,
            EvaluationPayload evaluation,
            CancellationToken ct = default)
        {
            Add(sessionId, questionId, sanitizedAnswerText, evaluation);
            return Task.CompletedTask;
        }

        public void Add(string sessionId, string questionId, string answerText, EvaluationPayload evaluation)
        {
            Answers.Add((sessionId, questionId, answerText, evaluation));
        }

        public Task<IReadOnlyList<EvaluationPayload>> GetSessionEvaluationsAsync(string sessionId, CancellationToken ct = default)
        {
            IReadOnlyList<EvaluationPayload> evaluations = Answers
                .Where(answer => answer.SessionId == sessionId)
                .Select(answer => answer.Evaluation)
                .ToList();

            return Task.FromResult(evaluations);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(Answers.Count);
    }

    private sealed class FakeStudyGuideRepository : IStudyGuideRepository
    {
        public Dictionary<string, string> Saved { get; } = new(StringComparer.Ordinal);

        public Task SaveAsync(string sessionId, string markdown, CancellationToken ct = default)
        {
            Saved[sessionId] = markdown;
            return Task.CompletedTask;
        }

        public Task<string?> GetBySessionAsync(string sessionId, CancellationToken ct = default)
        {
            Saved.TryGetValue(sessionId, out var markdown);
            return Task.FromResult<string?>(markdown);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class PassThroughPiiSanitizer : IPiiSanitizer
    {
        public PiiSanitizationResult Sanitize(string? input, bool mask = true) => new(input ?? string.Empty, []);

        public void AddPattern(PiiPatternDefinition definition)
        {
        }
    }

    private sealed class TrackingPiiSanitizer : IPiiSanitizer
    {
        public List<string> RawTexts { get; } = [];

        public PiiSanitizationResult Sanitize(string? input, bool mask = true)
        {
            var raw = input ?? string.Empty;
            RawTexts.Add(raw);
            return new PiiSanitizationResult(raw.Replace("alice@example.com", "[REDACTED]", StringComparison.Ordinal), []);
        }

        public void AddPattern(PiiPatternDefinition definition)
        {
        }
    }
}
