# Implementation Status — InterviewDrill AI

Last updated: November 16, 2026

This file is the live source of truth for the project's implementation status, in accordance with Article 6 of the Constitution.

---

## Executive Summary

| Metric | Value |
|---------|-------|
| Unit tests | Core 35, Agents 94, Infrastructure 121, Api 15 = **265/265** ✅ |
| Infrastructure tests | 58/58 ✅ (after turning Smart App Control off on the host). Anti-repetition additionally verified end-to-end via E2E `-UseDatabase`. |
| Build | ✅ 0 errors |
| E2E smoke (includes anti-repetition) | ✅ Passed — session 2 requested 8 new questions, 0 overlap (in-memory **and** `-UseDatabase`). `-UseDatabase` accepts an override via `INTERVIEWDRILL_CONNECTION_STRING` (Azure/AWS or another instance) with fallback to SQLEXPRESS |
| Formal coverage (Agents) | **~90.8%** (was 72.2%) — logic only, excludes generated code (`coverage.runsettings`) |
| Formal coverage (Infrastructure) | **~72.9%** (was 39.6%) — logic only, excludes generated code (`coverage.runsettings`). `JdDocumentTextExtractor` 100%, factory 95.8% |
| Formal coverage (Core) | 35 tests; the raw `line-rate` reflects trivial records/interfaces |
| Pending coverage | *(None relevant — the PDF happy path and the factory's reading of `appsettings.json` were closed in Phase 6)* |
| SQL Server Express database | ✅ `InterviewDrill_Testing` created on `localhost\SQLEXPRESS` (migration `20261116133001_InitialCreate`); E2E `-UseDatabase` green |
| OpenAI-compatible LLM providers | ✅ Code, DI, user-secrets and script ready for NVIDIA/OpenAI/OpenCode. **Verified for real**: NVIDIA SuperVL → `source=llm` (HTTP 200); OpenCode → 429 without Go; OpenAI → 429 without balance |
| Documented specs | 20 (000-014, 017-021) |
| Documented plans | 20 (000-014, 017-021) |
| Documented tasks | 22 (000-021) |

---

## Status by Specification (Specs 000-014)

| Spec | Name | Status | Plan | Tasks | Implemented Code |
|------|--------|--------|------|-------|---------------------|
| 000 | Product Vision | ✅ Complete | ✅ Complete | ✅ Complete | N/A (vision) |
| 001 | Project Initialization | ✅ Complete | ✅ Complete | ✅ Complete | ✅ SDD structure created |
| 002 | Scoring Synthesizer | ✅ Complete | ✅ Complete (.NET) | ✅ Complete | ✅ ScoreCalculator, VerdictDeterminer, AnchoredRubricMath, ScoringSynthesizer (26 tests) |
| 003 | Architecture Evaluator | ✅ Complete | ✅ Complete (.NET) | ~[~] Partial | ✅ HybridArchitectureEvaluator, HeuristicArchitectureEvaluator, LlmArchitectureJudge |
| 004 | Security & Rigor Judge | ✅ Complete | ✅ Complete (.NET) | ~[~] Partial | ✅ HybridSecurityRigorJudge, HeuristicSecurityRigorJudge, LlmSecurityJudge |
| 005 | Interviewer Agent | ✅ Complete | ✅ Complete (.NET) | ~[~] Partial | ✅ InterviewerAgent, DifficultyController, InterviewHub |
| 006 | PII Sanitizer | ✅ Complete | ✅ Complete (.NET) | ~[~] Partial | ✅ RegexPiiSanitizer, IPiiSanitizer |
| 007 | Semantic Cache | ✅ Complete | ✅ Complete (.NET) | ✅ Complete | ✅ SemanticCacheService, ISemanticCache + 10 tests; varbinary(max) compatible SQL Server Express |
| 008 | Frontend UI | ✅ Complete | ✅ Complete (React 19) | ~[~] Partial | ✅ React 19 + Vite + Zustand + SignalR |
| 009 | JD Parser Agent | ✅ Complete | ✅ Complete (.NET) | ~[~] Partial | ✅ LlmJdParserAgent, HeuristicJdParserAgent, endpoint /api/jd/parse |
| 010 | Study Guide Generator | ✅ Complete | ✅ Complete (.NET) | ~[~] Partial | ✅ StudyGuideGenerator, prioritization in C# |
| 011 | Persistence & Anti-Repetition | ✅ Complete | ✅ Complete (.NET) | ~[~] Partial | ✅ EF Core, entities, QuestionRegistry, SessionStore, migration 20261116133001 applied |
| 012 | LLM Providers | ✅ Complete | ✅ Complete (.NET) | ✅ Complete | ✅ `OpenAiCompatibleLlmClient` (NVIDIA NIM/OpenAI), `AzureOpenAILlmClient`, `BedrockLlmClient`, `OllamaLlmClient`; `scripts/llm-check.ps1` |
| 013 | Observability & Resilience | ✅ Complete | ✅ Complete (.NET) | ~[~] Partial | ✅ ActivitySource, OpenTelemetry, Polly, appsettings.json, PiiSanitizingLogger |
| 014 | PII Full Pipeline | ✅ Complete | ✅ Complete (.NET) | ✅ Complete | ✅ PII at all LLM and persistence points + PII-free logging + 4 integration tests |

---

## Code Implementation (Phases 2 and 3 Completed)

### Implemented components:

| Component | File | Status |
|------------|---------|--------|
| ActivitySourceProvider | `InterviewDrill.Core/Observability/ActivitySourceProvider.cs` | ✅ Implemented |
| PollyPolicies | `InterviewDrill.Infrastructure/Resilience/PollyPolicies.cs` | ✅ Implemented |
| AzureOpenAILlmClient | `InterviewDrill.Infrastructure/Llm/AzureOpenAILlmClient.cs` | ✅ Polly + OpenTelemetry |
| OllamaLlmClient | `InterviewDrill.Infrastructure/Llm/OllamaLlmClient.cs` | ✅ Polly + OpenTelemetry + BaseAddress |
| BedrockLlmClient | `InterviewDrill.Infrastructure/Llm/BedrockLlmClient.cs` | ✅ Polly + OpenTelemetry + SigV4 skeleton |
| SemanticCacheService | `InterviewDrill.Infrastructure/Caching/SemanticCacheService.cs` | ✅ In-memory + embeddings + cosine similarity + PII sanitization |
| ISemanticCache | `InterviewDrill.Core/Caching/ISemanticCache.cs` | ✅ Interface in Core |
| SqlStudyGuideRepository (PII) | `InterviewDrill.Infrastructure/Persistence/SqlStudyGuideRepository.cs` | ✅ Sanitizes Markdown before persisting |
| EfInterviewerSessionStore (PII) | `InterviewDrill.Infrastructure/Persistence/EfInterviewerSessionStore.cs` | ✅ Sanitizes state before serializing |
| OpenTelemetry DI | `InterviewDrill.Api/Program.cs` | ✅ Configured with 5 ActivitySources |
| HttpClient Timeout | `InterviewDrill.Api/Program.cs` | ✅ 30 seconds for LLM clients |
| PiiSanitizingLoggerProvider | `InterviewDrill.Infrastructure/Logging/PiiSanitizingLoggerProvider.cs` | ✅ Logging without PII or secrets |
| SimpleOptionsMonitor | `InterviewDrill.Infrastructure/Logging/PiiSanitizingLoggerProvider.cs` | ✅ IOptionsMonitor for ConsoleLoggerProvider |
| QuestionEntity (varbinary) | `InterviewDrill.Infrastructure/Persistence/Entities/QuestionEntity.cs` | ✅ Embedding = `byte[]` (SQL Server Express compatible) |
| QuestionRegistry (byte[]) | `InterviewDrill.Infrastructure/Persistence/QuestionRegistry.cs` | ✅ Serializes float[] to byte[] |
| SqlQuestionRepository (memory) | `InterviewDrill.Infrastructure/Persistence/SqlQuestionRepository.cs` | ✅ In-memory semantic search with VectorMath.CosineSimilarity |
| appsettings.json | `InterviewDrill.Api/appsettings.json` | ✅ Sections OpenTelemetry, Resilience, ConnectionStrings (DefaultConnection + ProductionConnection) |
| appsettings.Development.json | `InterviewDrill.Api/appsettings.Development.json` | ✅ DefaultConnection for the development environment |

### New tests (Phase 3):

| Test | File | Cases |
|------|---------|-------|
| SemanticCacheServiceTests | `backend/tests/InterviewDrill.Infrastructure.Tests/Caching/SemanticCacheServiceTests.cs` | 10 tests (miss, hit, threshold, TTL, invalidation, namespaces, statistics, PII) |
| LlmPiiIntegrationTests | `backend/tests/InterviewDrill.Agents.Tests/LlmPiiIntegrationTests.cs` | 4 tests (ArchitectureJudge, SecurityJudge, QuestionGenerator, JD Parser PII sanitization) |
| InterviewDrillDbContextModelTests (updated) | `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/InterviewDrillDbContextModelTests.cs` | 2 updated tests: varbinary(max), no vector index |

### New tests (Phase 5 — Coverage, spec/plan/tasks 018):

| Test | File | Area |
|------|---------|------|
| InMemoryAndHeuristicGeneratorsTests | `backend/tests/InterviewDrill.Agents.Tests/InMemoryAndHeuristicGeneratorsTests.cs` | In-memory repos (0%→covered) + Heuristic/Fallback generator |
| AnchoredRubricMathTests | `backend/tests/InterviewDrill.Agents.Tests/AnchoredRubricMathTests.cs` | Normalization/anchored rubrics |
| HashEmbeddingAndUnavailableLlmTests | `backend/tests/InterviewDrill.Infrastructure.Tests/Llm/HashEmbeddingAndUnavailableLlmTests.cs` | Hash embeddings, Unavailable and Bedrock edge cases |
| PiiSanitizingLoggerTests | `backend/tests/InterviewDrill.Infrastructure.Tests/Logging/PiiSanitizingLoggerTests.cs` | PII logging, SimpleOptionsMonitor |
| SqlRepositoriesTests | `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/SqlRepositoriesTests.cs` | SqlQuestion + EfInterviewerSessionStore (EF InMemory) |
| SqlAnswerAndGuideRepositoriesTests | `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/SqlAnswerAndGuideRepositoriesTests.cs` | Answers/Guide/History SQL (EF InMemory) |
| SqlSessionRepositoryTests | `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/SqlSessionRepositoryTests.cs` | Session state + MarkCompleted (EF InMemory) |
| InterviewDrillDbContextFactoryTests | `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/InterviewDrillDbContextFactoryTests.cs` | Factory: env var, appsettings.json reading (temp CWD) and fallback |
| JdDocumentTextExtractorTests | `backend/tests/InterviewDrill.Infrastructure.Tests/Documents/JdDocumentTextExtractorTests.cs` | +null, uppercase extension, empty TXT, happy-path PDF (generated with QuestPDF) + invalid PDF/DOCX |

**Tooling**: `backend/coverage.runsettings` (excludes migrations and generated code in order to measure real logic).

### Added NuGet packages:

| Project | Package | Version |
|----------|---------|---------|
| InterviewDrill.Infrastructure | Polly | 7.2.3 |
| InterviewDrill.Infrastructure | Polly.Extensions.Http | 2.0.1 |
| InterviewDrill.Infrastructure | OpenTelemetry | 1.9.0 |
| InterviewDrill.Infrastructure | OpenTelemetry.Exporter.Console | 1.9.0 |
| InterviewDrill.Api | OpenTelemetry.Extensions.Hosting | 1.9.0 |
| InterviewDrill.Api | Microsoft.EntityFrameworkCore.Design | 11.0.0 |
| InterviewDrill.Infrastructure.Tests *(test only)* | Microsoft.EntityFrameworkCore.InMemory | 11.0.0 |

---

## SQL Server Express Database

| Item | Value |
|----------|-------|
| Server | `localhost\SQLEXPRESS` |
| Database | `InterviewDrill_Testing` |
| Authentication | Windows (Trusted_Connection) |
| User | `YOUR-MACHINE\your-user` |
| Applied migration | `20261116133001_InitialCreate` |
| Created tables | Users, JobDescriptions, InterviewSessions, Questions, Answers, StudyGuides |
| Connection string | `Server=localhost\SQLEXPRESS;Database=InterviewDrill_Testing;Trusted_Connection=True;TrustServerCertificate=True;` |

### To use the DB in the API:
```powershell
$env:INTERVIEWDRILL_CONNECTION_STRING = "Server=localhost\SQLEXPRESS;Database=InterviewDrill_Testing;Trusted_Connection=True;TrustServerCertificate=True;"
```

---

## Code Fixes (Phase 3)

| Fix | File | Description |
|------------|---------|-------------|
| PII sanitization consistency | `SemanticCacheService.SetAsync` | The Query is sanitized before generating the embedding (consistent with GetAsync) |
| OpenTelemetry/Resilience configuration | `appsettings.json` | Sections added for configuration without recompiling |
| PII-free logging | `PiiSanitizingLoggerProvider` | Wraps ILoggerProvider to filter PII and secrets out of all logs |
| SQL Server Express connection | `appsettings.json` | `DefaultConnection` with `localhost\SQLEXPRESS;Database=InterviewDrill_Testing` |
| Embedding model (vector → varbinary) | `QuestionEntity.cs` | `SqlVector<float>` → `byte[]`; `varbinary(max)` instead of `vector(1536)` |
| Semantic search (SQL Server Express) | `SqlQuestionRepository.cs` | Rewritten for in-memory search with `VectorMath.CosineSimilarity` |
| Embedding serialization | `QuestionRegistry.cs` | `float[]` → `byte[]` via `SelectMany(BitConverter.GetBytes).ToArray()` |
| Regenerated migrations | `backend/src/InterviewDrill.Infrastructure/Persistence/Migrations/` | `20261116133001_InitialCreate` (corrected model, no vector index) |
| Updated model tests | `InterviewDrillDbContextModelTests.cs` | `varbinary(max)` and no vector index (instead of vector(1536) and a cosine index) |

---

## Code Fixes (Phase 4 — Anti-Repetition)

| Fix | File | Description |
|------------|---------|-------------|
| **Anti-repetition BUG** | `Program.cs` | `InMemoryQuestionRegistry` changed from `AddScoped` to `AddSingleton`. Because it was Scoped, it was recreated on every HTTP request and lost the history of questions already asked, so the second session repeated questions from the first. With Singleton (and a thread-safe `ConcurrentDictionary`) the registry persists across requests and sessions of the process. |
| Anti-repetition E2E verification | `scripts/e2e-smoke.ps1` | Refactored with an `Invoke-InterviewRun` function; it runs a second session with the same user+JD and verifies that the questions do NOT overlap with those of the first session. |
| Core tests expanded | `VectorMathTests.cs` | +3 cases: empty vectors (exception), zero-norm vector, and opposite vectors (similarity −1). Core coverage rises from 32 to 35 tests. |

---

## Known Issues and Environment Limitations

| Issue | Description | Workaround / Status |
|----------|-------------|---------------------|
| Infrastructure.Tests blocked (resolved) | Windows Defender Application Control (Windows 11 Smart App Control) blocked loading the test assembly (0x800711C7), even in regular PowerShell. | **RESOLVED**: after turning Smart App Control off on the host, `InterviewDrill.Infrastructure.Tests` run **58/58** (Core+Agents+Api+Infra = **147/147**). If the block returns or it is a managed WDAC policy, alternative method: confirm anti-repetition/model via `scripts/e2e-smoke.ps1 -UseDatabase` (it starts the API per process and does not depend on the test assembly). |
| Core coverage (35 tests) | Core had gaps in VectorMath edge cases | Empty, zero-norm, and opposite vectors were added. ScoreCalculator/VerdictDeterminer already covered the boundaries. |
| SQL Server Express migrations | Does not support the `VECTOR` type or `VectorDistance` | Use `varbinary(max)` + in-memory search with `VectorMath.CosineSimilarity` (fixed in Phase 3). |

---

## Real DB Integration (Option 1, Phase 4)

| Aspect | Status |
|---------|--------|
| API connected to SQL Server Express | ✅ via `INTERVIEWDRILL_CONNECTION_STRING` → `InterviewDrill_Testing` |
| Aggregate materialization in the DB | ✅ `EfInterviewerSessionStore.SaveAsync` creates `User → JobDescription → InterviewSession` with intermediate `SaveChangesAsync` calls (no FK violation) |
| Questions/answers/guide persisted | ✅ `SqlQuestionRepository`, `SqlInterviewAnswerRepository`, `SqlStudyGuideRepository` |
| Anti-repetition against the real DB | ✅ E2E `-UseDatabase`: 2 sessions same user+JD → 8 new questions, 0 overlap (dedup reading from `Questions`) |
| Mode detection in health | ✅ `/api/health` reports `persistence='sql-server'` |
| Formalized E2E gate | ✅ spec/plan/tasks 017 (`scripts/e2e-smoke.ps1` with/`-UseDatabase`) |
| Configurable connection string (Option B) | ✅ `-UseDatabase` first uses `INTERVIEWDRILL_CONNECTION_STRING` (for Azure/AWS or another instance) and does a *fallback* to `localhost\SQLEXPRESS;InterviewDrill_Testing` |
| Reconciled Constitution | ✅ persistence stack (varbinary+cosine on Express), quality gates and DoD updated |

---

## Coverage Improvement (Phase 5, spec/plan/tasks 018)

No changes to production code: only tests + measurement filter.

| Project | Tests | Coverage (logic) | How it was measured |
|----------|-------|-------------------|---------------|
| Agents | 42 → **81** | 72.2% → **~90.8%** | `dotnet test --settings backend/coverage.runsettings --collect:"XPlat code coverage"` |
| Infrastructure | 58 → **110** | 39.6% → **~71.6%** | idem (excludes generated code) |
| **Total suite** | 147 → **238** | | Core 35 + Agents 81 + Infra 110 + Api 12, all ✅ |

**What was closed**: in-memory and heuristic repos (previously 0%), `AnchoredRubricMath`, SQL repos + `EfInterviewerSessionStore` via **EF InMemory** (no SQL Server), LLM edge cases (`HashEmbedding`/`Unavailable`/`Bedrock`), PII logging and `JdDocumentTextExtractor`.

**Metric honesty**: `coverage.runsettings` excludes `*.Migrations*` and generated attributes. Left out of scope are the PDF binary fixture (happy path) and the factory's reading of `appsettings.json`.

---

## CI/CD and Deployment (Phase 6, spec/plan/tasks 019)

| Item | Artifact | Description |
|----------|-----------|-------------|
| CI pipeline | `.github/workflows/ci.yml` | `backend` (build + test + honest coverage with `coverage.runsettings` + upload), `frontend` (oxlint + vitest + vite build), `e2e` on `windows-latest` (`scripts/e2e-smoke.ps1` in-memory mode, anti-repetition) |
| Deployment guide | `DEPLOYMENT.md` | Environment variables, connection string, and steps for **Azure SQL** (+ Managed Identity) and **AWS RDS**; migrations; `persistence='sql-server'` verification; validation with `-UseDatabase` |
| Gates in the Constitution | `memory/constitution.md` | CI is the automated Definition of Done; `-UseDatabase` local/self-hosted only (requires SQL Server) |
| Coverage closed | `backend/tests/…` | `JdDocumentTextExtractor` 100% (PDF happy path via QuestPDF); `InterviewDrillDbContextFactory` 95.8% (env var, appsettings, fallback) |

**E2E matrix** (see `DEPLOYMENT.md`): default `in-memory` (hosted CI) and `-UseDatabase` (local/self-hosted) with the **same** `scripts/e2e-smoke.ps1`.

---

## LLM Providers and Local Validation (2026-11-16)

- **New OpenAI-compatible provider** (`OpenAiCompatibleLlmClient` + `OpenAiCompatibleOptions`): covers **NVIDIA NIM / OpenAI / OpenCode** (`BaseUrl`, `ApiKey` via env, `Model`, `MaxTokens/TopP/EnableThinking`, `reasoning_content` fallback). Foundry/Bedrock/Ollama remain open.
- **`scripts/llm-check.ps1`**: local connectivity ping with `-Provider Nvidia|OpenAI|OpenCode|Custom` (never hardcodes). For NVIDIA it replicates `max_tokens=16384, top_p=0.95, chat_template_kwargs.enable_thinking=true`.
- **Verified base model (2026-11-16)**: `nvidia/nemotron-3.5-super-vl-120b-a12b` → `/api/jd/parse` returned `profileSource=llm` (~7-8s) via `LLM:Provider=OpenAICompatible`. NIM IDs in **lowercase with hyphens** (PascalCase/retired ones give 404/410). `ApiKey` in **user-secrets** (`dotnet user-secrets set "LLM:OpenAICompatible:ApiKey"` with `UserSecretsId`), not in the repo.
- **E2E with a real model**: `scripts/e2e-smoke.ps1 -WithLlm` with SuperVL and `thinking=OFF, max_tokens=4096` fully green (includes anti-repetition). Thinking ON (16384 tokens) is too slow for the gate.
- **Test determinism**: `HeuristicApiFactory` (env `Testing` + override `LLM:Provider=Heuristic`) so that the Api.Tests `WebApplicationFactory` does **not** load user-secrets/keys or make network calls; deterministic suite.
- **Bilingual ES/EN (spec/plan/tasks 021, 2026-11-16)**: the complete UI is localized with a typed dictionary (`frontend/src/i18n/strings.ts`) + a persisted ES/EN toggle; the interview language (`language: "es"|"en"`) travels in `POST /sessions`/`POST /answers`, is stored in `InterviewerStateJson` (no migration), and controls questions (bilingual heuristic bank + LLM prompt instruction), the synthesizer's advice, the judges' prose, and the study guide. The `EvaluationPayload` contract is intact. Verified for real with SuperVL: `language=es` → question 100% in Spanish; `language=en` → English. New gate in `e2e-smoke.ps1` ("E2E bilingual OK"). Suite **265/265**; frontend build+lint+vitest 19/19.
- **CI annotation cleanup + security audit (2026-11-18)**: removed duplicate `using` directives and unused test-fixture field; replaced `BuildServiceProvider()` in the logging pipeline with a directly instantiated shared `RegexPiiSanitizer` (fixes ASP0000); dropped the invalid `include-prerelease` input from `setup-dotnet@v4`; upgraded `OpenTelemetry*` 1.9.0 → **1.21.0** and `Microsoft.AspNetCore.OpenApi` rc.3 → **11.0.0** (fixes advisories NU1902/NU1903 that GitHub flagged). Local build now emits **0 warnings / 0 errors**; suite remains **265/265**. First GitHub Actions run: all 4 jobs green (24 warnings → addressed here).
- **Secrets hardening & publication prep (2026-11-16)**: README rewritten (English, current state: providers, languages, gates, ports). `.gitignore` hardened: env files, `appsettings.Production.json`, private keys/certs (`*.key/*.pem/*.p12/id_*`), cloud credentials (`.aws/`, `credentials*`, `.npmrc`, `.git-credentials`), DB dumps (`*.mdf/*.ldf/*.bak`) and temp files. Necessary exception: `!frontend/package-lock.json` (without a lockfile, CI `npm ci` failed). CI gained a `security` job = secret scan on `nvapi-…{20,}/sk-…/ghp_…/AKIA…/PEM` patterns that fails the build on real credentials (test folders excluded — their PII fixtures deliberately embed fake keys). Verified: 0 hits outside fixtures + YAML valid. Git repo initialized: single initial commit, clean tree, no secrets.
- **`switch` configuration**: `llmProvider` is read with `builder.Configuration["LLM:Provider"]` (respects env + files).
- **Provider robustness**: NIM timeouts/5xx/parsing are mapped to `LlmUnavailableException` → the Interviewer/JD/evaluator wrappers degrade to deterministic heuristics (no 500).
- **Real test with the user's key (OpenAI)** `sk-proj-…`: **429 insufficient_quota** (valid, no balance; OpenAI format, not NVIDIA).
- **Real NVIDIA test** `nvapi-…`: **HTTP 200** via `/api/jd/parse` with SuperVL (see above).
- **Real OpenCode test** `oc_sk_…`: **429 FreeUsageLimitError** (it authenticates, no Go). The client supports `LLM:Provider=OpenCode`; requires Go or a daily reset.
- 🔴 **Security**: the keys (`nvapi-…`, `oc_sk_…`, `sk-proj-…`) were left in plaintext in the chat → **rotate them**. In the repo there are only empty placeholders; `.gitignore` excludes secrets; locally it uses **user-secrets** (outside the repo).
- **Local runner (spec/plan/tasks 020)**: `scripts/run-local.ps1` (API+frontend with NVIDIA SuperVL via user-secrets; `-Heuristic` with no network) and `scripts/local-check.ps1` (console test: parse JD → session → real verdict). Verified 2026-11-16: `local-check` returned `source=llm`, a generated Kafka question, and `OverallScore=56.44` with a correct `fail` verdict for a generic answer.
- **502 Bad Gateway fix in the browser (2026-11-16)**: the Vite proxy (`frontend/vite.config.ts`) pointed by default at `localhost:5000` while the local API runs on `5210` → the browser's `/api/*` calls returned 502. Default changed to `http://127.0.0.1:5210` (overridable with `VITE_API_PROXY_TARGET`); `run-local.ps1` sets that env var and `e2e-smoke.ps1` now verifies the proxy with `GET :5174/api/health` (previously it only checked the HTML). Verified: `POST :5174/api/jd/parse` → `source=llm` in ~5.5s with DB+SuperVL.

---

## Dependency Architecture (verified)

```
Api  →  Agents  →  Core
Api  →  Infrastructure  →  Core
Agents  →  Core (nunca al revés)
```

- **Core**: Contracts, immutable records, pure scoring (Zero-Math), ActivitySource, ISemanticCache, IPiiSanitizer, VectorMath
- **Agents**: Orchestration logic, hybrid judges, Interviewer, Study Guide
- **Infrastructure**: EF Core, repositories, Redis (optional), LLM providers, PII Sanitizer, Semantic Cache, Polly, PII-free logging
- **Api**: ASP.NET Core 11, SignalR, controllers, DI, OpenTelemetry, PII-free logging

---

## Constitution Principles (verified)

| Principle | Status | Evidence |
|-----------|--------|-----------|
| Zero-Math | ✅ | Scoring in C# (ScoreCalculator, VerdictDeterminer, AnchoredRubricMath); VectorMath in C# |
| PII Sanitization | ✅ | RegexPiiSanitizer at all LLM and persistence points + PiiSanitizingLogger |
| No hardcoded secrets | ✅ | Empty placeholders in appsettings.json |
| Anti-repetition | ✅ | QuestionRegistry with normalized hash + cosine similarity > 0.92 (in memory) |
| In-memory fallback | ✅ | All repositories have an in-memory implementation |
| OpenAI-compatible (NVIDIA NIM / OpenAI / OpenCode) | ✅ | `OpenAiCompatibleLlmClient` + DI with aliases `Nvidia`/`OpenAI`/`OpenCode` + OpenCode endpoint at `opencode.ai/inference/openai/v1`; validated by `scripts/llm-check.ps1 -Provider OpenCode` |
| Pluggable LLM providers | ✅ | Azure OpenAI, Bedrock, Ollama, Heuristic via ILlmCompletionClient |
| Hybrid evaluators | ✅ | Deterministic pre-filter + LLM judge with anchored 1-5 rubric |
| Scoring 0.6/0.4 | ✅ | TechnicalDepth * 0.6 + ProductionReadiness * 0.4 |
| PII-free logging | ✅ | PiiSanitizingLoggerProvider wraps all log sinks |
| SQL Server Express compatible | ✅ | varbinary(max) for embeddings; in-memory semantic search |

---

## How to Read This Document

This file must be updated every time a specification, plan, or task is completed. It is the source of truth for knowing what is implemented, what is in progress, and what is still left to do.
