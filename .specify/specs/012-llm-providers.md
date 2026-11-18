# Specification 012: LLM Providers (NVIDIA NIM / OpenAI / OpenCode, Azure Foundry, Bedrock, Ollama)

## Overview
Pluggable LLM providers for the InterviewDrill AI system. They allow using an **OpenAI-compatible endpoint (NVIDIA NIM, OpenAI, OpenCode)**, Azure OpenAI / Azure Foundry, AWS Bedrock, or Ollama as the inference backend, configurable via `appsettings.json` under the `LLM` section. No hardcoded API keys; credentials via environment variables / user-secrets.

## Stack
- **.NET 11 / C# 15** (implemented in `InterviewDrill.Infrastructure.Llm`)
- **Interface in `InterviewDrill.Core.Llm`**: `ILlmCompletionClient`, `LlmCompletionRequest`, `LlmProviderOptions`
- **HttpClient + HttpClientFactory** for HTTP calls to providers
- **Polly** for resilience (retry, circuit breaker, timeout)
- **OpenTelemetry/ActivitySource** for distributed tracing
- **appsettings.json** for provider and credential configuration

## User Stories
- **As a** developer, **I want** to configure the LLM provider via `appsettings.json` (`LLM:Provider`), **so that** I can use NVIDIA NIM, OpenAI, OpenCode, Azure OpenAI, Bedrock, or Ollama without modifying code.
- **As a** developer, **I want** no hardcoded API keys, **so that** the project is secure and production-ready.
- **As a** developer, **I want** a heuristic fallback when no LLM is available, **so that** the system works in offline/demo mode.
- **As an** engineer, **I want** to pass `max_tokens`/`top_p`/`enable_thinking` for reasoning models (Nemotron) and read `reasoning_content` if `content` comes back empty.

## Functional Requirements

### FR1: Common interface `ILlmCompletionClient`
- Method `CompleteAsync(LlmCompletionRequest, CancellationToken)` → `Task<string>`
- Implemented by: `OpenAiCompatibleLlmClient` (NVIDIA/OpenAI/OpenCode), `AzureOpenAILlmClient`, `BedrockLlmClient`, `OllamaLlmClient`, `UnavailableLlmCompletionClient` (fallback)

### FR2: Configuration via appsettings.json
- Section `LLM:Provider`: `"OpenAICompatible" | "Nvidia" | "NVIDIA" | "OpenAI" | "OpenCode" | "azure..." etc.` (see aliases per FR)
- Section `LLM:OpenAiCompatible`: `BaseUrl`, `ApiKey` (env), `Model`, `MaxTokens?`, `TopP?`, `EnableThinking?`
- Section `LLM:AzureOpenAI`: Endpoint, ApiKey, DeploymentName, ApiVersion
- Section `LLM:Bedrock`: Region, AccessKey, SecretKey, ModelId
- Section `LLM:Ollama`: BaseUrl, Model

### FR2b: OpenAI-compatible provider (NVIDIA NIM / OpenAI)
- A single client `OpenAiCompatibleLlmClient` covers **NVIDIA NIM** (`BaseUrl=https://integrate.api.nvidia.com/v1`) and **OpenAI** (`BaseUrl=https://api.openai.com/v1`), because both speak `/chat/completions`.
- Body: `{model, messages[system+user], temperature, max_tokens, top_p?, chat_template_kwargs.enable_thinking?, response_format?}`.
  - `MaxTokens` (default 4096; Nemotron thinking → 16384), `TopP` optional (e.g. 0.95), `EnableThinking` optional → `chat_template_kwargs`.
  - `response_format: { type: "json_object" }` when the `LlmCompletionRequest` carries a schema (judges with anchored rubric).
- If `content` comes back empty (thinking models), `reasoning_content` is read as a fallback.
- `Provider` accepts the aliases `"OpenAICompatible" | "Nvidia" | "NVIDIA" | "OpenAI"`.
- Script `scripts/llm-check.ps1 -Provider Nvidia|OpenAI` to validate connectivity locally without starting the app (reads the key from `NVIDIA_API_KEY` / `OPENAI_API_KEY`). For NVIDIA it sends `max_tokens=16384, top_p=0.95, enable_thinking=true` replicating NVIDIA's official example.
- ⚠️ NIM API Catalog model IDs are **lowercase with hyphens** (e.g. `nvidia/nemotron-3.5-lightning-30b-a3b`, `nvidia/nemotron-3.5-super-vl-120b-a12b`; not PascalCase).

