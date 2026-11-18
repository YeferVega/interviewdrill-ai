using System.Net;
using InterviewDrill.Core.Llm;
using InterviewDrill.Infrastructure.Llm;

namespace InterviewDrill.Infrastructure.Tests.Llm;

public sealed class OpenAiCompatibleLlmClientTests
{
    private static FakeHandler RunOpenAiCompatible(
        OpenAiCompatibleOptions options,
        LlmCompletionRequest request,
        string responseJson = """{"choices":[{"message":{"content":"x"}}]}""")
    {
        var handler = new FakeHandler(responseJson);
        var client = new OpenAiCompatibleLlmClient(new HttpClient(handler), options);
        // Invocar CompleteAsync deja el handler con el body capturado.
        try { client.CompleteAsync(request).GetAwaiter().GetResult(); }
        catch { /* los casos que lanzan InvalidOperationException se validan aparte */ }
        return handler;
    }

    [Fact]
    public async Task CompleteAsync_ReturnsContentFromEndpoint()
    {
        var handler = new FakeHandler("""
            {"choices":[{"message":{"role":"assistant","content":"Hola desde NVIDIA NIM"},"finish_reason":"stop"}],
             "usage":{"prompt_tokens":8,"completion_tokens":6,"total_tokens":14}}
            """);
        var client = new OpenAiCompatibleLlmClient(new HttpClient(handler), new OpenAiCompatibleOptions
        {
            BaseUrl = "https://integrate.api.nvidia.com/v1",
            ApiKey = "nvapi-fake",
            Model = "nvidia/nemotron-3.5-lightning-30b-a3b"
        });

        var result = await client.CompleteAsync(new LlmCompletionRequest("sys", "¿Qué es NVIDIA NIM?"));

        Assert.Equal("Hola desde NVIDIA NIM", result);
        Assert.Equal("https://integrate.api.nvidia.com/v1/chat/completions", handler.LastUrl);
        Assert.Equal("Bearer", handler.LastAuthScheme);
        Assert.Equal("nvapi-fake", handler.LastAuthParameter);
    }

    [Fact]
    public void Body_IncludesModelSlugAndDefaults()
    {
        var handler = RunOpenAiCompatible(
            new OpenAiCompatibleOptions
            {
                BaseUrl = "https://integrate.api.nvidia.com/v1",
                ApiKey = "nvapi-fake",
                Model = "nvidia/nemotron-3.5-lightning-30b-a3b"
            },
            new LlmCompletionRequest("sys", "user"));

        Assert.Contains("nvidia/nemotron-3.5-lightning-30b-a3b", handler.LastBody);
        Assert.Contains("4096", handler.LastBody);
        Assert.DoesNotContain("top_p", handler.LastBody);
        Assert.DoesNotContain("chat_template_kwargs", handler.LastBody);
    }

    [Fact]
    public void Body_IncludesTopPMaxTokensAndThinkingWhenConfigured()
    {
        var handler = RunOpenAiCompatible(
            new OpenAiCompatibleOptions
            {
                BaseUrl = "https://integrate.api.nvidia.com/v1",
                ApiKey = "nvapi-fake",
                Model = "nvidia/nemotron-3.5-lightning-30b-a3b",
                TopP = 0.95,
                MaxTokens = 16384,
                EnableThinking = true
            },
            new LlmCompletionRequest("sys", "user", null, 1.0));

        Assert.Contains("16384", handler.LastBody);
        Assert.Contains("top_p", handler.LastBody);
        Assert.Contains("chat_template_kwargs", handler.LastBody);
        Assert.Contains("enable_thinking", handler.LastBody);
    }

    [Fact]
    public async Task CompleteAsync_ReadsReasoningContentWhenContentIsEmpty()
    {
        var handler = new FakeHandler("""{"choices":[{"message":{"content":"","reasoning_content":"La respuesta es 42"}}]}""");
        var client = new OpenAiCompatibleLlmClient(new HttpClient(handler), new OpenAiCompatibleOptions
        {
            BaseUrl = "https://integrate.api.nvidia.com/v1",
            ApiKey = "nvapi-fake",
            Model = "nvidia/nemotron-3.5-lightning-30b-a3b"
        });

        var result = await client.CompleteAsync(new LlmCompletionRequest("sys", "user"));

        Assert.Equal("La respuesta es 42", result);
    }

    [Theory]
    [InlineData(null, "model-x", "key-x")]
    [InlineData("https://x/v1", null, "key-x")]
    [InlineData("https://x/v1", "model-x", null)]
    [InlineData("https://x/v1", "model-x", "   ")]
    public async Task CompleteAsync_MissingConfiguration_Throws(string? baseUrl, string? model, string? apiKey)
    {
        var client = new OpenAiCompatibleLlmClient(
            new HttpClient(new FakeHandler("{}")),
            new OpenAiCompatibleOptions { BaseUrl = baseUrl, Model = model, ApiKey = apiKey });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.CompleteAsync(new LlmCompletionRequest("sys", "user")));
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public FakeHandler(string responseJson) => _responseJson = responseJson;

        public string? LastBody { get; private set; }
        public string? LastUrl { get; private set; }
        public string? LastAuthScheme { get; private set; }
        public string? LastAuthParameter { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUrl = request.RequestUri?.ToString();
            LastAuthScheme = request.Headers.Authorization?.Scheme;
            LastAuthParameter = request.Headers.Authorization?.Parameter;
            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson)
            };
        }
    }
}
