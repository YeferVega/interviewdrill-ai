# Technical Plan: Scoring Synthesizer Agent (.NET 11)

## Architecture Overview
The Scoring Synthesizer follows a **Processing Pipeline** architecture with validation at each stage. It operates as a standalone service that receives evaluations from two evaluator agents, validates them, computes the weighted scoring and generates structured feedback. Implemented in **C# / .NET 11** within `InterviewDrill.Core`.

```
┌─────────────────────────────────────────────────────────┐
│              Scoring Synthesizer Pipeline               │
├─────────────────────────────────────────────────────────┤
│ 1. Input Validation (JSON Schema)                       │
│ 2. Data Extraction & Normalization                      │
│ 3. Score Calculation (Weighted Formula)                 │
│ 4. Verdict Determination                                │
│ 5. Feedback Consolidation                               │
│ 6. Audit Logging                                        │
│ 7. Output Generation                                    │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. ScoreCalculator (`InterviewDrill.Core/Scoring/ScoreCalculator.cs`)
**Responsibility:** Calculation of the overallScore using a weighted formula.
- Implements: `overallScore = (TechnicalDepth * 0.6) + (ProductionReadiness * 0.4)`
- Clamps result to 0-100 range
- Zero-Math principle: calculation done in C#, never delegated to an LLM

### 2. VerdictDeterminer (`InterviewDrill.Core/Scoring/VerdictDeterminer.cs`)
**Responsibility:** Determines the verdict based on overallScore.
- PASS: score >= 80
- NEEDS_PRACTICE: 60 <= score < 80
- FAIL: score < 60
- Strategy pattern for extensibility

### 3. IScoringSynthesizer (`InterviewDrill.Core/Contracts/IAgents.cs`)
**Responsibility:** Interface of the scoring synthesizer service.

### 4. EvaluationPayload (`InterviewDrill.Core/Contracts/Evaluation.cs`)
**Responsibility:** Immutable contract (C# record) for the evaluation payload.

## Data Flow

```
Architecture Evaluator (TechnicalDepth) ───┐
                                           ├──> Scoring Synthesizer ──> EvaluationPayload
Security & Rigor Judge (ProductionReadiness)───┘
```

## API Design (C# Records)

```csharp
// Input from Architecture Evaluator
public record ArchitectureEvaluation(
    double TechnicalDepth,
    IReadOnlyList<string> CoveredKeywords,
    IReadOnlyList<string> MissedConcepts,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> DetectedPatterns,
    IReadOnlyList<string> AntiPatterns);

// Input from Security & Rigor Judge
public record SecurityEvaluation(
    double ProductionReadiness,
    IReadOnlyList<string> CoveredKeywords,
    IReadOnlyList<string> MissedConcepts,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> SecurityConcerns,
    double ResilienceScore);

// Output EvaluationPayload (immutable contract)
public record EvaluationPayload(
    double OverallScore,
    double TechnicalDepth,
    double ProductionReadiness,
    string Verdict,
    IReadOnlyList<string> CoveredKeywords,
    IReadOnlyList<string> MissedConcepts,
    IReadOnlyList<string> Strengths,
    string ImprovementAdvice);
```

## JSON Schema Definition
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "EvaluationPayload",
  "type": "object",
  "required": [
    "overallScore",
    "technicalDepth",
    "productionReadiness",
    "verdict",
    "coveredKeywords",
    "missedConcepts",
    "strengths",
    "improvementAdvice"
  ],
  "properties": {
    "overallScore": { "type": "number", "minimum": 0, "maximum": 100 },
    "technicalDepth": { "type": "number", "minimum": 0, "maximum": 100 },
    "productionReadiness": { "type": "number", "minimum": 0, "maximum": 100 },
    "verdict": { "type": "string", "enum": ["PASS", "NEEDS_PRACTICE", "FAIL"] },
    "coveredKeywords": { "type": "array", "items": { "type": "string" } },
    "missedConcepts": { "type": "array", "items": { "type": "string" } },
    "strengths": { "type": "array", "items": { "type": "string" } },
    "improvementAdvice": { "type": "string" }
  }
}
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Project's authoritative stack, type-safe with immutable records |
| **xUnit** | Backend testing framework |
| **System.Text.Json** | JSON Schema serialization and validation |
| **InterviewDrill.Core** | Contracts layer and pure scoring (Zero-Math) |

## File Structure (.NET)

```
backend/
├── src/
│   └── InterviewDrill.Core/
│       ├── Scoring/
│       │   ├── ScoreCalculator.cs          # Fórmula 0.6/0.4, rango 0-100
│       │   └── VerdictDeterminer.cs         # PASS/NEEDS_PRACTICE/FAIL
│       └── Contracts/
│           ├── IAgents.cs                   # Interfaces de los 6 agentes
│           └── Evaluation.cs                # EvaluationPayload record
└── tests/
    └── InterviewDrill.Core.Tests/
        └── Scoring/
            ├── ScoreCalculatorTests.cs      # 12 tests (fórmulas, fronteras)
            └── VerdictDeterminerTests.cs    # 8 tests (fronteras exactas)
```

## Implementation Strategy

### Phase 1: Foundation (COMPLETED)
- [x] ScoreCalculator implemented and tested (0.6/0.4)
- [x] VerdictDeterminer implemented and tested (PASS/NEEDS_PRACTICE/FAIL)
- [x] Immutable contracts defined in Core
- [x] 26 tests passing (boundaries 59.99/60/79.99/80)

### Phase 2: Documentation (PENDING)
- [ ] Document the scoring synthesizer contract in the updated spec 002

## Testing Strategy

### Unit Tests (>90% coverage)
- **ScoreCalculator**: Boundary conditions (0, 60, 80, 100), decimal rounding, weight validation
- **VerdictDeterminer**: All verdict thresholds, edge cases

### Test Results
- **Passed**: 26/26 tests in InterviewDrill.Core.Tests
- **Boundary tests**: 59.99 (FAIL), 60 (NEEDS_PRACTICE), 79.99 (NEEDS_PRACTICE), 80 (PASS)

## Performance Considerations
- **Target**: < 2s P95 for scoring synthesis
- **Optimization**: All calculations are O(1) arithmetic in C#

## Security Considerations
- No sensitive data in scoring (only scores and verdicts)
- Input validation via JSON Schema before processing
