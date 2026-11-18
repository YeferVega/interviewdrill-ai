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
/// Proveedor LLM genérico compatible con la API de OpenAI (/chat/completions).
/// Sirve para NVIDIA NIM (BaseUrl https://integrate.api.nvidia.com/v1), OpenAI, y
/// cualquier endpoint OpenAI-compatible. Incluye Polly (retry/circuit breaker/timeout)
/// y OpenTelemetry, igual que los demás clientes.
/// La API Key se provee por configuración (variable de entorno), nunca hardcodeada.
/// </summary>
public sealed class OpenAiCompatibleLlmClient : ILlmCompletionClient
{
    private static readonly ActivitySource Activity = ActivitySourceProvider.LlmActivitySource;

    private readonly HttpClient _httpClient;
    private readonly OpenAiCompatibleOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public OpenAiCompatibleLlmClient(HttpClient httpClient, OpenAiCompatibleOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _resiliencePolicy = PollyPolicies.GetResilientPolicy();
    }

    public async Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException(
                "OpenAI-compatible no está configurado. Defina 'LLM:OpenAiCompatible:BaseUrl' (ej. https://integrate.api.nvidia.com/v1).");

        if (string.IsNullOrWhiteSpace(_options.Model))
            throw new InvalidOperationException(
                "OpenAI-compatible requiere 'LLM:OpenAiCompatible:Model' (ej. meta/llama-3.1-70b-instruct).");

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "OpenAI-compatible requiere 'LLM:OpenAiCompatible:ApiKey'. Inyéctelo por variable de entorno, no lo hardcodee.");

        using var activity = Activity.StartActivity("OpenAiCompatible.CompleteAsync", ActivityKind.Client);
        activity?.SetTag("llm.provider", "OpenAICompatible");
        activity?.SetTag("llm.model", _options.Model);
        activity?.SetTag("llm.base_url", _options.BaseUrl);
        activity?.SetTag("llm.temperature", request.Temperature);
        activity?.SetTag("llm.correlationId", request.CorrelationId);

        var url = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";

        var messages = new List<object>
        {
            new { role = "system", content = request.SystemPrompt },
            new { role = "user", content = request.UserPrompt }
        };

        var body = new Dictionary<string, object>
        {
            ["model"] = _options.Model,
            ["messages"] = messages,
            ["temperature"] = request.Temperature,
            ["max_tokens"] = _options.MaxTokens ?? 4096,
        };

        if (_options.TopP is { } topP)
        {
            body["top_p"] = topP;
        }

        if (_options.EnableThinking is { } thinking)
        {
            body["chat_template_kwargs"] = new Dictionary<string, object> { ["enable_thinking"] = thinking };
        }

        if (request.ResponseJsonSchema is not null)
        {
            body["response_format"] = new { type = "json_object" };
        }

        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.url", url);

        // HttpClient reutiliza la requestMessage entre reintentos: clonamos por intento para Polly.
        HttpResponseMessage response;
        try
        {
            response = await _resiliencePolicy.ExecuteAsync(
                async (ctInner) =>
                {
                    using var attempt = Clone(requestMessage);
                    var responseMsg = await _httpClient.SendAsync(attempt, ctInner);
                    responseMsg.EnsureSuccessStatusCode();
                    return responseMsg;
                },
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Fallos del proveedor (timeout, 5xx tras reintentos, red) se mapean a
            // LlmUnavailableException para que los agentes caigan al modo heurístico.
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            throw new LlmUnavailableException(
                $"El proveedor OpenAI-compatible ({_options.BaseUrl}) no está disponible: {ex.Message}", ex);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetTag("http.response_size", responseBody.Length);

            using var doc = JsonDocument.Parse(responseBody);
            var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

            // Modelos "thinking" pueden dejar content vacío y poner la salida en reasoning_content;
            // leemos content y, si viene vacío, usamos reasoning_content.
            var contentText = message.TryGetProperty("content", out var c) ? c.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(contentText) && message.TryGetProperty("reasoning_content", out var r))
            {
                contentText = r.GetString() ?? string.Empty;
            }

            activity?.SetTag("llm.response_length", contentText.Length);
            activity?.Stop();

            return contentText;
        }
    }

    private static HttpRequestMessage Clone(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        if (original.Content is not null)
        {
            clone.Content = new StringContent(
                original.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                Encoding.UTF8,
                "application/json");
        }
        if (original.Headers.Authorization is not null)
        {
            clone.Headers.Authorization = original.Headers.Authorization;
        }
        return clone;
    }
}
