# Specification: Security & Rigor Judge Agent

## Overview
The Security & Rigor Judge is an auditor agent that evaluates the production readiness of the candidate's answers, focusing on security, data governance, operational resilience, and cost efficiency. It assigns a ProductionReadiness score (0-100). It uses **hybrid evaluation**: deterministic pre-filters (regex/keywords in C#) run first; the LLM judge with an anchored rubric (1–5 per criterion, `reasoning` before the score, `temperature = 0`) issues the semantic judgment.

## Stack
- **.NET 11 / C# 15** (implementation in `InterviewDrill.Agents`)
- **Interfaces in `InterviewDrill.Core`**: `ISecurityRigorJudge`, `SecurityEvaluation`
- **Structured output** from the LLM with a strict JSON schema

## Requirements

### Functional Requirements
1. **Deterministic pre-filter**: assess whether the answer contemplates PII sanitization (RegexPiiSanitizer)
2. **Deterministic pre-filter**: verify the presence of relevance guardrails (RelevanceScore < 0.78)
3. **Deterministic pre-filter**: detect human intervention mechanisms (Human-in-the-loop)
4. **Deterministic pre-filter**: assess rate limiting handling (HTTP 429)
5. **Deterministic pre-filter**: detect semantic cache implementation
6. **LLM judge**: evaluate P95/P99 latency considerations, sensitive data protection, operational resilience
7. Assign a ProductionReadiness score from 0 to 100 (computed in C#)
8. Generate the list of security/resilience coveredKeywords
9. Generate the list of security missedConcepts
10. Identify specific securityConcerns
11. Compute the resilienceScore (0-100)
12. Return a valid SecurityEvaluation payload (JSON Schema)

### Non-Functional Requirements
1. **Performance**: Full analysis < 3 seconds P95
2. **Security focus**: Precise detection of security issues
3. **Compliance**: Validation against security standards
4. **Auditability**: Detailed logging of decisions
5. **Hybrid evaluation**: deterministic pre-filters FIRST, LLM judge for semantic judgment

## User Stories
- as a **Security & Rigor Judge**, I want to evaluate answers from a production perspective to ensure the candidate considers critical security and resilience aspects
- as a **system**, I want to detect security issues early to prevent risky implementations
- as a **team**, I want candidates to demonstrate knowledge of guardrails and data protection
- as a **team**, I want the LLM judge to use `temperature = 0` and `reasoning` before the score for consistency

## Acceptance Criteria
- [x] Detects PII sanitization when RegexPiiSanitizer or data masking is mentioned
- [x] Detects relevance guardrails when a RelevanceScore threshold is mentioned
- [x] Detects human-in-the-loop mechanisms
- [x] Detects rate limiting handling (HTTP 429)
- [x] Detects semantic cache implementation
- [x] Detects latency considerations (P95/P99 targets)
- [x] Detects data encryption/protection
- [~] Detects audit logging mentions (observability/logs/metrics/traces covered; exact term limited)
- [x] Identifies security concerns in the answer
- [x] Assigns a ProductionReadiness score based on coverage + LLM rubric (computation in C#)
- [x] Computes an independent resilienceScore
- [x] Generates the coveredKeywords list
- [x] Generates the missedConcepts list
- [x] Identifies security strengths
- [x] Returns a valid SecurityEvaluation payload
- [~] Unit tests for each detection
- [ ] Golden dataset tests for the LLM judge
- [ ] Coverage > 85%

## Out of Scope
- TechnicalDepth evaluation (responsibility of Architecture Evaluator)
- Question generation
- overallScore calculation

## Dependencies
- **Scoring Synthesizer types**: `SecurityEvaluation` interface in `InterviewDrill.Core`
- **LLM provider** with structured output (temperature 0)
- **Logging**: Audit trail for evaluation decisions (Pino / .NET logging)

## Risks
1. **Risk**: False negatives in detecting security issues
   - **Mitigation**: Use multiple signals and context analysis; pre-filter + LLM judge

2. **Risk**: Ambiguous answers about security
   - **Mitigation**: Request clarification in doubtful cases

3. **Risk**: New threats are not in the detection list
   - **Mitigation**: Keep an updated list of security patterns

4. **Risk**: LLM judge variance across runs
   - **Mitigation**: `temperature = 0`, structured output, calibration against a golden dataset
