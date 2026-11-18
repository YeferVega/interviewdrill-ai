# Task List: 021-bilingual-ui-interview-language

## Sprint/Iteration: Fase 9 — Bilingual UI + interview language (ES/EN)

### Tasks

#### Task 1: Core contracts for language
- **Description**: Add `InterviewLanguages` helper, optional `Language` on `QuestionGenerationRequest`, `InterviewerSessionState`, `AnswerSubmission`, `CandidateResponse`, and optional `language` params (after `ct`) on `IInterviewerAgent`, `IScoringSynthesizer`, `IStudyGuideGenerator`.
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: None
- **Status**: [x] Complete — no existing signature broken; null = historical behavior.

#### Task 2: Agents honor the language
- **Description**: Interviewer stores/forwards language; LLM interviewer + judges receive output-language instructions; heuristic generator gets a full English template bank; synthesizer advice and study guide templates localized (bilingual phrase sets).
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — verified live: ES question fully Spanish, EN fully Spanish→English with SuperVL.

#### Task 3: API & Hub surface
- **Description**: `Language` optional field on `StartInterviewRequest`/`SubmitAnswerRequest`; REST + SignalR pass normalized codes through.
- **Priority**: High
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 1-2
- **Status**: [x] Complete.

#### Task 4: Backend tests
- **Description**: `LanguageSupportTests` (normalizer, heuristic banks, synthesizer ES advice, guide EN, interviewer persistence, processor flow-through) + Api.Tests theory for session language and localized advice.
- **Priority**: High
- **Estimated Time**: 1.5 hours
- **Dependencies**: Tasks 1-3
- **Status**: [x] Complete — suite 265/265 green (Agents 94, Api 15).

#### Task 5: Frontend i18n
- **Description**: `i18n/strings.ts` typed ES/EN dictionaries + parity tests; `services/language.ts` persistence; store slice (`uiLanguage`, `interviewLanguage`, setters); localize all components, verdict/difficulty/progress/date helpers, theme labels, store validation errors; `UiLanguageToggle` + `InterviewLanguageField` components; `<html lang>` sync; CSS for new controls.
- **Priority**: High
- **Estimated Time**: 3 hours
- **Dependencies**: Task 3
- **Status**: [x] Complete — build/lint OK; vitest 19/19.

#### Task 6: E2E bilingual gate
- **Description**: `scripts/e2e-smoke.ps1` starts ES + EN sessions in heuristic mode and asserts question language; keeps the deterministic guarantee for CI.
- **Priority**: Medium
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 3-5
- **Status**: [x] Complete — "E2E bilingual OK" in the smoke run.

### Definition of Done
- [x] UI fully localized ES/EN with persisted choice (localStorage + `<html lang>`)
- [x] Interview questions, feedback prose and study guide follow the chosen language (LLM and heuristic paths)
- [x] `EvaluationPayload` contract and DB schema untouched
- [x] Backend 265/265, frontend 19/19 + build + lint green; E2E passes with the new bilingual step
- [x] Docs: spec/plan/tasks 021; IMPLEMENTATION_STATUS + DEPLOYMENT updated
