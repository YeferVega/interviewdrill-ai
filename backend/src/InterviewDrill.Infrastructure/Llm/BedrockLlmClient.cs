using System.Diagnostics;
using System.Text;
using System.Text.Json;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Observability;
using InterviewDrill.Infrastructure.Resilience;
using Polly;

namespace InterviewDrill.Infrastructure.Llm;

/// <summary>
/// Proveedor LLM para AWS Bedrock.
/// Requiere configuración en appsettings.json sección "LLM:Bedrock" (Region, AccessKey, SecretKey, ModelId).
///
/// Nota: AWS Bedrock requiere firma AWS Signature Version 4 en cada petición.
/// Esta implementación es un esqueleto funcional: Amazon.Runtime y Amazon.BedrockRuntime
/// deben agregarse como dependencias para firmar las solicitudes. Hasta entonces,
/// lanza NotImplementedException con instrucciones claras.
/// </summary>
public sealed class BedrockLlmClient : ILlmCompletionClient
{
    private static readonly ActivitySource Activity = ActivitySourceProvider.LlmActivitySource;

    private readonly HttpClient _httpClient;
    private readonly BedrockOptions _options;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public BedrockLlmClient(HttpClient httpClient, BedrockOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        _resiliencePolicy = PollyPolicies.GetResilientPolicy();
    }

    public async Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
    {
        using var activity = Activity.StartActivity("Bedrock.CompleteAsync", ActivityKind.Client);
        activity?.SetTag("llm.provider", "Bedrock");
        activity?.SetTag("llm.model", _options.ModelId);
        activity?.SetTag("llm.region", _options.Region);
        activity?.SetTag("llm.temperature", request.Temperature);
        activity?.SetTag("llm.correlationId", request.CorrelationId);

        if (string.IsNullOrWhiteSpace(_options.ModelId))
        {
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", "Bedrock no está configurado.");
            throw new InvalidOperationException(
                "Bedrock no está configurado. Agregue 'LLM:Bedrock:ModelId' en appsettings.json.");
        }

        var url = $"https://bedrock-runtime.{_options.Region}.amazonaws.com/model/{_options.ModelId}/invoke";

        var body = new
        {
            prompt = $"Human: {request.UserPrompt}\n\nAssistant:",
            max_tokens = 4096,
            temperature = request.Temperature,
            top_p = 0.9
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.url", url);

        // NOTA: AWS Bedrock exige firma AWS Signature Version 4 en cada petición HTTP.
        // Para activar este proveedor real se deben agregar los paquetes:
        //   Amazon.Runtime  y  Amazon.BedrockRuntime
        // y firmar la petición con AWS4Signer antes de enviarla.
        activity?.SetTag("error", true);
        activity?.SetTag("error.message", "Bedrock requiere firma AWS Signature Version 4.");
        throw new NotImplementedException(
            "Bedrock requiere firma AWS Signature Version 4. " +
            "Agregue los paquetes Amazon.Runtime y Amazon.BedrockRuntime, configure credenciales " +
            "en appsettings.json (LLM:Bedrock:AccessKey, SecretKey, Region) e implemente el firmado " +
            "de la solicitud HTTP antes de usar este proveedor.");
    }
}
