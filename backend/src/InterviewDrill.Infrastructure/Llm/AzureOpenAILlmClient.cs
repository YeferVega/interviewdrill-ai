using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Observability;
using InterviewDrill.Infrastructure.Resilience;
using Polly;

namespace InterviewDrill.Infrastructure.Llm;

/// <summary>
/// Proveedor LLM para Azure OpenAI / Azure OpenAI Foundry.
/// Lee configuración de appsettings.json sección "LLM:AzureOpenAI".
/// Incluye resiliencia con Polly (retry, circuit breaker, timeout) y
/// trazabilidad distribuida con OpenTelemetry (ActivitySource).
/// </summary>
public sealed class AzureOpenAILlmClient : ILlmCompletionClient
{
    private static readonly ActivitySource Activity = ActivitySourceProvider.LlmActivitySource;

    private readonly HttpClient _httpClient;
    private readonly AzureOpenAIOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public AzureOpenAILlmClient(HttpClient httpClient, AzureOpenAIOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _resiliencePolicy = PollyPolicies.GetResilientPolicy();
    }

    public async Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new InvalidOperationException(
                "Azure OpenAI no está configurado. Agregue 'LLM:AzureOpenAI:Endpoint' en appsettings.json.");

        if (string.IsNullOrWhiteSpace(_options.DeploymentName))
            throw new InvalidOperationException(
                "Azure OpenAI requiere 'LLM:AzureOpenAI:DeploymentName' en appsettings.json.");

        using var activity = Activity.StartActivity("AzureOpenAI.CompleteAsync", ActivityKind.Client);
        activity?.SetTag("llm.provider", "AzureOpenAI");
        activity?.SetTag("llm.deployment", _options.DeploymentName);
        activity?.SetTag("llm.temperature", request.Temperature);
        activity?.SetTag("llm.correlationId", request.CorrelationId);

        var url = $"{_options.Endpoint.TrimEnd('/')}/openai/deployments/{_options.DeploymentName}/chat/completions?api-version={_options.ApiVersion ?? "2024-02-01"}";

        var messages = new List<object>
        {
            new { role = "system", content = request.SystemPrompt },
            new { role = "user", content = request.UserPrompt }
        };

        var body = new
        {
            messages,
            temperature = request.Temperature,
            max_tokens = 4096,
            response_format = request.ResponseJsonSchema is not null ? new { type = "json_object" } : null
        };

        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.url", url);

        var response = await _resiliencePolicy.ExecuteAsync(
            async (ctInner) =>
            {
                var responseMsg = await _httpClient.SendAsync(requestMessage, ctInner);
                responseMsg.EnsureSuccessStatusCode();
                return responseMsg;
            },
            ct);

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        activity?.SetTag("http.status_code", (int)response.StatusCode);
        activity?.SetTag("http.response_size", responseBody.Length);

        using var doc = JsonDocument.Parse(responseBody);

        var contentText = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

        activity?.SetTag("llm.response_length", contentText.Length);
        activity?.Stop();

        return contentText;
    }
}
