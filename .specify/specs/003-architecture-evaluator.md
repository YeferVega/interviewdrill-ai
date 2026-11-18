# Specification: Architecture Evaluator Agent

## Overview
The Architecture Evaluator is a specialized agent that analyzes the candidate's technical answers, assesses technical depth, detects key architectural patterns and antipatterns, and assigns a TechnicalDepth score (0-100). It uses **hybrid evaluation**: deterministic pre-filters (regex/keywords in C#) run first; the LLM judge with an anchored rubric (1–5 per criterion, `reasoning` before the score, `temperature = 0`) issues the semantic judgment. The 0–100 points are computed in C# code (Zero-Math).

## Stack
- **.NET 11 / C# 15** (implementation in `InterviewDrill.Agents`)
- **Interfaces in `InterviewDrill.Core`**: `IArchitectureEvaluator`, `ArchitectureEvaluation`
- **Structured output** from the LLM with a strict JSON schema

## Requirements

### Functional Requirements
1. Analyze the candidate's technical answers in free text
2. **Deterministic pre-filter**: detect key concepts with regex/keywords in C# (Zero-Math, KernelFunction, HNSW, Kafka, ActivitySource, Polly, RAG, Semantic Cache)
3. **LLM judge**: semantic evaluation with an anchored 1–5 rubric per criterion (clarity, technical depth, correctness, communication)
4. Identify correct architectural patterns
5. Detect antipatterns: delegating math to LLMs, strong coupling, blocking synchronous calls, no error handling, hardcoded secrets, no logging, ignoring PII
6. Assign a TechnicalDepth score from 0 to 100 (computed in C# from the LLM rubric + pre-filters)
7. Generate the list of coveredKeywords found
8. Generate the list of missedConcepts expected but not mentioned
9. Identify specific strengths of the answer
10. Return a valid ArchitectureEvaluation payload (JSON Schema)

### Non-Functional Requirements
1. **Performance**: Full analysis < 3 seconds P95
2. **Accuracy**: Concept detection > 90% precision
3. **Consistency**: Same inputs produce same outputs (`temperature = 0` in the LLM judge)
4. **Extensibility**: Easy to add new concepts and patterns
5. **Hybrid evaluation**: deterministic pre-filters FIRST, LLM judge for semantic judgment

## User Stories
- As an **Architecture Evaluator**, I want to analyze technical answers to assess the depth of the candidate's knowledge
- as a **system**, I want to detect key patterns to provide specific and actionable feedback
- as a **candidate**, I want the correct concepts I mentioned to be recognized so I receive a fair score
- as a **team**, I want the LLM judge to use `temperature = 0` and `reasoning` before the score for consistency

## Acceptance Criteria
- [~] Detects the Zero-Math pattern when not delegating math to LLMs is mentioned
- [x] Detects KernelFunction when Semantic Kernel orchestration is mentioned
- [x] Detects HNSW when efficient vector search is mentioned
- [x] Detects Kafka when event streaming is mentioned
- [x] Detects ActivitySource/Observability when distributed tracing is mentioned
- [x] Detects Polly when resilience patterns are mentioned
- [x] Detects RAG and Semantic Cache when applicable
- [x] Detects antipattern: delegating math to LLM
- [~] Detects antipattern: strong coupling to AI provider (heuristic phrase only; semantic via LLM judge)
- [ ] Detects antipattern: synchronous blocking calls for heavy files
- [~] Detects antipattern: no error handling (limited heuristic; the LLM judge can detect it semantically)
- [x] Detects antipattern: hardcoding secrets
- [x] Detects antipattern: ignoring PII protection
- [x] Assigns a TechnicalDepth score based on covered concepts + LLM rubric (computation in C#)
- [x] Generates the coveredKeywords list without duplicates
- [~] Generates the missedConcepts list based on expected concepts
- [x] Identifies specific strengths of the answer
- [x] Returns a valid ArchitectureEvaluation payload
- [~] Unit tests for detection of each concept
- [x] Unit tests for scoring logic (boundary conditions)
- [ ] Golden dataset tests for the LLM judge
- [ ] Coverage > 85%

## Out of Scope
- ProductionReadiness evaluation (responsibility of Security & Rigor Judge)
- Question generation (responsibility of Interviewer Agent)
- overallScore calculation (responsibility of Scoring Synthesizer)

## Dependencies
- **Scoring Synthesizer types**: `ArchitectureEvaluation` interface in `InterviewDrill.Core`
- **LLM provider** with structured output (temperature 0)
- **Logging**: Audit trail for evaluation decisions (Pino / .NET logging)

## Risks
1. **Risk**: False positives in concept detection (e.g., negations like "evitar estar tied to a provider")
   - **Mitigation**: Deterministic pre-filter + semantic LLM judge; do not rely on regex alone

2. **Risk**: Very long answers may exceed the timeout
   - **Mitigation**: Implement chunk processing or history summarization

3. **Risk**: New concepts are not in the detection list
   - **Mitigation**: Extensible design to easily add concepts in ConceptLibrary

4. **Risk**: LLM judge variance across runs
   - **Mitigation**: `temperature = 0`, structured output, calibration against a golden dataset
