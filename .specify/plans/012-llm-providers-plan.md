# Technical Plan: LLM Providers (.NET 11)

## Architecture Overview
The LLM providers are pluggable implementations of `ILlmCompletionClient` in `InterviewDrill.Infrastructure.Llm`. The provider selection is done via `appsettings.json` (`LLM:Provider`) and the DI registration in `Program.cs`.

```
┌─────────────────────────────────────────────────────────┐
│              LLM Provider Resolution                      │
├─────────────────────────────────────────────────────────┤
│ appsettings.json: LLM:Provider = "AzureOpenAI"          │
│                                         │               │
│                                         v               │
│                    ┌──────────────────────┐             │
│                    │   Program.cs DI       │             │
│                    │   switch (provider)   │             │
│                    └──────────┬───────────┘             │
│                               │                          │
│              ┌────────────────┼────────────────┐        │
│              │                │                │        │
│              v                v                v        │
│         AzureOpenAI       Bedrock            Ollama   │
│         LlmClient         LlmClient          LlmClient │
│         (HTTP + Bearer)   (SigV4 pendiente)  (HTTP)   │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. ILlmCompletionClient (`InterviewDrill.Core.Llm/ILlmCompletionClient.cs`)
**Responsibility:** Common interface for all LLM providers.
- `CompleteAsync(LlmCompletionRequest, CancellationToken)` → `Task<string>`

### 2. LlmCompletionRequest (`InterviewDrill.Core.Llm/ILlmCompletionClient.cs`)
**Responsibility:** Immutable record with LLM request parameters.
- `SystemPrompt`, `UserPrompt`, `ResponseJsonSchema?`, `Temperature`, `CorrelationId?`
- Positional constructor (no named arguments)

### 3. LlmProviderOptions (`InterviewDrill.Core.Llm/LlmProviderOptions.cs`)
**Responsibility:** Provider configuration from appsettings.json.
- `Provider`: "AzureOpenAI", "Bedrock", "Ollama", "Heuristic"
- `AzureOpenAI`, `Bedrock`, `Ollama` sub-options

### 4. AzureOpenAILlmClient (`InterviewDrill.Infrastructure.Llm/AzureOpenAILlmClient.cs`)
**Responsibility:** Client for Azure OpenAI / Azure Foundry.
- Chat Completions API with Bearer token
- Structured output with `response_format: { type: "json_object" }`
- `InvalidOperationException` if the Endpoint or DeploymentName is missing

### 5. BedrockLlmClient (`InterviewDrill.Infrastructure.Llm/BedrockLlmClient.cs`)
**Responsibility:** Skeleton for AWS Bedrock.
- `NotImplementedException` with instructions for SigV4
- Requires `Amazon.Runtime` + `Amazon.BedrockRuntime` for a real implementation

### 6. OllamaLlmClient (`InterviewDrill.Infrastructure.Llm/OllamaLlmClient.cs`)
**Responsibility:** Client for local/remote Ollama.
- Endpoint `/api/generate` with `stream: false`
- Configurable base URL and model

### 7. UnavailableLlmCompletionClient (`InterviewDrill.Infrastructure.Llm/`)
**Responsibility:** Fallback when the provider is not available.
- Returns a response indicating that the LLM is not configured

## Data Flow

```
appsettings.json (LLM:Provider) ──> Program.cs DI switch
                                           │
                                           v
                                  ILlmCompletionClient implementation
                                           │
                                  Agents call CompleteAsync()
                                           │
                                  LLM Provider response
```

## API Design (C#)

```csharp
// Interface en Core
public sealed record LlmCompletionRequest(
    string SystemPrompt,
    string UserPrompt,
    string? ResponseJsonSchema = null,
    double Temperature = 0.0,
    string? CorrelationId = null);

public interface ILlmCompletionClient
{
    Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default);
}

// Implementaciones en Infrastructure
public sealed class AzureOpenAILlmClient : ILlmCompletionClient { ... }
public sealed class BedrockLlmClient : ILlmCompletionClient { ... }
public sealed class OllamaLlmClient : ILlmCompletionClient { ... }
public sealed class UnavailableLlmCompletionClient : ILlmCompletionClient { ... }
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack |
| **HttpClient + IHttpClientFactory** | HTTP calls to LLM providers |
| **System.Text.Json** | Serialization of requests and responses |
| **Microsoft.Extensions.Configuration** | Reading of appsettings.json |
| **Microsoft.Extensions.DependencyInjection** | Registration of providers according to configuration |
| **xUnit** | Testing framework |
| **Amazon.Runtime + Amazon.BedrockRuntime** | Required for real Bedrock (pending) |

## File Structure (.NET)

```
backend/
├── src/
│   ├── InterviewDrill.Core.Llm/
│   │   ├── ILlmCompletionClient.cs
│   │   └── LlmProviderOptions.cs
│   └── InterviewDrill.Infrastructure.Llm/
│       ├── AzureOpenAILlmClient.cs
│       ├── BedrockLlmClient.cs
│       ├── OllamaLlmClient.cs
│       └── UnavailableLlmCompletionClient.cs
└── tests/
    ├── InterviewDrill.Infrastructure.Tests/
    │   └── Llm/
    │       ├── AzureOpenAILlmClientTests.cs
    │       └── OllamaLlmClientTests.cs
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| ILlmCompletionClient interface | ✅ Complete | `InterviewDrill.Core.Llm` |
| LlmCompletionRequest record | ✅ Complete | Positional constructor |
| LlmProviderOptions | ✅ Complete | `InterviewDrill.Core.Llm` |
| AzureOpenAILlmClient | ✅ Complete | HTTP + Bearer token |
| BedrockLlmClient | ✅ Complete | Skeleton with NotImplementedException |
| OllamaLlmClient | ✅ Complete | HTTP /api/generate |
| UnavailableLlmCompletionClient | ✅ Complete | Default fallback |
| DI registration in Program.cs | ✅ Complete | Switch by LLM:Provider |
| LLM section in appsettings.json | ✅ Complete | Placeholders, no secrets |
| Azure OpenAI unit tests | ✅ Complete | With fake HttpMessageHandler |
| Ollama unit tests | ✅ Complete | With fake HttpMessageHandler |
| Bedrock unit tests | ❌ Not started | Skeleton, no real implementation |

## Testing Strategy

### Unit Tests (fake HttpMessageHandler)
- **AzureOpenAILlmClientTests**: Successful response, exception when configuration is missing
- **OllamaLlmClientTests**: Successful response, empty response, BaseAddress required
- **BedrockLlmClientTests**: NotImplementedException when the implementation is missing

### Coverage Target
- All LLM providers covered with tests

## Risks

| Risk | Mitigation |
|------|------------|
| LLM credentials unavailable | `Heuristic` provider as fallback; InvalidOperationException with clear instructions |
| AWS Bedrock requires Signature V4 | Documented skeleton; instructions for adding AWS packages |
| LLM timeout | Configurable HttpClient.Timeout |
