# Specification 010: Study Guide Generator Agent

## Overview
Agent that, at the end of the interview, consolidates all of the session's evaluations and generates a **detailed, prioritized study guide** of the topics the candidate must study to pass the real interview.

## Functional Requirements
1. Receive the complete history: `EvaluationPayload[]` per question + `JobProfile`.
2. **Priority calculation in C# (Zero-Math):**
   `priority(skill) = failureFrequency × jdWeight × (1 - avgScore/100)`
   - `failureFrequency`: how many times the skill appeared in `missedConcepts`/`antiPatterns`.
   - `jdWeight`: weight of the skill in the JobProfile (default 0.5 if not present).
3. Group failed concepts by topic (e.g. "Resilience" groups Polly + rate limiting + circuit breaker).
4. For each prioritized topic, the LLM generates (temperature 0.3, structured output):
   - Explanation of the concept (2–4 paragraphs) oriented toward interviews.
   - Why it matters **for this role** (JD context).
   - 2–3 study resources (official documentation first).
   - 1 practice exercise with success criteria.
   - Study time estimate.
5. Include a "What you already master" section (consolidated strengths) to reinforce confidence.
6. Output in **Markdown**, exportable to **PDF**.
7. The guide is persisted and visible in the session history.

## Non-Functional Requirements
- Generation < 10s P95.
- Maximum 7 prioritized topics (avoid overwhelming the user).
- No PII in the guide (the history already comes sanitized).

## Acceptance Criteria
- [x] Priority computed in C# code with tests (not delegated to the LLM)
- [x] Topics ordered by descending impact
- [x] Each topic includes: explanation, relevance to the JD, resources, exercise, time
- [x] Strengths section present
- [~] Markdown + PDF export (Markdown download + print-to-PDF UI; formal server-side PDF pending)
- [~] Tests with golden dataset (3 simulated sessions → expected guides) — initial tests done, formal dataset pending

## Out of Scope
- Multi-week study plan with calendar (phase 2).
- Study progress tracking (phase 2).

## Dependencies
- Scoring Synthesizer (spec 002): provides EvaluationPayload[]
- JD Parser (spec 009): provides skill weights
- Persistence (spec 011): stores the guide
