using System.Net;
using InterviewDrill.Api;
using InterviewDrill.Api.Tests;
using InterviewDrill.Core.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InterviewDrill.Api.Tests.EndPoints;

public sealed class InterviewEndpointsTests : IClassFixture<HeuristicApiFactory>
{
    private readonly HttpClient _client;

    public InterviewEndpointsTests(HeuristicApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StartInterviewSession_ReturnsFirstQuestion()
    {
        var request = new StartInterviewRequest("user-start", Profile);

        var response = await _client.PostJsonAsync("/api/interview/sessions", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadJsonAsync<InterviewStartResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.SessionId));
        Assert.Equal("RAG", body.Question.SkillTarget);
        Assert.False(string.IsNullOrWhiteSpace(body.Question.Text));
    }

    [Theory]
    [InlineData("en", false)]
    [InlineData("es", true)]
    public async Task StartInterviewSession_RespectsRequestedLanguage(
        string language,
        bool expectSpanishQuestion)
    {
        var response = await _client.PostJsonAsync(
            "/api/interview/sessions",
            new StartInterviewRequest($"user-lang-{language}", Profile, language));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadJsonAsync<InterviewStartResponse>();

        Assert.NotNull(body);
        Assert.Equal(expectSpanishQuestion, body.Question.Text.Contains('¿'));
    }

    [Fact]
    public async Task SubmitAnswer_LocalizesAdvice_WhenSessionLanguageIsSpanish()
    {
        var startResponse = await _client.PostJsonAsync(
            "/api/interview/sessions",
            new StartInterviewRequest("user-lang-advice", Profile, "es"));

        startResponse.EnsureSuccessStatusCode();
        var start = await startResponse.Content.ReadJsonAsync<InterviewStartResponse>();
        Assert.NotNull(start);

        var answerResponse = await _client.PostJsonAsync(
            $"/api/interview/sessions/{start.SessionId}/answers",
            new SubmitAnswerRequest("user-lang-advice", "vaga answer about nothing relevant"));

        answerResponse.EnsureSuccessStatusCode();
        var answer = await answerResponse.Content.ReadJsonAsync<InterviewAnswerResponse>();

        Assert.NotNull(answer);
        Assert.Contains(
            "necesita",
            answer.Feedback.ImprovementAdvice,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAnswer_ReturnsFeedbackAndNextQuestion()
    {
        var startResponse = await _client.PostJsonAsync(
            "/api/interview/sessions",
            new StartInterviewRequest("user-answer", Profile));

        startResponse.EnsureSuccessStatusCode();
        var start = await startResponse.Content.ReadJsonAsync<InterviewStartResponse>();
        Assert.NotNull(start);

        var answerResponse = await _client.PostJsonAsync(
            $"/api/interview/sessions/{start.SessionId}/answers",
            new SubmitAnswerRequest(
                "user-answer",
                "Usar RAG, embeddings, HNSW, guardrails, PII, rate limiting, observabilidad y Polly."));

        Assert.Equal(HttpStatusCode.OK, answerResponse.StatusCode);

        var answer = await answerResponse.Content.ReadJsonAsync<InterviewAnswerResponse>();
        Assert.NotNull(answer);
        Assert.True(answer.Feedback.OverallScore > 0);
        Assert.NotNull(answer.Feedback.ImprovementAdvice);
        Assert.True(answer.Feedback.CoveredKeywords.Count > 0);
    }

    [Fact]
    public async Task SubmitAnswer_ReturnsBadRequest_WhenAnswerIsEmpty()
    {
        var startResponse = await _client.PostJsonAsync(
            "/api/interview/sessions",
            new StartInterviewRequest("user-empty", Profile));

        startResponse.EnsureSuccessStatusCode();
        var start = await startResponse.Content.ReadJsonAsync<InterviewStartResponse>();
        Assert.NotNull(start);

        var response = await _client.PostJsonAsync(
            $"/api/interview/sessions/{start.SessionId}/answers",
            new SubmitAnswerRequest("user-empty", "   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitAnswer_ReturnsNotFound_WhenSessionUnknown()
    {
        var response = await _client.PostJsonAsync(
            "/api/interview/sessions/unknown-session/answers",
            new SubmitAnswerRequest("user-unknown", "answer"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static JobProfile Profile => new()
    {
        Role = "Senior Backend Engineer",
        Seniority = Seniority.Senior,
        RequiredSkills =
        [
            new WeightedSkill { Name = "RAG", Weight = 0.9 },
            new WeightedSkill { Name = "Kafka", Weight = 0.8 }
        ],
        Responsibilities = ["Build production GenAI services"],
        RawTextHash = "sha256:interview-test",
        ProfileSource = "test"
    };
}
