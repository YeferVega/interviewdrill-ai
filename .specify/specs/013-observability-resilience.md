# Specification 013: Observability and Resilience (OpenTelemetry / ActivitySource / Polly)

## Overview
Implement distributed observability and resilience in the InterviewDrill AI system. Includes distributed tracing with OpenTelemetry/ActivitySource for LLM calls, database queries, and session operations, plus resilience with Polly for retry/backoff and HTTP 429 handling in LLM providers.

## Stack
- **.NET 11 / C# 15** (implemented in `InterviewDrill.Infrastructure` and `InterviewDrill.Api`)
- **OpenTelemetry .NET SDK** (`OpenTelemetry`, `OpenTelemetry.Exporter.Console`)
- **ActivitySource** for named activities
- **Polly** for retry with exponential backoff, circuit breaker, HTTP 429 handling
- **appsettings.json** for OpenTelemetry and resilience configuration

## User Stories
- **As a** system operator, **I want** distributed tracing with OpenTelemetry, **so that** I can diagnose bottlenecks in LLM calls and DB queries.
- **As a** developer, **I want** Polly with retry/backoff in LLM clients, **so that** the platform is resilient to transient failures and HTTP 429.
- **As a** developer, **I want** ActivitySource with descriptive activity names, **so that** I can correlate traces across components.

## Functional Requirements

### FR1: OpenTelemetry instrumentation
- `ActivitySource` named `InterviewDrill` for application activities
- Activities for: LLM calls, EF Core queries, session operations, agent evaluations
- `ActivityTags` for: sessionId, questionId, skillTarget, provider, model, tokens, duration

### FR2: Polly resilience in LLM clients
- Retry with exponential backoff (2s, 4s, 8s) for transient failures
- Specific handling of HTTP 429 (rate limiting) with retry after `Retry-After`
- Circuit breaker to prevent failure cascades when the LLM is down
- HTTP request timeout to avoid indefinite blocking

### FR3: Configuration via appsettings.json
- Section `OpenTelemetry`: serviceName, exporter, sampling
- Section `Resilience`: maxRetryAttempts, initialBackoff, maxBackoff, circuitBreakerThreshold
- Section `LLM:Timeout`: timeout in seconds for LLM requests

### FR4: Structured logging
- Logging of all evaluation and scoring decisions
- Logging of retry attempts and circuit breaker state changes
- No logging of PII or secrets

## Non-Functional Requirements

- **Observability**: Distributed traces correlated across LLM, DB, and agents
- **Resilience**: The system does not fail completely if one LLM provider is down
- **Performance**: Minimal tracing overhead (< 5% of total latency)
- **Security**: No PII, API keys, or secrets logged in traces or logs

## Acceptance Criteria

- [x] `ActivitySource` named `InterviewDrill` configured
- [x] Activities for LLM calls with relevant tags
- [x] Activities for EF Core queries with relevant tags
- [x] Activities for session operations with relevant tags
- [x] Polly retry policy applied to LLM clients (Azure OpenAI, Ollama, Bedrock)
- [x] HTTP 429 handling with retry after `Retry-After`
- [x] Circuit breaker configured for LLM clients
- [x] Configurable HTTP request timeout
- [x] OpenTelemetry configurable via `appsettings.json`
- [x] Structured logging without PII or secrets
- [ ] Unit tests for Polly policies and ActivitySource

## Out of Scope

- Exporter to a specific backend (Jaeger, Zipkin, Azure Monitor) — can be added later
- Custom metrics (tracing only)
- Automated alerts based on traces

## Dependencies

- **NuGet packages**: `OpenTelemetry`, `OpenTelemetry.Exporter.Console` (or another), `Polly`, `Polly.Extensions.Http`
- **InterviewDrill.Infrastructure.Llm**: LLM clients where Polly is applied
- **InterviewDrill.Infrastructure.Persistence**: EF Core where queries are instrumented

## Risks

| Risk | Mitigation |
|------|------------|
| OpenTelemetry adds configuration complexity | Optional configuration via `appsettings.json`; in-memory/no-op when not configured |
| Polly retry may mask real LLM failures | Maximum retry limits (max. 3 attempts) and clear logging |
| Tracing overhead affects performance | Configurable sampling; activities only on critical paths |
| No traces backend configured | Console exporter for development |
