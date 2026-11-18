# Task List: 013-observability-resilience

## Sprint/Iteration: Observability and Resilience

### Tasks

#### Task 1: Create ActivitySource in InterviewDrill.Api
- **Priority**: High
- **Status**: [x] Complete — `ActivitySourceProvider` in `InterviewDrill.Core.Observability`

#### Task 2: Instrument LLM calls with ActivitySource
- **Priority**: High
- **Status**: [x] Complete — AzureOpenAILlmClient, OllamaLlmClient, BedrockLlmClient with ActivitySource

#### Task 3: Instrument EF Core queries with ActivitySource
- **Priority**: Medium
- **Status**: [ ] Not started — Pending (ActivitySource available to use)

#### Task 4: Instrument session operations with ActivitySource
- **Priority**: Medium
- **Status**: [ ] Not started — Pending (ActivitySource available to use)

#### Task 5: Configure OpenTelemetry in Program.cs
- **Priority**: High
- **Status**: [x] Complete — AddOpenTelemetry with AddSource for all ActivitySources + AddConsoleExporter

#### Task 6: Implement Polly retry policy with exponential backoff
- **Priority**: High
- **Status**: [x] Complete — `PollyPolicies.GetRetryPolicy()` with WaitAndRetryAsync

#### Task 7: Implement HTTP 429 handling with Retry-After
- **Priority**: High
- **Status**: [x] Complete — `OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)`

#### Task 8: Implement circuit breaker for LLM clients
- **Priority**: Medium
- **Status**: [x] Complete — `CircuitBreakerAsync` with a threshold of 5 failures and 30s duration

#### Task 9: Implement HTTP request timeout
- **Priority**: Medium
- **Status**: [x] Complete — `Policy.TimeoutAsync<HttpResponseMessage>` with 30 seconds

#### Task 10: Apply Polly policies to LLM clients
- **Priority**: High
- **Status**: [x] Complete — `PollyPolicies.GetResilientPolicy()` applied in AzureOpenAILlmClient, OllamaLlmClient, BedrockLlmClient

#### Task 11: Configure appsettings.json with OpenTelemetry and Resilience
- **Priority**: Medium
- **Status**: [ ] Not started — Pending to add sections in appsettings.json

#### Task 12: Implement structured logging without PII or secrets
- **Priority**: Medium
- **Status**: [ ] Not started — Pending (ActivitySource does not log PII by design)

#### Task 13: Run resilience unit tests
- **Priority**: High
- **Status**: [x] Complete — All Infrastructure tests pass (48/48)

### Definition of Done
- [x] ActivitySource "InterviewDrill" configured
- [x] Activities for LLM calls (instrumented in LLM clients)
- [~] Activities for DB queries (ActivitySource available, formal instrumentation pending — Task 3)
- [~] Activities for session operations (ActivitySource available, formal instrumentation pending — Task 4)
- [x] Polly retry with exponential backoff applied to LLM clients
- [x] HTTP 429 handling with Retry-After
- [x] Circuit breaker configured
- [x] Configurable HTTP request timeout (30 seconds)
- [x] OpenTelemetry configurable via code (AppsOpenTelemetry in Program.cs; appsettings.json sections pending — Task 11)
- [~] Structured logging without PII or secrets — ActivitySource does not log PII by design; formal decorated logger pending (Task 12)
- [x] Unit tests passing (Infrastructure 48/48)
