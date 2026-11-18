using System.Net;
using System.Net.Http.Json;
using InterviewDrill.Api;
using InterviewDrill.Api.Tests;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InterviewDrill.Api.Tests.EndPoints;

public sealed class InterviewHistoryEndpointTests : IClassFixture<HeuristicApiFactory>
{
    private readonly HttpClient _client;

    public InterviewHistoryEndpointTests(HeuristicApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task History_ReturnsStartedSession()
    {
        var userId = $"history-{Guid.NewGuid():N}";

        var startResponse = await _client.PostJsonAsync(
            "/api/interview/sessions",
            new StartInterviewRequest(userId, Profile()));

        startResponse.EnsureSuccessStatusCode();
        var start = await startResponse.Content.ReadJsonAsync<InterviewStartResponse>();
        Assert.NotNull(start);

        var historyResponse = await _client.GetAsync($"/api/interview/sessions?userId={Uri.EscapeDataString(userId)}");
        historyResponse.EnsureSuccessStatusCode();

        var history = await historyResponse.Content.ReadJsonAsync<InterviewSessionSummary[]>();
        Assert.NotNull(history);
        var session = Assert.Single(history);

        Assert.Equal(start.SessionId, session.SessionId);
        Assert.Equal("InProgress", session.Status);
        Assert.True(session.StartedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task History_ReturnsEmptyListForUnknownUser()
    {
        var response = await _client.GetAsync($"/api/interview/sessions?userId=unknown-{Guid.NewGuid():N}");

        response.EnsureSuccessStatusCode();
        var history = await response.Content.ReadJsonAsync<InterviewSessionSummary[]>();

        Assert.NotNull(history);
        Assert.Empty(history);
    }

    [Fact]
    public async Task StudyGuide_ReturnsNotFound_WhenGuideDoesNotExist()
    {
        var response = await _client.GetAsync("/api/interview/sessions/missing-session/study-guide");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StudyGuidePdf_ReturnsNotFound_WhenGuideDoesNotExist()
    {
        var response = await _client.GetAsync("/api/interview/sessions/missing-session/study-guide.pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static JobProfile Profile() => new()
    {
        Role = "Backend Engineer",
        Seniority = Seniority.Senior,
        RequiredSkills = [new WeightedSkill { Name = "RAG", Weight = 0.9 }],
        Responsibilities = ["Build APIs"],
        RawTextHash = $"sha256:{Guid.NewGuid():N}",
        ProfileSource = "test"
    };
}
