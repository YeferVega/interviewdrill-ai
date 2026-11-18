namespace InterviewDrill.Core.Llm;

/// <summary>
/// Configuración de proveedores de LLM.
/// Se lee desde appsettings.json bajo la sección "LLM".
/// </summary>
public sealed class LlmProviderOptions
{
    /// <summary>
    /// Proveedor activo: "OpenAICompatible" (NVIDIA NIM / OpenAI), "AzureOpenAI" (Foundry),
    /// "Bedrock", "Ollama" o "Heuristic" (fallback sin LLM).
    /// </summary>
    public string Provider { get; set; } = "Heuristic";

    public OpenAiCompatibleOptions? OpenAiCompatible { get; set; }
    public AzureOpenAIOptions? AzureOpenAI { get; set; }
    public BedrockOptions? Bedrock { get; set; }
    public OllamaOptions? Ollama { get; set; }
}

/// <summary>
/// Proveedor genérico compatible con la API de OpenAI (/chat/completions).
/// Funciona con NVIDIA NIM (https://integrate.api.nvidia.com/v1), OpenAI, y cualquier
/// endpoint OpenAI-compatible. La API Key se inyecta por variable de entorno / user-secrets,
/// nunca se hardcodea (Constitución: no hardcoded secrets).
/// </summary>
public sealed class OpenAiCompatibleOptions
{
    /// <summary>Base URL sin la ruta /chat/completions. Ej.: https://integrate.api.nvidia.com/v1</summary>
    public string? BaseUrl { get; set; }

    /// <summary>API Key. Usar variable de entorno LLM__OpenAiCompatible__ApiKey (NVIDIA: nvapi-…, OpenAI: sk-…).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Modelo NVIDIA/OpenAI. Ej.: nvidia/nemotron-3-ultra-550b-a55b, openai/gpt-4o-mini, gpt-4o-mini</summary>
    public string? Model { get; set; }

    /// <summary>Techo de tokens de salida. Nemotron con "thinking" necesita un valor alto (ej. 16384). Default: 4096.</summary>
    public int? MaxTokens { get; set; }

    /// <summary>Núcleo de probabilidad (top_p). Si se define se envía en el body (ej. 0.95 para Nemotron). Default: null → no se envía.</summary>
    public double? TopP { get; set; }

    /// <summary>Habilita el modo "thinking" de Nemotron (chat_template_kwargs.enable_thinking). Default: null → no se envía.</summary>
    public bool? EnableThinking { get; set; }
}

public sealed class AzureOpenAIOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? DeploymentName { get; set; }
    public string? ApiVersion { get; set; } = "2024-02-01";
}

public sealed class BedrockOptions
{
    public string? Region { get; set; } = "us-east-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? ModelId { get; set; } = "anthropic.claude-3-sonnet-20240229-v1:0";
}

public sealed class OllamaOptions
{
    public string? BaseUrl { get; set; } = "http://localhost:11434";
    public string? Model { get; set; } = "llama3";
}
