using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Llm;
using InterviewDrill.Infrastructure;
using InterviewDrill.Infrastructure.Security;

namespace InterviewDrill.Infrastructure.Tests.Agents;

public sealed class LlmJdParserAgentTests
{
    [Fact]
    public async Task ParseAsync_ReturnsStructuredJobProfile()
    {
        var llm = new FakeLlmClient(ValidJson);
        var parser = new LlmJdParserAgent(new RegexPiiSanitizer(), llm);

        var profile = await parser.ParseAsync("Senior Backend Engineer, email alice@example.com");

        Assert.Equal("Senior Backend Engineer", profile.Role);
        Assert.Equal(Seniority.Senior, profile.Seniority);
        Assert.Equal(2, profile.RequiredSkills.Count);
        Assert.Equal("RAG", profile.RequiredSkills[0].Name);
        Assert.Equal(0.9, profile.RequiredSkills[0].Weight);
        Assert.Equal("sha256:", profile.RawTextHash[..7]);
        Assert.Equal("llm", profile.ProfileSource);
    }

    [Fact]
    public async Task ParseAsync_ComputesStableRawTextHashRegardlessWhitespace()
    {
        var llm = new FakeLlmClient(ValidJson);
        var parser = new LlmJdParserAgent(new RegexPiiSanitizer(), llm);

        var first = await parser.ParseAsync("Senior   Backend\nEngineer");
        var second = await parser.ParseAsync("  senior backend    engineer  ");

        Assert.Equal(first.RawTextHash, second.RawTextHash);
    }

    [Fact]
    public async Task ParseAsync_SanitizesPiiBeforeCallingLlm()
    {
        var llm = new FakeLlmClient("""
            {"role":"AI Engineer","seniority":"mid","requiredSkills":[],"niceToHaveSkills":[],"responsibilities":[]}
            """);
        var parser = new LlmJdParserAgent(new RegexPiiSanitizer(), llm);

        await parser.ParseAsync("Contact alice@example.com +1 555-123-4567");

        Assert.Contains("[EMAIL]", llm.LastUserPrompt);
        Assert.DoesNotContain("alice@example.com", llm.LastUserPrompt);
        Assert.DoesNotContain("555-123-4567", llm.LastUserPrompt);
        Assert.Equal(0, llm.LastTemperature);
    }

    [Fact]
    public async Task ParseAsync_RejectsInvalidJson()
    {
        var parser = new LlmJdParserAgent(new RegexPiiSanitizer(), new FakeLlmClient("not json"));

        await Assert.ThrowsAsync<JdParsingException>(() => parser.ParseAsync("Senior engineer"));
    }

    [Fact]
    public async Task ParseAsync_RejectsEmptyJd()
    {
        var parser = new LlmJdParserAgent(new RegexPiiSanitizer(), new FakeLlmClient("{}"));

        await Assert.ThrowsAsync<JdParsingException>(() => parser.ParseAsync("   "));
    }

    private const string ValidJson = """
        {
          "role": "Senior Backend Engineer",
          "seniority": "senior",
          "requiredSkills": [{"name":"RAG","weight":0.9},{"name":"Kafka","weight":0.8}],
          "niceToHaveSkills": ["Kubernetes"],
          "responsibilities": ["Own production services"]
        }
        """;

    private sealed class FakeLlmClient(string response) : ILlmCompletionClient
    {
        public string LastUserPrompt { get; private set; } = string.Empty;
        public double LastTemperature { get; private set; } = double.NaN;

        public Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
        {
            LastUserPrompt = request.UserPrompt;
            LastTemperature = request.Temperature;
            return Task.FromResult(response);
        }
    }
}
