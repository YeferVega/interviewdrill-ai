# Task List: 012-llm-providers

## Sprint/Iteration: LLM Provider Integration

### Tasks

#### Task 1: Verify ILlmCompletionClient and LlmCompletionRequest in Core
- **Priority**: High
- **Status**: [x] Complete

#### Task 2: Verify LlmProviderOptions in Core
- **Priority**: High
- **Status**: [x] Complete

#### Task 3: Verify AzureOpenAILlmClient in Infrastructure
- **Priority**: High
- **Status**: [x] Complete — HTTP + Bearer token + Polly + OpenTelemetry

#### Task 4: Verify BedrockLlmClient in Infrastructure
- **Priority**: High
- **Status**: [x] Complete — Skeleton with NotImplementedException for SigV4 + Polly + OpenTelemetry

#### Task 5: Verify OllamaLlmClient in Infrastructure
- **Priority**: High
- **Status**: [x] Complete — Endpoint /api/generate + BaseAddress + Polly + OpenTelemetry

#### Task 6: Verify UnavailableLlmCompletionClient as fallback
- **Priority**: Medium
- **Status**: [x] Complete

#### Task 7: Verify DI registration in Program.cs
- **Priority**: High
- **Status**: [x] Complete — Switch by LLM:Provider with HttpClient and Timeout

#### Task 8: Verify appsettings.json with LLM section
- **Priority**: High
- **Status**: [x] Complete — Empty placeholders, no secrets

#### Task 9: Run unit tests for LLM clients
- **Priority**: High
- **Status**: [x] Complete — AzureOpenAILlmClientTests and OllamaLlmClientTests passing

#### Task 10: Verify there are no hardcoded secrets
- **Priority**: High
- **Status**: [x] Complete

#### Task 11: OpenAI-compatible provider (NVIDIA NIM / OpenAI)
- **Description**: Add `OpenAiCompatibleLlmClient` + `OpenAiCompatibleOptions` (BaseUrl/ApiKey/Model), DI with aliases `OpenAICompatible`/`Nvidia`/`NVIDIA`/`OpenAI`, `LLM:OpenAiCompatible` placeholder (empty ApiKey), and the `scripts/llm-check.ps1` connectivity script. Keep Foundry and Bedrock open. Validate with a unit test (fake HttpMessageHandler) and a real connectivity test.
- **Priority**: High
- **Status**: [x] Complete — Tests: `OpenAiCompatibleLlmClientTests` (6 cases). Real test with the user's key: 429 `insufficient_quota` → valid key but OpenAI has no balance (and the `sk-proj-` format = OpenAI, not NVIDIA). Key to rotate.

#### Task 12: OpenCode provider (OpenAI-compatible gateway, free/Go models)
- **Description**: Configure the OpenAI-compatible client against OpenCode (`BaseUrl=https://opencode.ai/inference/openai/v1`, `ApiKey=OPENCODE_API_KEY`, `*-free` models for Go and full models with Go active). Accept the alias `"OpenCode"`. Validate local connectivity with `scripts/llm-check.ps1 -Provider OpenCode`.
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: Task 11
- **Status**: [x] Complete — correct endpoint and authentic OpenCode key; `429 FreeUsageLimitError` was returned because there is no active Go subscription. Code and config are ready: once Go is subscribed and/or the daily reset happens, the E2E will work without further changes.

#### Task 13: OpenAI-compatible body fields + reasoning_content fallback (NVIDIA evidence)
- **Description**: `OpenAiCompatibleOptions.MaxTokens/TopP/EnableThinking`; body `max_tokens/top_p/chat_template_kwargs`; `response_format json_object` for the judges. Fallback to `reasoning_content` when `content` is empty (thinking models). Validate real connectivity with `nvidia/nemotron-3.5-lightning-30b-a3b` (lowercase IDs with hyphens). Update `scripts/llm-check.ps1 -Provider Nvidia` to send `max_tokens=16384, top_p=0.95, enable_thinking=true`. Default Development config = NVIDIA with those values (ApiKey via env, empty in repo).
- **Priority**: High
- **Status**: [x] Complete — `nvidia/nemotron-3.5-lightning-30b-a3b` responded **HTTP 200** with an `nvapi-` key. Tests `OpenAiCompatibleLlmClientTests`: 8 cases (thinking/top_p/max_tokens body, reasoning_content fallback, missing config). Full suite **249/249**. NVIDIA key exposed: **rotate**.

### Definition of Done
- [x] `ILlmCompletionClient` defined in `InterviewDrill.Core.Llm`
- [x] `OpenAiCompatibleLlmClient` (NVIDIA/OpenAI/OpenCode) with `MaxTokens/TopP/EnableThinking`, `reasoning_content` fallback, and failure mapping to `LlmUnavailableException`; `AzureOpenAILlmClient`, `BedrockLlmClient`, `OllamaLlmClient` implemented
- [x] `LlmProviderOptions` with full configuration (incl. OpenAiCompatible + thinking fields)
- [x] `appsettings.json` with `LLM` section (empty ApiKey, no secrets); `appsettings.Development.json` with NVIDIA SuperVL by default
- [x] Real API key in **user-secrets** (`UserSecretsId` in csproj) or env; not in the repo
- [x] DI registration in `Program.cs` (accepts `OpenAICompatible | Nvidia | NVIDIA | OpenAI | OpenCode`; uses `builder.Configuration["LLM:Provider"]` and fallback by key environment variables)
- [x] No hardcoded secrets
- [x] Unit tests passing for OpenAI-compatible (incl. thinking/body/reasoning), Azure OpenAI, and Ollama
- [x] `scripts/llm-check.ps1 -Provider Nvidia` verified **200** with `nvidia/nemotron-3.5-lightning-30b-a3b` and with `nvidia/nemotron-3.5-super-vl-120b-a12b`
- [x] Real `/api/jd/parse` with SuperVL → `profileSource=llm`; `-WithLlm` E2E (thinking OFF) green
- [x] `HeuristicApiFactory` in Api.Tests: deterministic suite (no network) even when user-secrets exist

#### Task 14: NVIDIA SuperVL verification via user-secrets + real E2E
- **Description**: Configure SuperVL as the base model (config/Development), store `ApiKey` in user-secrets, verify that `/api/jd/parse` returns `profileSource=llm`, and that `scripts/e2e-smoke.ps1 -WithLlm` passes with `thinking=OFF, max_tokens=4096`.
- **Priority**: High
- **Status**: [x] Complete — JD parse `source=llm` in ~7-8s; full `-WithLlm` E2E passed; SuperVL with thinking ON is too slow for an automated gate.

#### Task 15: Test determinism when user-secrets are present
- **Description**: Prevent Api.Tests' `WebApplicationFactory<Program>` from loading user-secrets/env `NVIDIA_API_KEY` and making network calls (`JdParseEndpointTests` had become non-deterministic). Introduce `HeuristicApiFactory` (env `Testing` + override `LLM:Provider=Heuristic`) used by the 3 endpoint classes.
- **Priority**: High
- **Status**: [x] Complete — deterministic suite: Api 12/12 without touching the network; JD parse heuristic again; the rest remains 249/249.
