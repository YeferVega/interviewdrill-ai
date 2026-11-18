# Specification: Scoring Synthesizer Agent

## Overview
The Scoring Synthesizer is the consolidating agent that unifies the evaluations from the Architecture Evaluator and Security & Rigor Judge, computes the weighted score, and generates the final verdict with structured feedback for the candidate.

## Requirements

### Functional Requirements
1. Receive parallel evaluations from Architecture Evaluator (TechnicalDepth) and Security & Rigor Judge (ProductionReadiness)
2. Validate that the input payloads comply with the JSON Schema
3. Compute the overallScore using the formula: `overallScore = (TechnicalDepth * 0.6) + (ProductionReadiness * 0.4)`
4. Determine the verdict based on the overallScore:
   - PASS: score >= 80
   - NEEDS_PRACTICE: 60 <= score < 80
   - FAIL: score < 60
5. Consolidate strengths, coveredKeywords, missedConcepts from both evaluators
6. Generate actionable and specific improvementAdvice
7. Return the complete EvaluationPayload following the immutable JSON Schema contract

### Non-Functional Requirements
1. **Performance**: Response time < 2 seconds for scoring synthesis
2. **Reliability**: Error handling if some evaluator does not respond or returns invalid data
3. **Validation**: All payloads must pass JSON Schema validation before processing
4. **Traceability**: Logging of all scoring decisions for auditing
5. **Scalability**: Ability to process multiple concurrent evaluations

## User Stories
- As a **Scoring Synthesizer**, I want to receive structured evaluations from both judges so I can consolidate results consistently
- as a **candidate**, I want to receive immediate feedback after each answer to understand my performance and areas for improvement
- as a **system**, I want to validate all input payloads to guarantee data integrity and prevent cascading errors
- as a **development team**, I want the scoring to be transparent and reproducible so decisions can be audited

## Acceptance Criteria
- [ ] Receives TechnicalDepth (0-100) from Architecture Evaluator
- [ ] Receives ProductionReadiness (0-100) from Security & Rigor Judge
- [ ] Validates that both payloads comply with the JSON Schema before processing
- [ ] Computes overallScore correctly: `(TechnicalDepth * 0.6) + (ProductionReadiness * 0.4)`
- [ ] Assigns verdict PASS when overallScore >= 80
- [ ] Assigns verdict NEEDS_PRACTICE when 60 <= overallScore < 80
- [ ] Assigns verdict FAIL when overallScore < 60
- [ ] Consolidates coveredKeywords from both evaluators without duplicates
- [ ] Consolidates missedConcepts from both evaluators without duplicates
- [ ] Generates specific and actionable improvementAdvice
- [ ] Returns the complete EvaluationPayload with all required fields
- [ ] Handles errors gracefully if an evaluator fails or returns invalid data
- [ ] Logs of all scoring decisions for auditing
- [ ] Unit tests for all verdict scenarios (boundary conditions: 60, 80, 100)
- [ ] JSON Schema validation tests for valid and invalid payloads

## Out of Scope
- Implementation of Interviewer Agent (to be developed later)
- Implementation of Architecture Evaluator (to be developed later)
- Implementation of Security & Rigor Judge (to be developed later)
- Frontend UI to display results (to be developed later)
- Integration with external tracking or analytics systems

## Dependencies
- **JSON Schema** for EvaluationPayload validation (immutable contract, see `.specify/memory/constitution.md`)
- **.NET 11 / C# 15** for type-safe implementation (immutable records in `InterviewDrill.Core`)
- **xUnit** for unit tests
- **Pino** (backend) / structured logging for traceability of decisions

## Implementation Status (.NET 11)

The Scoring Synthesizer is already implemented in `InterviewDrill.Core`:

| Component | File | Status |
|---|---|---|
| `ScoreCalculator` | `backend/src/InterviewDrill.Core/Scoring/ScoreCalculator.cs` | ✅ Implemented (formula `0.6/0.4`, range validation 0-100, weights sum to 1.0) |
| `VerdictDeterminer` | `backend/src/InterviewDrill.Core/Scoring/VerdictDeterminer.cs` | ✅ Implemented (PASS ≥80, NEEDS_PRACTICE 60-79.99, FAIL <60, strategy pattern) |
| `IScoringSynthesizer` | `backend/src/InterviewDrill.Core/Contracts/IAgents.cs` | ✅ Interface defined |
| `EvaluationPayload` | `backend/src/InterviewDrill.Core/Contracts/Evaluation.cs` | ✅ Immutable contract (C# record) |
| Tests | `backend/tests/InterviewDrill.Core.Tests/Scoring/` | ✅ 26 tests passing (boundaries 59.99/60/79.99/80, invalid weights) |

The formula `overallScore = (TechnicalDepth * 0.6) + (ProductionReadiness * 0.4)` and the verdicts are coded in C# (Zero-Math principle — never delegated to LLMs).

## Risks
1. **Risk**: Malformed payloads from evaluators can cause cascading errors
   - **Mitigation**: Strict JSON Schema validation before processing, error handling with fallbacks

2. **Risk**: The scoring formula may not adequately reflect actual performance
   - **Mitigation**: Document the formula clearly, allow weight adjustments in configuration

3. **Risk**: Generic ImprovementAdvice may not be useful for the candidate
   - **Mitigation**: Base advice on specific missedConcepts, use templates with personalization

4. **Risk**: High latency if evaluators take long to respond
   - **Mitigation**: Implement timeouts, process evaluations in parallel, use cache for repeated patterns

## Implementation Notes
- Use the **Strategy** pattern to allow different scoring formulas in the future
- Implement **retry logic** with exponential backoff for evaluators that fail
- Create a **factory pattern** for EvaluationPayload construction
- Include a **semantic cache** for similar responses (future optimization)

## Data Contract (JSON Schema)
```json
{
  "overallScore": number (0-100),
  "technicalDepth": number (0-100),
  "productionReadiness": number (0-100),
  "verdict": "PASS" | "NEEDS_PRACTICE" | "FAIL",
  "coveredKeywords": string[],
  "missedConcepts": string[],
  "strengths": string[],
  "improvementAdvice": string
}
```

## Success Metrics
- 100% of payloads validated against the JSON Schema
- Synthesis time < 2s at P95
- Zero errors in production due to malformed payloads
- Test coverage > 90% for scoring logic
