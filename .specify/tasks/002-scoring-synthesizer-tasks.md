# Task List: 002-scoring-synthesizer

## Sprint/Iteration: Core Agent Development (.NET)

### Tasks

#### Task 1: Implement ScoreCalculator in C# (.NET / Zero-Math)
- **Description**: Implement `ScoreCalculator` in `InterviewDrill.Core` with the Zero-Math formula: `overallScore = (TechnicalDepth * 0.6) + (ProductionReadiness * 0.4)`. Arithmetic is never delegated to the LLM.
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Core contracts (`EvaluationPayload`, `ScoreCalculator`)
- **Status**: [x] Complete — `ScoreCalculator` implemented in `InterviewDrill.Core/Scoring/ScoreCalculator.cs`; 26 tests passing in `InterviewDrill.Core.Tests/Scoring/ScoreCalculatorTests.cs`.

#### Task 2: Implement VerdictDeterminer in C# (Zero-Math)
- **Description**: Implement `VerdictDeterminer` with verdict logic based on `overallScore` thresholds: PASS >= 80, NEEDS_PRACTICE 60–79, FAIL < 60.
- **Priority**: High
- **Estimated Time**: 1.5 hours
- **Dependencies**: Task 1, ScoreCalculator
- **Status**: [x] Complete — `VerdictDeterminer` implemented in `InterviewDrill.Core/Scoring/VerdictDeterminer.cs`; boundary tests (59.99/60/79.99/80) in `VerdictDeterminerTests.cs`.

#### Task 3: Implement AnchoredRubricMath (normalize 1–5 to 0–100 in C#)
- **Description**: Normalize LLM rubric scores (1–5) to a 0–100 scale and combine them with deterministic pre-filters, all in C# code.
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Task 1, Task 2
- **Status**: [x] Complete — `AnchoredRubricMath` implemented; normalizes 1–5 rubrics to 0–100 and combines them with the pre-filter without delegating arithmetic to the LLM.

#### Task 4: Implement ScoringSynthesizer (orchestrator of the scoring pipeline)
- **Description**: Orchestrator that receives the evaluations from the Architecture Evaluator and the Security & Rigor Judge, applies ScoreCalculator and VerdictDeterminer, and produces the structured `EvaluationPayload`.
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Tasks 1–3
- **Status**: [x] Complete — `ScoringSynthesizer` implemented and tested.

#### Task 5: Scoring unit tests (boundary conditions and verdicts)
- **Description**: Tests for boundary conditions (59.99, 60, 79.99, 80), weight validation (they sum to 1.0), and all verdict scenarios (PASS, NEEDS_PRACTICE, FAIL).
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Tasks 1–4
- **Status**: [x] Complete — 26 tests passing in `InterviewDrill.Core.Tests` (scoring + verdicts + weight validation).

### Definition of Done
- [x] `ScoreCalculator` implemented in C# (0.6/0.4 formula, Zero-Math)
- [x] `VerdictDeterminer` implemented in C# (60/80 thresholds)
- [x] `AnchoredRubricMath` implemented in C# (normalizes 1–5 → 0–100)
- [x] `ScoringSynthesizer` orchestrator implemented
- [x] Unit tests passing: 26/26 in `InterviewDrill.Core.Tests`
- [x] Boundary conditions validated (59.99/60/79.99/80)
