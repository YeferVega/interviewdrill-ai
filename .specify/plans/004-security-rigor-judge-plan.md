# Technical Plan: Security & Rigor Judge Agent (.NET 11)

## Architecture Overview
The Security & Rigor Judge follows a **Hybrid Security Audit** pattern with deterministic pre-filters and an LLM judge. Implemented in **C# / .NET 11** within `InterviewDrill.Agents`. It depends only on `InterviewDrill.Core` (interfaces).

```
┌─────────────────────────────────────────────────────────┐
│          Security & Rigor Judge Pipeline                │
├─────────────────────────────────────────────────────────┤
│ 1. Input Validation & Normalization                     │
│ 2. Pre-filter Determinista (PII, relevancia, rate limit)│
│ 3. Juez LLM con rúbrica anclada 1–5 (temperature 0)     │
│ 4. ProductionReadiness Scoring (0–100)                  │
│ 5. Resilience Score independiente                       │
│ 6. Output Validation                                    │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. HybridSecurityRigorJudge (`InterviewDrill.Agents/HybridSecurityRigorJudge.cs`)
**Responsibility:** Main orchestrator. Combines deterministic pre-filter + LLM judge.
- Calculates ProductionReadiness using `AnchoredRubricMath.CombineDeterministicAndJudge`

### 2. HeuristicSecurityRigorJudge (`InterviewDrill.Agents/HeuristicSecurityRigorJudge.cs`)
**Responsibility:** Deterministic pre-filter with regex/keywords in C#.
- Detects: PII sanitization, relevance guardrails (RelevanceScore < 0.78), human-in-the-loop, rate limiting (HTTP 429), semantic cache

### 3. LlmSecurityJudge (`InterviewDrill.Agents/LlmSecurityJudge.cs`)
**Responsibility:** LLM judge with an anchored 1–5 rubric.
- Structured output with a strict JSON schema
- Temperature 0 for consistency
- Criteria: P95/P99 latency, data protection, operational resilience

## Data Flow

```
Candidate Response ──> HeuristicSecurityRigorJudge (pre-filter)
                                           │
                                           v
                                  LlmSecurityJudge (LLM, temp 0)
                                           │
                                           v
                                  HybridSecurityRigorJudge (combinar)
                                           │
                                           v
                                  SecurityEvaluation (0-100 ProductionReadiness)
```

## API Design (C#)

```csharp
// Interface en Core
public interface ISecurityRigorJudge
{
    Task<SecurityEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default);
}
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack, Zero-Math in C# |
| **ILlmCompletionClient** | Pluggable interface for LLM providers |
| **IPiiSanitizer** | PII sanitization before sending to the LLM |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   └── InterviewDrill.Agents/
│       ├── HybridSecurityRigorJudge.cs
│       ├── HeuristicSecurityRigorJudge.cs
│       └── LlmSecurityJudge.cs
└── tests/
    └── InterviewDrill.Agents.Tests/
        └── LlmSecurityJudgeTests.cs (pendiente)
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| ISecurityRigorJudge implemented | ✅ Complete | `HybridSecurityRigorJudge` + `FallbackSecurityRigorJudge` |
| Deterministic pre-filters | ✅ Complete | `HeuristicSecurityRigorJudge` |
| LLM judge with temperature 0 | ~[~] In progress | `LlmSecurityJudge` implemented; real provider/calibration pending |
| ProductionReadiness in C# (Zero-Math) | ✅ Complete | `AnchoredRubricMath` |
| coveredKeywords, missedConcepts, securityConcerns, strengths | ✅ Complete | Deterministic + LLM merge with no duplicates |
| Unit tests | ~[~] In progress | Pre-filter, hybrid scoring, fallback; golden dataset pending |

## Testing Strategy

### Unit Tests
- **SecurityPatternDetector**: PII sanitization, relevance guardrails, human-in-the-loop, rate limiting, semantic cache
- **ResiliencePatternDetector**: Rate limiting, circuit breaker, retry, timeout, fallback
- **ProductionReadinessScorer**: Boundary conditions, weights
- **SecurityConcernsIdentifier**: Detection of critical gaps

### Coverage Target
- > 85% coverage on critical paths

## Risks

| Risk | Mitigation |
|------|------------|
| False negatives in security issue detection | Multiple signals and context analysis; pre-filter + LLM judge |
| Ambiguous answers about security | Request clarification in doubtful cases |
| LLM provider unavailable | Fallback to heuristic without score delegation to the model |
