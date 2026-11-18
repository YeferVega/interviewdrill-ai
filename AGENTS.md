# AGENTS.md — Constitution & Multi-Agent Architecture for InterviewDrill AI

This document governs the architecture, responsibilities, immutable contracts, and interaction rules for the agents of the GenAI technical interview simulator.

---

## 1. Agent Topology

The system operates under a Supervisor pattern with a Specialized Evaluation Panel:

- **JD Parser Agent (Job Opening Analyst):** Extracts role, seniority, skills, and responsibilities from the Job Description uploaded by the user (PDF/DOCX/TXT) and produces a structured `JobProfile`.
- **Interviewer Agent (Host / Technical Moderator):** Generates questions and failure scenarios **from the `JobProfile`** (not from a static bank), adjusts difficulty according to performance, and **never repeats questions already asked in previous sessions** for the same user+JD.
- **Architecture Evaluator (Systems and Backend Specialist):** Analyzes the technical depth and the software patterns proposed (hybrid evaluation: deterministic pre-filters + LLM judge with anchored rubrics).
- **Security & Rigor Judge (Production & Guardrails Auditor):** Evaluates resilience, data privacy, and failure mitigation.
- **Scoring Synthesizer (Feedback Consolidator):** Consolidates the results into a structured verdict with immediate feedback.
- **Study Guide Generator (Study Guide Generator):** At the end of the interview, it aggregates all evaluations and produces a detailed, prioritized study guide of the topics to reinforce.

Data flow:
1. Candidate uploads the Job Description → JD Parser Agent produces the `JobProfile`.
2. Interviewer Agent issues a question generated from the `JobProfile` (deduplicated against persistent history).
3. Candidate enters their technical answer.
4. Architecture Evaluator and Security & Rigor Judge analyze the answer in parallel.
5. Scoring Synthesizer collects both evaluations, computes the weighted score, and issues the immediate report.
6. When the interview closes, the Study Guide Generator consolidates all `missedConcepts` and issues the study guide.

---

## 2. Role Definitions

### A. JD Parser Agent (Job Opening Analyst)
- **Purpose:** Convert the Job Description into a structured profile that feeds the entire interview.
- **Inputs:** JD file (PDF/DOCX/TXT) or plain text.
- **Outputs:** `JobProfile { role, seniority, requiredSkills[], niceToHaveSkills[], responsibilities[], rawTextHash }`.
- **Behavior Rules:**
  - Sanitizes PII from the document before sending it to the LLM.
  - Returns structured output with a strict schema (temperature 0).
  - `rawTextHash` uniquely identifies the JD for question deduplication.

### B. Interviewer Agent (Interview Moderator)
- **Purpose:** Pose situational questions, production problems, and follow-up questions generated from the `JobProfile`.
- **Inputs:** Session history, `JobProfile`, persistent registry of questions already asked (user+JD).
- **Behavior Rules:**
  - Asks direct questions and practical use cases (e.g., quota failures, latency, hallucinations, data leakage).
  - Does not hand over the solution before the user answers.
  - Adjusts the rigor level of the next question according to the performance reported by the synthesizer.
  - **Anti-repetition:** before issuing a question, checks against the persistent registry (exact and semantically similar) and regenerates if there is a collision.

### C. Architecture Evaluator (Systems and Backend Specialist)
- **Purpose:** Assess the soundness of the answer in terms of software design and enterprise patterns.
- **Hybrid evaluation:** deterministic pre-filters (regex/keywords) detect cheap signals; the LLM judge with an anchored rubric (1–5 per criterion, `reasoning` before the score, temperature 0) issues the semantic judgment. The 0–100 points are calculated in C# (Zero-Math).
- **Evaluation Criteria:**
  - Presence of key concepts and patterns: `Zero-Math`, `KernelFunction`, `HNSW`, `Kafka`, `ActivitySource`, `Polly`, etc.
  - Anti-pattern detection: delegating math to LLMs, tight coupling to the AI provider, blocking synchronous calls for heavy files.
  - Assigns a score from 0 to 100 in `TechnicalDepth`.

### D. Security & Rigor Judge (Production & Guardrails Auditor)
- **Purpose:** Evaluate whether the solution considers security risks, data governance, and operational stability.
- **Evaluation Criteria:**
  - Mandatory prior sanitization of sensitive data (`RegexPiiSanitizer`, masking before inference or vectorization).
  - Relevance guardrails (`RelevanceScore < 0.78`) and human intervention mechanisms (`Human-in-the-loop`).
  - Cost efficiency and performance: memory, P95/P99 latency, semantic cache, and rate-limit handling (HTTP 429).
  - Assigns a score from 0 to 100 in `ProductionReadiness`.

### E. Scoring Synthesizer (Feedback Consolidator)
- **Purpose:** Unify the evaluations into a structured report and generate precise recommendations.
- **Scoring Formula:** `overallScore = (TechnicalDepth * 0.6) + (ProductionReadiness * 0.4)`.
- **Verdicts:**
  - `PASS`: score >= 80
  - `NEEDS_PRACTICE`: 60 <= score < 80
  - `FAIL`: score < 60

### F. Study Guide Generator (Study Guide Generator)
- **Purpose:** Transform the complete interview history into an actionable study plan.
- **Inputs:** All evaluations of the session (`missedConcepts`, `antiPatterns`, per-question scores), the `JobProfile`.
- **Outputs:** Markdown guide containing: prioritized topics (highest impact first), an explanation of each concept, suggested resources, practice exercises, and a time estimate.
- **Behavior Rules:**
  - Prioritizes by failure frequency × skill weight in the JD.
  - The priority calculations are done in C#; the LLM only drafts explanations and resources.
  - Exportable to PDF.

---

## 3. Immutable Data Contract (JSON Schema)

The evaluation output must obligatorily adhere to this JSON structure:

```json
{
  "$schema": "[http://json-schema.org/draft-07/schema#](http://json-schema.org/draft-07/schema#)",
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