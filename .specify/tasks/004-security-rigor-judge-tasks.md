# Task List: 004-security-rigor-judge

## Sprint/Iteration: Phase 3 — Evaluation Panel

### Tasks

#### Task 1: Implement ISecurityRigorJudge in InterviewDrill.Agents
- **Description**: Create the concrete class implementing ISecurityRigorJudge with hybrid evaluation (deterministic regex pre-filters + LLM judge with anchored rubric).
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Core interfaces (ISecurityRigorJudge, SecurityEvaluation)
- **Status**: [x] Complete — `HybridSecurityRigorJudge` + `FallbackSecurityRigorJudge`.

#### Task 2: Implement deterministic pre-filters (regex/keywords in C#)
- **Description**: Detect PII sanitization, relevance guardrails (RelevanceScore < 0.78), human-in-the-loop, rate limiting (HTTP 429), semantic cache using regex in C#.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — `HeuristicSecurityRigorJudge`.

#### Task 3: Implement LLM judge for security and resilience (temperature 0)
- **Description**: Call the LLM with structured output (strict JSON schema, reasoning before the score, temperature 0) to evaluate P95/P99 latency, data protection, operational resilience.
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Task 2, LLM provider with structured output
- **Status**: [~] In progress — `LlmSecurityJudge` implemented with schema and temperature 0; real provider/golden dataset calibration pending.

#### Task 4: Compute ProductionReadiness and resilienceScore in C# (Zero-Math)
- **Description**: Compute 0–100 scores in C# code from pre-filters and the LLM rubric. Never delegate arithmetic to the LLM.
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Task 3, ScoreCalculator in Core
- **Status**: [x] Complete — `AnchoredRubricMath` normalizes and combines rubrics in C#.

#### Task 5: Generate coveredKeywords, missedConcepts, securityConcerns and strengths
- **Description**: Extract covered security concepts, missed ones, specific concerns, and strengths for the SecurityEvaluation payload.
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 4
- **Status**: [x] Complete — deterministic + LLM merge with no duplicates.

#### Task 6: Unit tests for the Security & Rigor Judge
- **Description**: Tests for security guardrail detection, scoring logic, golden dataset for the LLM judge.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 1-5
- **Status**: [~] In progress — pre-filter, hybrid scoring, and fallback tests; real golden dataset still pending.

### Definition of Done
- [x] ISecurityRigorJudge implemented with hybrid evaluation
- [x] Deterministic pre-filters work (PII, relevance, HITL, rate limiting, semantic cache)
- [~] LLM judge with temperature 0 and reasoning-first (implemented; real provider/calibration pending)
- [x] ProductionReadiness and resilienceScore computed in C# (Zero-Math)
- [~] Unit tests passing (> 85% coverage on critical paths)
- [ ] Golden dataset validated