### FR2d: Base model and error handling (OpenAI-compatible)
- **Base model in Development**: `nvidia/nemotron-3.5-super-vl-120b-a12b` with `MaxTokens=16384, TopP=0.95, EnableThinking=true`.
- **ApiKey outside the repo**: stored in **user-secrets** (`dotnet user-secrets set "LLM:OpenAICompatible:ApiKey" ...` with `UserSecretsId` in the csproj) or via env `NVIDIA_API_KEY` (the `switch` in `Program.cs` also resolves the key via `NVIDIA_API_KEY`/`OPENAI_API_KEY`/`OPENCODE_API_KEY` if the config value is empty). `appsettings.json`/`.Development.json` keep `ApiKey: ""`.
- **Consistent config**: `llmProvider` is read with `builder.Configuration["LLM:Provider"]` so that env and files are respected (the `GetSection().GetValue` in the switch did not see the override).
- **Provider errors → fallback**: timeouts/5xx/invalid parsing are mapped to `LlmUnavailableException`; the Interviewer/JD/evaluator wrappers degrade to deterministic heuristics instead of returning 500.
- **Deterministic tests**: `HeuristicApiFactory` (env `Testing` + override `LLM:Provider=Heuristic`) prevents `WebApplicationFactory` from loading user-secrets/keys and making real calls.

### FR2c: OpenCode provider (OpenAI-compatible gateway)
- `BaseUrl=https://opencode.ai/inference/openai/v1`, `ApiKey` via env `OPENCODE_API_KEY` (or alias `LLM__OpenAiCompatible__ApiKey`), models `glm-5.3-flash-free`, `nemotron-3.5-lightning-free`, `deepseek-v4.1-flash-free`, `minimax-m3.1-flash-free`, etc. (paid: full Go models).
- Provider alias in `LLM:Provider`: `"OpenCode"`.
- ⚠️ The `*-free` models have a **daily quota limit**; external API clients with sustained usage require **OpenCode Go** (paid plan). The user's key works and authenticates; `429 FreeUsageLimitError` was returned once the free-tier was exhausted.

### FR3: Azure OpenAI / Azure Foundry provider
- Endpoint, API Key, Deployment Name, API Version
- Chat Completions API: `/openai/deployments/{deployment}/chat/completions?api-version={version}`
- Bearer token authentication
- Structured output with `response_format: { type: "json_object" }`
- Polly + OpenTelemetry integrated

### FR4: AWS Bedrock provider
- Region, Access Key, Secret Key, Model Id
- AWS Signature Version 4 signing (requires `Amazon.Runtime` + `Amazon.BedrockRuntime`)
- Functional skeleton with `NotImplementedException` until dependencies and signing are added
- Polly + OpenTelemetry integrated (in skeleton)

### FR5: Ollama provider
- Base URL and model
- Endpoint `/api/generate` with `stream: false`
- Supports configurable temperature
- Polly + OpenTelemetry integrated

### FR6: Heuristic provider (fallback)
- Does not call any external LLM
- Returns a predefined heuristic response
- Used when `LLM:Provider = "Heuristic"` or when no credentials exist

### FR7: Error handling
- `InvalidOperationException` with clear instructions when credentials are missing
- Configurable timeout on HTTP requests (30 seconds)
- Retry with exponential backoff, circuit breaker, and HTTP 429 handling via Polly

## Non-Functional Requirements

- **Security**: No hardcoded API keys; empty placeholders in appsettings.json
- **Reliability**: Fallback to Heuristic when the configured provider is unavailable
- **Performance**: HTTP request timeout to avoid indefinite blocking
- **Testability**: Tests with a fake `HttpMessageHandler` (never call real services)

## User Stories (detailed)

- **As a** developer, **I want** `LLM:Provider = "OpenAICompatible"` + `nvidia/...` or `gpt-4o-mini`, **so that** I use NIM/OpenAI via a single client.
- **As a** developer, **I want** `LLM:Provider = "OpenCode"` + `*-free` models, **so that** I take advantage of the OpenCode free tier (with Go for sustained usage).
- **As a** developer, **I want** `LLM:Provider = "AzureOpenAI"` and to configure `LLM:AzureOpenAI:*`, **so that** I use Foundry.
- **As a** developer, **I want** `LLM:Provider = "Bedrock"` and AWS credentials, **so that** I use Bedrock (once SigV4 is implemented).
- **As a** developer, **I want** `LLM:Provider = "Ollama"`, **so that** I use local Ollama at no cost.
- **As a** developer, **I want** `LLM:Provider = "Heuristic"`, **so that** it works offline/demo.

## Acceptance Criteria

