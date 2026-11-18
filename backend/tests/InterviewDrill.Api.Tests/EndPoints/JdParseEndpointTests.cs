using System.Net;
using System.Net.Http.Headers;
using System.Text;
using InterviewDrill.Core.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InterviewDrill.Api.Tests.EndPoints;

public sealed class JdParseEndpointTests : IClassFixture<HeuristicApiFactory>
{
    private readonly HttpClient _client;

    public JdParseEndpointTests(HeuristicApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Parse_ReturnsJobProfile_WhenTextIsPosted()
    {
        var content = new StringContent(
            """
            {
              "text": "Senior Backend Engineer\nResponsibilities:\n- Design RAG systems using HNSW\nRequired skills: Kafka, PostgreSQL, React\nNice to have Kubernetes"
            }
            """,
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/jd/parse", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadJsonAsync<JobProfile>();
        Assert.NotNull(profile);
        Assert.Equal("Senior Backend Engineer", profile.Role);
        Assert.Equal(Seniority.Senior, profile.Seniority);
        Assert.Contains("RAG", profile.RequiredSkills.Select(s => s.Name));
        Assert.Contains("Kafka", profile.RequiredSkills.Select(s => s.Name));
        Assert.Contains("heuristic", profile.ProfileSource);
        Assert.Contains("sha256:", profile.RawTextHash);
    }

    [Fact]
    public async Task Parse_ReturnsJobProfile_WhenTxtFileIsUploaded()
    {
        using var multipart = new MultipartFormDataContent();
        var fileContent = new StringContent(
            "Lead AI Engineer\nRequired skills: RAG, Kafka, PostgreSQL\nNice to have Kubernetes",
            Encoding.UTF8,
            "text/plain")
        {
            Headers = { ContentType = new MediaTypeHeaderValue("text/plain") }
        };

        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"file\"",
            FileName = "\"job.txt\""
        };

        multipart.Add(fileContent);

        var response = await _client.PostAsync("/api/jd/parse", multipart);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadJsonAsync<JobProfile>();
        Assert.NotNull(profile);
        Assert.Equal("Lead AI Engineer", profile.Role);
        Assert.Equal(Seniority.Lead, profile.Seniority);
    }

    [Fact]
    public async Task Parse_ReturnsBadRequest_WhenUnsupportedFileIsUploaded()
    {
        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);

        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"file\"",
            FileName = "\"job.exe\""
        };

        multipart.Add(fileContent);

        var response = await _client.PostAsync("/api/jd/parse", multipart);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Parse_ReturnsBadRequest_WhenBodyIsEmpty()
    {
        var content = new StringContent("{\"text\":\"\"}", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/jd/parse", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
