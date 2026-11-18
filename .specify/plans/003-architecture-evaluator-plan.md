# Technical Plan: Architecture Evaluator Agent (.NET 11)

## Architecture Overview
The Architecture Evaluator follows a **Hybrid Analysis Pipeline** pattern with deterministic pre-filters and an LLM judge. Implemented in **C# / .NET 11** within `InterviewDrill.Agents`. It depends only on `InterviewDrill.Core` (interfaces).

```
┌─────────────────────────────────────────────────────────┐
│           Architecture Evaluator Pipeline               │
├─────────────────────────────────────────────────────────┤
│ 1. Input Validation & Normalization                     │
│ 2. Pre-filter Determinista (regex/keywords en C#)       │
│ 3. Juez LLM con rúbrica anclada 1–5 (temperature 0)     │
│ 4. Combinación determinístico + juez (C# Zero-Math)     │
│ 5. TechnicalDepth Scoring (0–100)                       │
│ 6. Output Validation                                    │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. HybridArchitectureEvaluator (`InterviewDrill.Agents/HybridArchitectureEvaluator.cs`)
**Responsibility:** Main orchestrator. Combines deterministic pre-filter + LLM judge.
- Depends on: `IArchitectureEvaluator` (heuristic) and `IArchitectureEvaluator` (LLM judge)
- Calculates TechnicalDepth using `AnchoredRubricMath.CombineDeterministicAndJudge`

### 2. HeuristicArchitectureEvaluator (`InterviewDrill.Agents/HeuristicArchitectureEvaluator.cs`)
**Responsibility:** Deterministic pre-filter with regex/keywords in C#.
- Detects: Zero-Math, KernelFunction, HNSW, Kafka, ActivitySource, Polly, RAG, Semantic Cache
- Detects anti-patterns: delegating math to LLM, strong coupling, hardcoding secrets, ignoring PII

### 3. LlmArchitectureJudge (`InterviewDrill.Agents/LlmArchitectureJudge.cs`)
**Responsibility:** LLM judge with an anchored 1–5 rubric.
- Structured output with a strict JSON schema
- Temperature 0 for consistency
- Reasoning before the score
- Criteria: Clarity, TechnicalAccuracy, ArchitectureDepth, Communication

### 4. AnchoredRubricMath (`InterviewDrill.Agents/AnchoredRubricMath.cs`)
**Responsibility:** Zero-Math calculation in C#.
- Normalize: converts the 1-5 rubric to 0-100
- WeightedNormalized: weighted average of criteria
- CombineDeterministicAndJudge: combines the pre-filter and the judge

## Data Flow

```
Candidate Response ──> HeuristicArchitectureEvaluator (pre-filter)
                                           │
                                           v
                                  LlmArchitectureJudge (LLM, temp 0)
                                           │
                                           v
                                  HybridArchitectureEvaluator (combinar)
                                           │
                                           v
                                  ArchitectureEvaluation (0-100 TechnicalDepth)
```

## API Design (C#)

```csharp
// Interface en Core
public interface IArchitectureEvaluator
{
    Task<ArchitectureEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default);
}

// Implementation en Agents
public sealed class HybridArchitectureEvaluator(
    HeuristicArchitectureEvaluator preFilter,
    LlmArchitectureJudge judge) : IArchitectureEvaluator

public sealed class LlmArchitectureJudge(
    ILlmCompletionClient llmClient,
    IPiiSanitizer piiSanitizer)
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack, Zero-Math in C# |
| **ILlmCompletionClient** | Pluggable interface for LLM providers (Azure OpenAI, Bedrock, Ollama) |
| **IPiiSanitizer** | PII sanitization before sending to the LLM |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   └── InterviewDrill.Agents/
│       ├── HybridArchitectureEvaluator.cs
│       ├── HeuristicArchitectureEvaluator.cs
│       ├── LlmArchitectureJudge.cs
│       └── AnchoredRubricMath.cs
└── tests/
    └── InterviewDrill.Agents.Tests/
        └── LlmArchitectureEvaluatorTests.cs
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| IArchitectureEvaluator implemented | ✅ Complete | `HybridArchitectureEvaluator` + `FallbackArchitectureEvaluator` |
| Deterministic pre-filters | ✅ Complete | `HeuristicArchitectureEvaluator` |
| LLM judge with temperature 0 | ~[~] In progress | `LlmArchitectureJudge` implemented; real provider pending |
| TechnicalDepth in C# (Zero-Math) | ✅ Complete | `AnchoredRubricMath` |
| coveredKeywords, missedConcepts, strengths | ✅ Complete | Deterministic + LLM merge, no duplicates |
| Unit tests | ~[~] In progress | Pre-filter, hybrid scoring, invalid score, fallback; golden dataset pending |

## Testing Strategy

### Unit Tests
- **PatternDetector**: Each concept (Zero-Math, KernelFunction, HNSW, Kafka, ActivitySource, Polly, RAG, Semantic Cache)
- **AntiPatternDetector**: Delegating math to LLM, strong coupling, hardcoding secrets, ignoring PII
- **TechnicalDepthScorer**: Boundary conditions, penalties for anti-patterns
- **HybridArchitectureEvaluator**: Deterministic + judge combination

### Coverage Target
- > 85% coverage on critical paths

## Risks

| Risk | Mitigation |
|------|------------|
| False positives in concept detection | Deterministic pre-filter + semantic LLM judge; do not rely on regex alone |
| LLM judge variance between runs | temperature = 0, structured output, calibration against golden dataset |
| LLM provider unavailable | Fallback to heuristic without score delegation to the model |
