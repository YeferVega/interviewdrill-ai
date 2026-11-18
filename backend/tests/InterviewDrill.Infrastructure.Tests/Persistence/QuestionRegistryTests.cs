using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Vectors;
using InterviewDrill.Infrastructure.Persistence;
using InterviewDrill.Infrastructure.Persistence.Entities;
using InterviewDrill.Infrastructure.Security;

namespace InterviewDrill.Infrastructure.Tests.Persistence;

public sealed class QuestionRegistryTests
{
    [Fact]
    public void ComputeNormalizedHash_IsCaseAndWhitespaceInsensitive()
    {
        var first = QuestionHashing.ComputeNormalizedHash("  ¿Cómo manejas HTTP 429? ");
        var second = QuestionHashing.ComputeNormalizedHash("¿cómo    manejas http 429?");

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task RegisterAsync_StoresFirstQuestionAndRejectsExactDuplicate()
    {
        var repository = new FakeQuestionRepository();
        var generator = new FakeEmbeddingGenerator();
        var registry = CreateRegistry(repository, generator);
        var question = CreateQuestion();

        var first = await registry.RegisterAsync("session-1", "user-1", "sha256:jd", question);
        var duplicate = await registry.RegisterAsync("session-2", "user-1", "sha256:jd", CreateQuestion());

        Assert.True(first);
        Assert.False(duplicate);
        Assert.Single(repository.Questions);
    }

    [Fact]
    public async Task RegisterAsync_SanitizesQuestionBeforePersistingAndEmbedding()
    {
        var repository = new FakeQuestionRepository();
        var generator = new FakeEmbeddingGenerator();
        var registry = CreateRegistry(repository, generator);

        var registered = await registry.RegisterAsync(
            "session-1",
            "user-1",
            "sha256:jd",
            CreateQuestion("¿Cómo manejas la cuota para alice@example.com?"));

        Assert.True(registered);
        var question = Assert.Single(repository.Questions);
        Assert.Contains("[EMAIL]", question.Text);
        Assert.DoesNotContain("alice@example.com", question.Text);
        Assert.NotNull(generator.LastGeneratedText);
        Assert.Contains("[EMAIL]", generator.LastGeneratedText);
        Assert.DoesNotContain("alice@example.com", generator.LastGeneratedText);
    }

    [Fact]
    public async Task RegisterAsync_RejectsSemanticParaphraseForSameUserAndJd()
    {
        var repository = new FakeQuestionRepository
        {
            SemanticDuplicateExists = true
        };
        var generator = new FakeEmbeddingGenerator();
        var registry = CreateRegistry(repository, generator);

        var registered = await registry.RegisterAsync(
            "session-1",
            "user-1",
            "sha256:jd",
            CreateQuestion());

        Assert.False(registered);
        Assert.Empty(repository.Questions);
        Assert.True(generator.Calls > 0);
    }

    [Fact]
    public async Task WasAlreadyAskedAsync_UsesSemanticCheckAfterExactMiss()
    {
        var repository = new FakeQuestionRepository
        {
            SemanticDuplicateExists = true
        };
        var registry = CreateRegistry(repository, new FakeEmbeddingGenerator());

        var asked = await registry.WasAlreadyAskedAsync(
            "user-1",
            "sha256:jd",
            "¿Cómo controlas la cuota de la API?");

        Assert.True(asked);
    }

    [Fact]
    public async Task RegisterAsync_DoesNotDeduplicateAcrossUsers()
    {
        var repository = new FakeQuestionRepository();
        var registry = CreateRegistry(repository, new FakeEmbeddingGenerator());
        var question = CreateQuestion();

        await registry.RegisterAsync("session-1", "user-1", "sha256:jd", question);
        var secondUser = await registry.RegisterAsync("session-2", "user-2", "sha256:jd", question);

        Assert.True(secondUser);
        Assert.Equal(2, repository.Questions.Count);
    }

    private static QuestionRegistry CreateRegistry(FakeQuestionRepository repository, FakeEmbeddingGenerator generator)
    {
        return new QuestionRegistry(repository, generator, new RegexPiiSanitizer());
    }

    private static InterviewQuestion CreateQuestion(string text = "¿Cómo manejas HTTP 429?") =>
        new()
        {
            Id = "question-1",
            Text = text,
            SkillTarget = "Resiliencia",
            Difficulty = Difficulty.Medium,
            Type = QuestionType.FailureScenario
        };

    private sealed class FakeQuestionRepository : IQuestionRepository
    {
        public List<QuestionEntity> Questions { get; } = [];

        public bool SemanticDuplicateExists { get; init; }

        public Task<bool> ExistsNormalizedHashAsync(
            string userId,
            string jobDescriptionHash,
            string normalizedQuestionHash,
            CancellationToken ct = default)
        {
            return Task.FromResult(Questions.Any(q =>
                q.UserId == userId
                && q.JobDescriptionHash == jobDescriptionHash
                && q.NormalizedQuestionHash == normalizedQuestionHash));
        }

        public Task<bool> ExistsSemanticDuplicateAsync(
            string userId,
            string jobDescriptionHash,
            float[] embedding,
            double similarityThreshold = VectorMath.SemanticDuplicateThreshold,
            CancellationToken ct = default)
        {
            Assert.Equal(VectorMath.SemanticDuplicateThreshold, similarityThreshold);

            return Task.FromResult(SemanticDuplicateExists);
        }

        public Task AddAsync(QuestionEntity question, CancellationToken ct = default)
        {
            Questions.Add(question);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        public int Calls { get; private set; }
        public string? LastGeneratedText { get; private set; }

        public Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
        {
            Calls++;
            LastGeneratedText = text;
            return Task.FromResult(new float[] { 1f, 0f });
        }
    }
}
