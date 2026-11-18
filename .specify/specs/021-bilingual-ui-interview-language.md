# Specification 021: Bilingual Interface & Interview Language (ES/EN)

## Overview
The UI ships in Spanish; the interview pipeline historically mixes languages (LLM questions in English, deterministic banks in Spanish, English advice, Spanish guides). This unit gives the user two independent language controls:

1. **Interface language** — a header toggle (ES/EN) that localizes every UI string and persists in `localStorage`.
2. **Interview language** — a choice made at JD upload or at profile review, persisted per browser, that tells the backend which language to use for questions, judge feedback prose and the study guide — with a real LLM (NVIDIA SuperVL) or with the deterministic heuristic agents.

## Stack
- **React 19 / Vite / Zustand** — i18n dictionary + toggles (no new dependencies)
- **.NET 11 minimal APIs + SignalR hub** — optional `language` ("es"/"en") on session start and answer submission
- **NVIDIA NIM (OpenAI-compatible)** — prompt-level output language instruction
- **SQL Server persistence** — session language travels inside the already-serialized `InterviewerStateJson` (no migration)

## User Stories
- **As a** Spanish-speaking candidate, **I want** the whole interview (questions, advice, guide) in Spanish, **so that** I can train without context switching.
- **As a** candidate preparing for an English-speaking loop, **I want** questions and feedback in English, **so that** I practice in the target language.
- **As a** user, **I want** to change the interface language instantly and keep my choice across reloads, **so that** the tool feels native.

## Functional Requirements
### FR1: UI localization
- [x] All static strings live in `frontend/src/i18n/strings.ts` with `es` and `en` dictionaries behind a typed `UiStrings` interface (key parity enforced by tests).
- [x] `UiLanguageToggle` in the header switches ES/EN; selection persisted (`interviewdrill:ui-language`) and applied to `<html lang>`.
- [x] Verdict labels, difficulty labels, progress, dates (`Intl` locale), theme labels, validation messages and API-failure fallbacks are localized.

### FR2: Interview language selection
- [x] `InterviewLanguageField` radio group (Español / English) on the JD upload page and the profile review page; persisted (`interviewdrill:interview-language`).
- [x] The choice is sent on `POST /api/interview/sessions` and `POST /api/interview/sessions/{id}/answers` (`language` field, default "es").

### FR3: Backend language propagation
- [x] `InterviewLanguages.Normalize` accepts aliases (`ES`, `es-ES`, `English`, `en-US`…); unknown → null → historical behavior.
- [x] Session language is stored in `InterviewerSessionState.Language` (JSON state column — no DB migration) and reused for every follow-up question.
- [x] LLM question generation receives an explicit output-language instruction; heuristic generator switches between full ES/EN template banks.
- [x] Judge prompts append a prose-language instruction (canonical keywords stay untranslated); `ScoringSynthesizer` has bilingual advice phrases; `StudyGuideGenerator` emits the whole guide (topics, priority labels, exercises) in the chosen language.
- [x] The immutable `EvaluationPayload` JSON contract is untouched: only free-text values change language.

## Non-Functional Requirements
- [x] Null/absent language preserves the historical outputs byte-for-byte where asserted by existing tests (deterministic default: Spanish for heuristic banks/guides, English for synthesizer advice).
- [x] No new runtime dependencies (frontend or backend).

## Acceptance Criteria (verified 2026-11-16)
- [x] Backend suite: 265/265 green (Agents 94, Api 15 incl. new language tests).
- [x] Frontend: `tsc -b && vite build` OK, oxlint OK, vitest 19/19 (dictionary parity + toggles).
- [x] `scripts/e2e-smoke.ps1` (Heuristic): bilingual gate — `language=es` question contains "¿", `language=en` question matches English patterns.
- [x] Live NVIDIA SuperVL: session with `language=es` returned a fully Spanish failure-scenario question; `language=en` returned an English one.

## Out of Scope
- Translating the canonical technical keyword lists (`coveredKeywords`, `missedConcepts`).
- Locizing the JD parser (profile fields mirror the job posting's own language).
- Other UI languages beyond ES/EN.

## Dependencies
- Specs 005/009 (interviewer), 003/004 (judges), 007 (study guide), 012 (OpenAI-compatible client), 020 (local run).

## Risks
| Risk | Mitigation |
|------|------------|
| LLM ignores the language instruction | Explicit sentence appended to system prompt; heuristic fallback is fully bilingual |
| Old sessions have `Language=null` | Null keeps historical behavior; new answers default per request |
| UI key drift between dictionaries | Parity unit test fails the build on any missing key |
