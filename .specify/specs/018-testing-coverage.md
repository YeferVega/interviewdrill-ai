# Specification 018: Testing Coverage Hardening

## Overview
Raise the coverage of the **InterviewDrill.Agents** and **InterviewDrill.Infrastructure** projects up to the Constitution's thresholds (≥ 80% on critical paths) without touching the production runtime. A measured baseline is added, tests targeting the real gaps, and a standard coverage filter that excludes generated code so the metric reflects only hand-written logic.

## Stack
- **.NET 11 / C# 15**, xUnit, coverlet (`--collect:"XPlat code coverage"`)
- **Microsoft.EntityFrameworkCore.InMemory** (only in `InterviewDrill.Infrastructure.Tests`) to exercise the SQL repositories without SQL Server
- **`backend/coverage.runsettings`** with `Exclude`/`ExcludeByAttribute` for generated code

## User Stories
- **As an** engineer, **I want** tests for the in-memory repositories and heuristics that were previously at 0%, **so that** I cover the demo/offline routes.
- **As an** engineer, **I want** tests for PII logging, hash embeddings, and edge-case LLM providers, **so that** there are no gaps in security/LLM.
- **As a** reviewer, **I want** the coverage metric to exclude migrations and primary-constructor artifacts, **so that** the figure is honest.

## Functional Requirements
- [x] Per-class baseline measured with `--collect:"XPlat Code Coverage"`.
- [x] **Agents**: `InMemoryQuestionRegistry`, `InMemoryInterviewAnswerRepository`, `InMemoryStudyGuideRepository`, `InMemorySessionHistoryRepository`, `HeuristicInterviewerQuestionGenerator`, `FallbackInterviewerQuestionGenerator` (previously 0%).
- [x] **Agents**: `AnchoredRubricMath` (0→100%): `Score1To5`, `Normalize`, `WeightedNormalized`, `CombineDeterministicAndJudge`.
- [x] **Infrastructure**: `SqlQuestionRepository` (hash + semantic dedup in memory), `SqlInterviewAnswerRepository` (serialization, invalid skip, per-session scope), `SqlStudyGuideRepository` (sanitizes + update-preserves-id), `SqlSessionRepository` (GetState stats + SetCompleted).
- [x] **Infrastructure**: `EfInterviewerSessionStore` (creates the User→JD→Session aggregate, updates ProfileJson, sanitizes PII, load, MarkCompleted) with EF InMemory.
- [x] **Infrastructure**: `HashEmbeddingGenerator` (dim, normalization, determinism, null, cancellation), `UnavailableLlmCompletionClient`, `BedrockLlmClient` (empty ModelId and SigV4), `PiiSanitizingLoggerProvider/Logger`, `SimpleOptionsMonitor<T>`.
- [x] **Infrastructure**: `JdDocumentTextExtractor` (null, uppercase extension, invalid PDF, empty TXT).
- [x] `coverage.runsettings` excludes `*.Migrations*` + generated attributes; the honest logic coverage is measured.

## Non-Functional Requirements
- [x] **Isolated** tests: no network, no real LLM, no SQL Server (InMemory for EF).
- [x] **No regression**: existing tests keep passing; final suite **238/238**.
- [x] **Production code is not modified** in this spec (tests + measurement config only).

## Acceptance Criteria
- [x] Agents: 72.2% → **~90%** (baseline vs post).
- [x] Infrastructure: 39.6% → **~71%** honest (hand-written logic).
- [x] Full suite Core+Agents+Infra+Api **238/238**.
- [x] E2E (in-memory and `-UseDatabase`) green, anti-repetition OK.
- [x] `build` with 0 errors.

## Out of Scope
- Binary PDF fixture for the happy path of `JdDocumentTextExtractor.ExtractPdf` (still pending; the error path is covered).
- Reading of `appsettings.json` by `InterviewDrillDbContextFactory` in test (fragile due to CWD; the fallback is covered).
- Coverage of the Api's `Program.cs` (bootstrapping; covered via E2E).

## Dependencies
- Specs 005/007/010/011/012/013/014 (the classes under test).

## Risks
| Risk | Mitigation |
|------|------------|
| coverlet reports low primary-constructors numbers (e.g. `SqlSessionRepository` 25%) | The real async state machines are at 100%; documented as an artifact and coverage prioritized per method |
| EF InMemory diverges from real SQL Server | Combined with the `-UseDatabase` E2E gate, which does use SQL Server Express |

## Metrics (baseline → target achieved)
| Project | Tests before | Tests now | Coverage before | Honest coverage now |
|----------|------------|-------------|----------------|------------------------|
| Agents | 42 | 81 | 72.2% | ~90.8% |
| Infrastructure | 58 | 110 | 39.6% | ~71.6% (logic) |
| Core | 35 | 35 | — | — |
| Api | 12 | 12 | — | — |
| **Total** | **147** | **238** | | |
