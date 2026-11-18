# Task List: 003-architecture-evaluator

## Sprint/Iteration: Phase 3 — Evaluation Panel

### Tasks

#### Task 1: Implement IArchitectureEvaluator in InterviewDrill.Agents
- **Description**: Create the concrete class implementing IArchitectureEvaluator with hybrid evaluation (deterministic regex pre-filters + LLM judge with anchored rubric).
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Core interfaces (IArchitectureEvaluator, ArchitectureEvaluation), ConceptLibrary, AntiPatternDetector
- **Status**: [x] Complete — `HybridArchitectureEvaluator` + `FallbackArchitectureEvaluator`.

#### Task 2: Implement deterministic pre-filters (regex/keywords in C#)
- **Description**: Detect key concepts (Zero-Math, KernelFunction, HNSW, Kafka, ActivitySource, Polly, RAG, Semantic Cache) and anti-patterns using regex in C# before calling the LLM.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 1, ConceptLibrary, AntiPatternDetector
- **Status**: [x] Complete — `HeuristicArchitectureEvaluator`.

#### Task 3: Implement LLM judge with anchored 1–5 rubric (temperature 0)
- **Description**: Call the LLM with structured output (strict JSON schema, reasoning before the score, temperature 0) to evaluate clarity, technical depth, correctness, and communication.
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Task 2, LLM provider with structured output
- **Status**: [~] In progress — `LlmArchitectureJudge` implemented with schema and temperature 0; real LLM provider and golden dataset calibration still pending.

#### Task 4: Compute TechnicalDepth in C# (Zero-Math)
- **Description**: Compute the 0–100 score in C# code from the pre-filter results and the LLM rubric. Never delegate arithmetic to the LLM.
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Task 3, ScoreCalculator in Core
- **Status**: [x] Complete — `AnchoredRubricMath` normalizes 1–5 to 0–100 and combines it with the pre-filter in C#.

#### Task 5: Generate coveredKeywords, missedConcepts and strengths
- **Description**: Extract covered concepts, missed concepts, and strengths from the answer for the ArchitectureEvaluation payload.
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 4
- **Status**: [x] Complete — deterministic + LLM merge, no duplicates.

#### Task 6: Unit tests for the Architecture Evaluator
- **Description**: Tests for concept detection, anti-patterns, scoring logic with boundary conditions, and a golden dataset for the LLM judge.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 1-5
- **Status**: [~] In progress — pre-filter, hybrid scoring, invalid score, and fallback tests implemented; real golden dataset still pending.

### Definition of Done
- [x] IArchitectureEvaluator implemented with hybrid evaluation
- [x] Deterministic pre-filters work (regex/keywords in C#)
- [~] LLM judge with temperature 0 and reasoning-first (implemented; real provider/calibration pending)
- [x] TechnicalDepth computed in C# (Zero-Math)
- [~] Unit tests passing (> 85% coverage on critical paths)
- [ ] Golden dataset validated
