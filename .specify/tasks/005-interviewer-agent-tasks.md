# Task List: 005-interviewer-agent

## Sprint/Iteration: Phase 2 — Interview Loop

### Tasks

#### Task 1: Implement IInterviewerAgent in InterviewDrill.Agents
- **Description**: Create the concrete class implementing IInterviewerAgent. It generates questions dynamically from JobProfile (skills + responsibilities), never from a static bank.
- **Priority**: High
- **Estimated Time**: 4 hours
- **Dependencies**: Spec 009 (JobProfile), Spec 011 (QuestionRegistry), Core interfaces
- **Status**: [x] Complete — `InterviewerAgent` in `InterviewDrill.Agents`.

#### Task 2: Implement Difficulty Controller in C# (Zero-Math)
- **Description**: Difficulty-adaptation logic in code: score >= 80 raises the level, 60-79 keeps it, < 60 lowers the level and probe_deeper, 2 consecutive FAILs switch skill.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 1, ScoreCalculator and VerdictDeterminer in Core
- **Status**: [x] Complete — `DifficultyController` with tests.

#### Task 3: Integrate anti-repetition (exact + semantic > 0.92)
- **Description**: Before issuing a question, check against QuestionRegistry. If there is a collision, regenerate (max. 3 attempts). Then mark the skill as exhausted.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Spec 011 (QuestionRegistry implemented)
- **Status**: [x] Complete — `InterviewerAgent` registers and retries up to 3 attempts.

#### Task 4: Implement skill coverage ≥ 90%
- **Description**: Plan the interview to cover at least 90% of requiredSkills in 8-10 questions. Coverage guardrail in code.
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 1
- **Status**: [~] In progress — `InterviewSkillPlanner` prioritizes non-exhausted skills by weight and lowest asked count; formal metric guardrail still pending.

#### Task 5: Generate follow-up questions (probe_deeper)
- **Description**: When the answer is superficial, generate a follow-up contextualized to the skill and role of the JD.
- **Priority**: Medium
- **Estimated Time**: 2 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — FAIL keeps the skill and generates `QuestionType.FollowUp`.

#### Task 6: Wire up the SignalR hub for real time
- **Description**: Create a SignalR hub in InterviewDrill.Api to send questions and receive answers without polling.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 1, InterviewDrill.Api
- **Status**: [x] Complete — `InterviewHub`, REST start/answer endpoints, and broadcast per session group. React client still pending.

#### Task 7: Heuristic fallback without LLM (demo mode)
- **Description**: Minimal emergency question bank per skill for when no LLM is available. Mark it as fallback, not the primary mode.
- **Priority**: Low
- **Estimated Time**: 2 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — `HeuristicInterviewerQuestionGenerator` + `FallbackInterviewerQuestionGenerator`.

#### Task 8: Unit tests for the Interviewer Agent
- **Description**: Tests for generation from JobProfile, difficulty adaptation, anti-repetition, skill coverage, follow-up questions.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Tasks 1-7
- **Status**: [~] In progress — unit tests, processor, evaluators, DifficultyController, and API endpoints green; formal coverage ≥90% and SignalR connection tests still pending.

### Definition of Done
- [x] IInterviewerAgent implemented and tested
- [x] The interview closes automatically upon reaching 8 questions
- [x] Difficulty Controller works for the 4 score cases
- [~] Anti-repetition verified (exact and semantic > 0.92) — unit/e2e offline green; real database and real embeddings still pending
- [~] Coverage ≥ 90% of requiredSkills in an 8-question session
- [x] SignalR communication working in backend + React dev server; offline E2E smoke approved
- [x] Unit/integration tests passing for the current loop (>80% critical paths, formal metric still pending)
