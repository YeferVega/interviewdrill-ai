using System.Diagnostics;
using System.Text;
using System.Text.Json;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Observability;
using InterviewDrill.Infrastructure.Resilience;
using Polly;

namespace InterviewDrill.Infrastructure.Llm;

/// <summary>
/// Proveedor LLM para Ollama (local o remoto).
/// Requiere configuración en appsettings.json sección "LLM:Ollama" (BaseUrl, Model).
/// Incluye resiliencia con Polly (retry, circuit breaker, timeout) y
/// trazabilidad distribuida con OpenTelemetry (ActivitySource).
/// </summary>
public sealed class OllamaLlmClient : ILlmCompletionClient
{
    private static readonly ActivitySource Activity = ActivitySourceProvider.LlmActivitySource;

    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public OllamaLlmClient(HttpClient httpClient, OllamaOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _resiliencePolicy = PollyPolicies.GetResilientPolicy();
    }

    public async Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
    {
        using var activity = Activity.StartActivity("Ollama.CompleteAsync", ActivityKind.Client);
        activity?.SetTag("llm.provider", "Ollama");
        activity?.SetTag("llm.model", _options.Model);
        activity?.SetTag("llm.temperature", request.Temperature);
        activity?.SetTag("llm.correlationId", request.CorrelationId);

        var body = new
        {
            model = _options.Model,
            prompt = $"{request.SystemPrompt}\n\n{request.UserPrompt}",
            stream = false,
            temperature = request.Temperature
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.url", "/api/generate");

        var response = await _resiliencePolicy.ExecuteAsync(
            async (ctInner) =>
            {
                var responseMsg = await _httpClient.PostAsync("/api/generate", content, ctInner);
                responseMsg.EnsureSuccessStatusCode();
                return responseMsg;
            },
            ct);

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        activity?.SetTag("http.status_code", (int)response.StatusCode);
        activity?.SetTag("http.response_size", responseBody.Length);

        using var doc = JsonDocument.Parse(responseBody);

        var result = doc.RootElement.TryGetProperty("response", out var responseText)
            ? responseText.GetString() ?? string.Empty
            : string.Empty;

        activity?.SetTag("llm.response_length", result.Length);
        activity?.Stop();

        return result;
    }
}
