using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Llm;
using InterviewDrill.Infrastructure;
using InterviewDrill.Infrastructure.Security;

namespace InterviewDrill.Infrastructure.Tests.Agents;

public sealed class HeuristicJdParserAgentTests
{
    [Fact]
    public async Task ParseAsync_ExtractsProfileWithoutLlm()
    {
        var parser = new HeuristicJdParserAgent(new RegexPiiSanitizer());
        const string jd = """
            Senior Backend Engineer
            Responsibilities:
            - Design RAG systems using HNSW
            - Build event driven services with Kafka
            Required skills: Semantic Kernel, PostgreSQL, React
            Nice to have Kubernetes
            """;

        var profile = await parser.ParseAsync(jd);

        Assert.Equal("Senior Backend Engineer", profile.Role);
        Assert.Equal(Seniority.Senior, profile.Seniority);
        Assert.Equal("heuristic", profile.ProfileSource);
        Assert.Contains("sha256:", profile.RawTextHash);
        Assert.Contains("RAG", profile.RequiredSkills.Select(s => s.Name));
        Assert.Contains("HNSW", profile.RequiredSkills.Select(s => s.Name));
        Assert.Contains("Kafka", profile.RequiredSkills.Select(s => s.Name));
        Assert.Contains("Kubernetes", profile.NiceToHaveSkills);
        Assert.NotEmpty(profile.Responsibilities);
    }

    [Fact]
    public async Task ParseAsync_SanitizesPiiInExtractedText()
    {
        var parser = new HeuristicJdParserAgent(new RegexPiiSanitizer());
        const string jd = """
            AI Lead
            Responsibilities:
            - Design pipelines for alice@example.com
            - Build resilient services
            Required skills: RAG
            """;

        var profile = await parser.ParseAsync(jd);

        Assert.DoesNotContain("alice@example.com", string.Join(' ', profile.Responsibilities));
        Assert.Contains("[EMAIL]", string.Join(' ', profile.Responsibilities));
    }
}

public sealed class FallbackJdParserAgentTests
{
    [Fact]
    public async Task ParseAsync_FallsBackWhenLlmUnavailable()
    {
        var parser = new FallbackJdParserAgent(
            new ThrowingParser(new LlmUnavailableException()),
            new HeuristicJdParserAgent(new RegexPiiSanitizer()));

        var profile = await parser.ParseAsync("Senior Backend Engineer\nRequired skills: RAG, Kafka");

        Assert.Equal("heuristic", profile.ProfileSource);
        Assert.Contains("RAG", profile.RequiredSkills.Select(s => s.Name));
    }

    [Fact]
    public async Task ParseAsync_FallsBackWhenLlmReturnsInvalidJson()
    {
        var parser = new FallbackJdParserAgent(
            new ThrowingParser(new JdParsingException("invalid")),
            new HeuristicJdParserAgent(new RegexPiiSanitizer()));

        var profile = await parser.ParseAsync("Lead AI Engineer\nNice to have Kubernetes");

        Assert.Equal("heuristic", profile.ProfileSource);
    }

    private sealed class ThrowingParser(Exception exception) : IJdParserAgent
    {
        public Task<JobProfile> ParseAsync(string content, CancellationToken ct = default)
        {
            throw exception;
        }
    }
}
