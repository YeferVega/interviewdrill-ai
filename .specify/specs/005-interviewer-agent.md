# Specification 005: Interviewer Agent (REVISED)

> ⚠️ **Revision 2026-11-12:** Replaces the static question bank and fixed topics with **dynamic generation from the JobProfile**. Stack: .NET 11 + Microsoft Agent Framework (not TypeScript).

## Overview
The Interviewer Agent is the technical host of the interview. It generates situational questions, production problems, and follow-up questions **from the skills and responsibilities of the `JobProfile`**, adjusts difficulty according to the performance reported by the Scoring Synthesizer, and guarantees that no question repeats across sessions of the same user+JD.

## Requirements

### Functional Requirements
1. Generate questions with an LLM from `JobProfile.requiredSkills` + `responsibilities` (never from a static bank).
2. Maintain the interview session history (persisted, spec 011).
3. Ask direct questions and practical use cases (quota failures, latency, hallucinations, data leakage) **contextualized to the JD role**.
4. Do not hand over the solution before the user answers.
5. Adjust the difficulty of the next question according to performance (easy → medium → hard → expert).
6. **Anti-repetition:** verify the candidate question against the persistent registry (exact + semantic, spec 011) and regenerate if there is a collision (max. 3 attempts).
7. **Skills coverage:** plan the interview to cover ≥ 90% of `requiredSkills` in 8–10 questions (coverage guardrail).
8. Generate follow-up questions (`probe_deeper`) when the answer is superficial.
9. Decide the end of the interview: all planned questions completed or sufficient evaluation signal.
10. Real-time communication via **SignalR** with the React frontend.

### Non-Functional Requirements
1. **Performance**: Question generation < 5s P95 (includes LLM + dedup check).
2. **Context retention**: Full history persisted per question (checkpoint).
3. **Adaptability**: Difficulty adjusts after each EvaluationPayload.
4. **Variety**: 0 semantic repetitions (> 0.85 similarity) for the same user+JD.
5. **Partial determinism**: temperature 0.7 for question variety; the difficulty and coverage decision is C# code (not LLM).

## Difficulty Controller (logic in C#, not LLM)
- `score >= 80` (PASS) → raise one level
- `60 <= score < 80` (NEEDS_PRACTICE) → maintain level
- `score < 60` (FAIL) → lower one level and run `probe_deeper` on the same skill
- 2 consecutive FAILs on the same skill → switch skills (avoid frustration)

## User Stories
- as a **candidate**, I want questions about the real skills of the position to practice what is relevant
- as a **candidate**, I want the questions to be new when I retake the interview
- as a **system**, I want to adapt the difficulty to keep the candidate in their learning zone

## Acceptance Criteria
- [x] Generates questions from JobProfile (test: 2 different JDs → different questions)
- [~] Does not reveal solutions prematurely (heuristic and LLM prompt instruct it; formal semantic validation pending)
- [x] Difficulty adaptation according to the Difficulty Controller matrix (tests of the 4 cases)
- [x] Exact and semantic anti-repetition verified (tests with spec 011)
- [ ] Coverage ≥ 90% of requiredSkills in an 8-question session
- [x] The interview ends upon reaching the maximum of 8 questions
- [x] Follow-up questions after superficial answers
- [~] Session survives restart (EF store implemented; real application/validation in Azure SQL/AWS SQL pending)
- [x] Immediate feedback with EvaluationPayload after each answer
- [x] Functional SignalR communication in backend + React client (smoke E2E of endpoints and UI dev server approved)
- [x] Heuristic fallback without LLM (demo/offline mode)

## Out of Scope
- Answer evaluation (evaluators, specs 003/004)
- Scores calculation (Scoring Synthesizer, spec 002)
- Study guide generation (spec 010)
- Frontend UI (spec 008)

## Dependencies
- **Spec 009**: JobProfile
- **Spec 011**: Persistence and anti-repetition
- **Spec 002**: EvaluationPayload of the Scoring Synthesizer
- **LLM provider** with structured output

## Risks
1. **Risk**: Very difficult questions demotivate → *Mitigation*: Difficulty Controller with gradual step-down.
2. **Risk**: LLM generates a repeated question → *Mitigation*: double-layer dedup + bounded regeneration.
3. **Risk**: Context window overflow in long sessions → *Mitigation*: history summarization + only the last N interactions are sent to the LLM.
4. **Risk**: No LLM available → *Mitigation*: minimal per-skill emergency heuristic bank (fallback only, not the main mode).
