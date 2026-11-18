using System.Net;
using InterviewDrill.Core.Llm;
using InterviewDrill.Infrastructure.Llm;

namespace InterviewDrill.Infrastructure.Tests.Llm;

public sealed class AzureOpenAILlmClientTests
{
    [Fact]
    public async Task CompleteAsync_ReturnsResponseFromAzureOpenAI()
    {
        var httpClient = new HttpClient(new FakeAzureOpenAILlmHandler("""
            {
              "id": "chatcmpl-123",
              "object": "chat.completion",
              "created": 1710000000,
              "model": "gpt-4",
              "choices": [
                {
                  "index": 0,
                  "message": {
                    "role": "assistant",
                    "content": "Respuesta de Azure OpenAI"
                  },
                  "finish_reason": "stop"
                }
              ],
              "usage": { "prompt_tokens": 10, "completion_tokens": 5, "total_tokens": 15 }
            }
            """));

        var client = new AzureOpenAILlmClient(httpClient, new AzureOpenAIOptions
        {
            Endpoint = "https://mi-recurso.openai.azure.com",
            ApiKey = "clave-fake",
            DeploymentName = "gpt-4",
            ApiVersion = "2024-02-01"
        });

        var result = await client.CompleteAsync(new LlmCompletionRequest(
            "Eres un experto en arquitectura de software",
            "¿Qué es Azure OpenAI?",
            null,
            0.7));

        Assert.Equal("Respuesta de Azure OpenAI", result);
    }

    [Fact]
    public async Task CompleteAsync_ThrowsWhenMissingConfiguration()
    {
        var httpClient = new HttpClient(new FakeAzureOpenAILlmHandler("{}"));
        var client = new AzureOpenAILlmClient(httpClient, new AzureOpenAIOptions
        {
            Endpoint = string.Empty,
            DeploymentName = string.Empty
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.CompleteAsync(new LlmCompletionRequest(
                "",
                "Pregunta",
                null,
                0.7)));
    }

    private sealed class FakeAzureOpenAILlmHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public FakeAzureOpenAILlmHandler(string responseJson)
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
