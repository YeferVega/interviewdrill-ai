# Technical Plan: Observability and Resilience (.NET 11)

## Architecture Overview
Observability and resilience instrumentation in the InterviewDrill AI system. Implemented in **C# / .NET 11**.

```
┌─────────────────────────────────────────────────────────┐
│         Observability & Resilience Architecture          │
├─────────────────────────────────────────────────────────┤
│  OpenTelemetry                                          │
│  ├── ActivitySource "InterviewDrill"                    │
│  ├── Activities: LLM calls, DB queries, Session ops     │
│  └── Tags: sessionId, questionId, provider, duration    │
│                                                         │
│  Polly Resilience                                       │
│  ├── Retry with exponential backoff                     │
│  ├── HTTP 429 handling with Retry-After                 │
│  ├── Circuit breaker                                    │
│  └── HTTP timeout                                       │
│                                                         │
│  Structured Logging                                     │
│  ├── No PII, no secrets                                 │
│  └── Evaluation decisions logged                        │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. ActivitySource (`InterviewDrill.Api/Observability/ActivitySourceProvider.cs`)
**Responsibility:** Provider of the `ActivitySource` named `InterviewDrill`.
- Activities for: LLM calls, EF Core queries, session operations, agent evaluations
- Tags: sessionId, questionId, skillTarget, provider, model, tokens, duration

### 2. OpenTelemetry Configuration (`InterviewDrill.Api/Program.cs`)
**Responsibility:** OpenTelemetry configuration from appsettings.json.
- `OpenTelemetry` section: serviceName, exporter, sampling
- Exporter to Console or No-op for development

### 3. Polly Policies (`InterviewDrill.Infrastructure.Resilience/PollyPolicies.cs`)
**Responsibility:** Resilience policies for LLM clients.
- Retry with exponential backoff (2s, 4s, 8s, max. 3 attempts)
- HTTP 429 handling with the `Retry-After` header
- Circuit breaker (failure threshold, open duration)
- HTTP request timeout

### 4. ResilientLlmClientWrapper (`InterviewDrill.Infrastructure.Resilience/`)
**Responsibility:** Wrapper around LLM clients that applies the Polly policies.

## Data Flow

```
LLM Client Call ──> Polly Policy (Retry + Circuit Breaker + Timeout)
                                           │
                                  ¿Éxito? ──> Retornar respuesta
                                  ┌───────┴───────┐
                                  Sí              No
                                  │               │
                                  v               v
                         Retornar           Log error + fallback
                                  │               │
                                  v               v
                         Activity.Stop()    Activity.Stop() con excepción
```

## API Design (C#)

```csharp
// ActivitySource
public static class ActivitySourceProvider
{
    public static readonly ActivitySource ActivitySource = new("InterviewDrill");
}

// Polly Policies
public static class PollyPolicies
{
    public static AsyncPolicy<HttpResponseMessage> GetRetryPolicy();
    public static AsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy();
    public static AsyncPolicy<HttpResponseMessage> GetTimeoutPolicy();
    public static AsyncPolicyWrap<HttpResponseMessage> GetResilientPolicy();
}
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack |
| **OpenTelemetry .NET SDK** | Standard distributed tracing |
| **ActivitySource** | Named activities with tags |
| **Polly** | Resilience: retry, circuit breaker, timeout |
| **Polly.Extensions.Http** | Polly extensions for HttpClient |
| **appsettings.json** | Observability and resilience configuration |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   ├── InterviewDrill.Api/
│   │   ├── Observability/
│   │   │   └── ActivitySourceProvider.cs
│   │   └── Program.cs (OpenTelemetry + Polly configuration)
│   └── InterviewDrill.Infrastructure.Resilience/
│       ├── PollyPolicies.cs
│       └── ResilientLlmClientWrapper.cs
└── tests/
    └── InterviewDrill.Infrastructure.Tests/
        └── Resilience/
            └── PollyPoliciesTests.cs
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| ActivitySource "InterviewDrill" | ❌ Not started | Does not exist in code |
| OpenTelemetry in Program.cs | ❌ Not started | Not configured |
| Polly retry policy | ❌ Not started | Not implemented |
| HTTP 429 handling | ❌ Not started | Not implemented |
| Circuit breaker | ❌ Not started | Not implemented |
| HTTP timeout | ❌ Not started | Not implemented |
| appsettings.json configuration | ❌ Not started | Section does not exist |
| Structured logging without PII | ❌ Not started | Not implemented |
| Unit tests | ❌ Not started | They do not exist |

## Testing Strategy

### Unit Tests
- **PollyPolicies**: Retry with backoff, circuit breaker, timeout, HTTP 429 handling
- **ActivitySource**: Activities created with the correct tags
- **ResilientLlmClientWrapper**: Application of policies to LLM calls

### Coverage Target
- > 80% coverage for the resilience policies

## Risks

| Risk | Mitigation |
|------|------------|
| OpenTelemetry adds complexity | Optional configuration via appsettings.json; No-op when not configured |
| Polly retry masks real failures | Max. 3 attempts, clear logging of each attempt |
| Tracing overhead | Configurable sampling, only critical paths |
| No traces backend | Exporter to Console for development |
