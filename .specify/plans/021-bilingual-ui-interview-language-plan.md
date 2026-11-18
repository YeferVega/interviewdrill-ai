# Technical Plan 021: Bilingual Interface & Interview Language (ES/EN)

## Objective
Ship two orthogonal language controls — UI chrome (frontend dictionary) and interview content (a `language` code that travels through the session) — without touching the immutable `EvaluationPayload` contract or the DB schema.

## Approach
- **Contract-first, backwards compatible.** All backend plumbing uses *optional* parameters placed **after** `CancellationToken` (`StartSessionAsync(..., ct, language)`), so every existing positional caller and test keeps compiling and behaving identically. `null` language always means "historical behavior".
- **Language lives in the session.** `InterviewerSessionState.Language` is persisted inside the existing `InterviewerStateJson` column, so follow-up questions and the closing study guide inherit it without a migration.
- **Deterministic + LLM paths both covered.** Heuristic question banks have full ES/EN template sets; judge prompts receive a one-line output-language instruction; synthesizer advice and study-guide markdown have bilingual phrase sets. Canonical keywords (Polly, HNSW…) are never translated so scoring stays stable.
- **Frontend i18n without dependencies.** Typed `UiStrings` interface with `es`/`en` dictionaries, a parity test, `localStorage` persistence (same pattern as theme), and a Zustand store slice for both language preferences. The interview language is sent on `POST /sessions` and `POST /answers`.

## Decisions
- Default interview language = "es" for new sessions sent by this frontend (matches historical Spanish-first UX); API callers may send "en".
- UI toggle is header-level and instant (no reload); `<html lang>` updated for accessibility.
- The study guide PDF endpoint keeps rendering whatever markdown was stored — its language is decided at generation time.

## Validation
1. Backend suite (265 tests) + new `LanguageSupportTests` and API language theory cases.
2. Frontend: typecheck, build, oxlint, vitest (dictionary parity + toggle behavior).
3. `scripts/e2e-smoke.ps1` — new deterministic bilingual gate (ES question contains "¿", EN question matches English patterns) + existing anti-repetition flow.
4. Live check against NVIDIA SuperVL (`language=es` vs `language=en` on the same JD).

## Rollout
- Restart API + frontend; both servers run from the same build as before (no config changes).