- [x] `ILlmCompletionClient` defined in `InterviewDrill.Core.Llm`
- [x] `OpenAiCompatibleLlmClient` (NVIDIA NIM / OpenAI / OpenCode) implemented + DI with aliases `Nvidia`/`OpenAI`/`OpenAICompatible`/`OpenCode`
- [x] Body supports `MaxTokens`/`TopP`/`EnableThinking` (chat_template_kwargs) and `reasoning_content` fallback
- [x] `appsettings.json` with `LLM:OpenAiCompatible` (BaseUrl placeholder, empty ApiKey)
- [x] `LLM:AzureOpenAI`, `LLM:Bedrock`, `LLM:Ollama` available
- [x] Script `scripts/llm-check.ps1` supports `-Provider Nvidia|OpenAI|OpenCode|Custom`
- [x] DI registration in `Program.cs` according to `LLM:Provider`
- [x] `UnavailableLlmCompletionClient` as default fallback (Heuristic)
- [x] No hardcoded secrets; empty placeholders
- [x] Unit tests with a fake `HttpMessageHandler` for OpenAI-compatible (incl. thinking/top_p body, reasoning_content), Azure OpenAI, and Ollama
- [x] Unit tests for `InvalidOperationException` when credentials are missing (BaseUrl/Model/ApiKey)
- [x] Default base model: `nvidia/nemotron-3.5-super-vl-120b-a12b` (base + Development); `ApiKey` in **user-secrets** or env, never in the repo
- [x] `switch` reads `builder.Configuration["LLM:Provider"]` to respect env + files; key fallback via `NVIDIA_API_KEY`/`OPENAI_API_KEY`/`OPENCODE_API_KEY`
- [x] Provider failures (timeout/5xx/parse) mapped to `LlmUnavailableException` → deterministic heuristic degradation (no 500)
- [x] `HeuristicApiFactory` forces `LLM:Provider=Heuristic` in `InterviewDrill.Api.Tests` (deterministic suite even when user-secrets exist)

## Connectivity and real-model validation (finding 2026-11-16)
- **NVIDIA `nvapi-…`**: `/api/jd/parse` with `LLM:Provider=OpenAICompatible` + `nvidia/nemotron-3.5-super-vl-120b-a12b` returned `profileSource=llm`, role="Senior Backend Engineer", skills=[RAG,Kafka] in ~7-8s. **HTTP 200** confirmed. The NIM key authenticates. PascalCase model IDs returned 404 page not found; retired variants returned `410 Gone`.
- **Full E2E with a real model**: `scripts/e2e-smoke.ps1 -WithLlm` (SuperVL, `thinking=OFF, max_tokens=4096`) passed end to end with 0 question overlap. With `thinking=ON` (16384 tokens) the judges are too slow for the automated gate.
- **`llmProvider` config**: the original `switch` used `GetSection("LLM:Provider").GetValue<string>`, which in the child `dotnet run` did not see the Development override; it was changed to `builder.Configuration["LLM:Provider"]`.
- **Timeouts/providers** (30s): if NIM is slow or returns 5xx, the client now throws `LlmUnavailableException` → `FallbackJdParserAgent`/`Interviewer` degrade to deterministic heuristics (previously it blew up with a 500).
- **OpenCode `oc_sk_…`**: authenticates, but the free tier returns `429 FreeUsageLimitError` without a Go subscription (the `*-free` models are intended for the paid Go plan).
- **OpenAI `sk-proj-…`**: `429 insufficient_quota` (no balance). The format is not NVIDIA's (`nvapi-…`).
- 🔴 The keys were shared in plain text → **rotate every key**. Only empty placeholders remain in the repo; the real key goes to user-secrets or env.
- **Tests**: `HeuristicApiFactory` (env `Testing`) prevents `dotnet test` from using real keys (the default `WebApplicationFactory<Program>` loaded user-secrets and made network calls, making the suite non-deterministic).

## Out of Scope

- Real AWS Signature Version 4 signing (requires `Amazon.Runtime` + `Amazon.BedrockRuntime`)
- Azure AD / Managed Identity authentication (although DEPLOYMENT documents it for DB)
- Streaming of LLM responses (our clients use full non-stream responses)

## Dependencies

- **InterviewDrill.Core.Llm**: `ILlmCompletionClient`, `LlmCompletionRequest`, `LlmProviderOptions`
- **InterviewDrill.Core.Security**: `IPiiSanitizer` (sanitize before sending to the LLM)
- **System.Net.Http**: HttpClient, HttpClientFactory
- **Microsoft.Extensions.Configuration**: appsettings.json
- **Polly**: resilience in LLM clients
- **OpenTelemetry**: tracing of LLM calls

## Risks

| Risk | Mitigation |
|------|------------|
| LLM credentials unavailable | `Heuristic` provider as offline fallback; clear missing-configuration messages |
| Incorrect NVIDIA model IDs (410/404) | Use lowercase IDs with hyphens; validate with `scripts/llm-check.ps1 -Provider Nvidia -Model ...` |
| Thinking LLM with empty `content` | The client reads `reasoning_content` as a fallback |
| AWS Bedrock requires Signature V4 | Skeleton with `NotImplementedException` and instructions |
| LLM timeout or outage | HTTP timeout + heuristic fallback |
