using System.Net;
using InterviewDrill.Core.Llm;
using InterviewDrill.Infrastructure.Llm;

namespace InterviewDrill.Infrastructure.Tests.Llm;

public sealed class OllamaLlmClientTests
{
    [Fact]
    public async Task CompleteAsync_ReturnsResponseFromOllama()
    {
        var httpClient = new HttpClient(new FakeHttpHandler("{\"response\":\"La respuesta generada por Ollama\"}"))
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        var ollama = new OllamaLlmClient(httpClient, new OllamaOptions { Model = "llama3" });

        var result = await ollama.CompleteAsync(new LlmCompletionRequest(
            "Eres un experto en arquitectura de software",
            "¿Qué es HNSW?",
            null,
            0.7));

        Assert.Equal("La respuesta generada por Ollama", result);
    }

    [Fact]
    public async Task CompleteAsync_HandlesEmptyResponse()
    {
        var httpClient = new HttpClient(new FakeHttpHandler("{\"response\":\"\"}"))
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        var ollama = new OllamaLlmClient(httpClient, new OllamaOptions { Model = "llama3" });

        var result = await ollama.CompleteAsync(new LlmCompletionRequest(
            "Pregunta",
            "",
            null,
            0.0));

        Assert.Equal(string.Empty, result);
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public FakeHttpHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson)
            });
        }
    }
}