# Technical Plan 000: Foundation & Scaffold (ALIGNED)

> ⚠️ **Revision 2026-11-12:** Stack corrected to .NET 11 + React. The TypeScript code in `src/`, `tests/`, `server/` remains as **legacy reference**. All new development happens in `backend/` (.NET) and `frontend/` (React).

## Current status (validated)

**Phase 0: Scaffold — COMPLETED** ✅
- .NET 11 solution (`InterviewDrill.slnx`) with 4 src projects + 2 test projects.
- `InterviewDrill.Core`: contracts for 6 agents, `JobProfile`, `EvaluationPayload`, `ScoreCalculator` (0.6/0.4), `VerdictDeterminer`.
- `InterviewDrill.Core.Tests`: 26 tests passing (scoring + verdicts with boundaries 59.99/60/79.99/80, weight validation).
- `InterviewDrill.Agents.Tests`: 2 contract tests.
- `frontend/`: React 19 + Vite + TS + Zustand + SignalR client, build OK.
- `dotnet build` and `dotnet test` green.

**Current status:** basic persistence, PII, JD parser, JD endpoint, Interviewer loop with closure at 8 questions, SignalR backend/frontend, hybrid evaluators, Scoring Synthesizer, persistent checkpoints/answers, Study Guide Generator, history API/UI and offline E2E smoke approved. Main pending items: real LLM provider, migration applied on Azure SQL/AWS SQL, real embeddings, PDF server-side, formal WCAG audit, Redis and .NET RC SDK security.

---

## Solution and scalable architecture

```
OpenCode_SpecKit/
├── backend/
│   ├── InterviewDrill.slnx
│   ├── src/
│   │   ├── InterviewDrill.Core/           # ⚠️ Solo contratos, registros y scoring puro (Zero-Math). Sin dependencia de Infra ni Api.
│   │   ├── InterviewDrill.Agents/         # Lógica de orquestación de agentes. Depende SOLO de Core (interfaces). No conoce implementaciones de BD ni LLM.
│   │   ├── InterviewDrill.Infrastructure/ # Implementaciones concretas: EF Core, repositorios, Redis, extractores PDF/DOCX/TXT, proveedores LLM, caché.
│   │   └── InterviewDrill.Api/            # ASP.NET Core 11, SignalR hubs, controllers. Orquesta Agents + Infrastructure.
│   └── tests/
│       ├── InterviewDrill.Core.Tests/     # xUnit: scoring, veredictos, contratos.
│       └── InterviewDrill.Agents.Tests/   # xUnit: agentes con golden dataset (mocks de infraestructura).
├── frontend/                              # React 19 + Vite + TS + Zustand + SignalR client
├── src/, tests/, server/                  # LEGACY TypeScript prototype (reference only)
└── .specify/                              # SDD docs (specs, plans, constitution)
```

### Applied scalable architecture principles

| Principle | Application |
|---|---|
| **Dependency Inversion** | `Agents` depends only on interfaces in `Core` (e.g. `IQuestionRegistry`, `IArchitectureEvaluator`). The concrete implementations live in `Infrastructure`. `Api` wires everything together. |
| **Core / Agents / Infrastructure / Api separation** | `Core` references no other backend project. `Agents` does not reference `Infrastructure`. `Infrastructure` does not reference `Agents`. |
| **Zero-Math** | All score calculation, study priority and verdicts are done in C# in `Core`. LLMs only emit semantic judgments with anchored 1–5 rubrics, never arithmetic. |
| **Immutable contracts** | `JobProfile`, `EvaluationPayload`, `InterviewQuestion` are immutable C# `record`s, validated with JSON Schema at the API boundaries. |
| **Hybrid evaluation** | Deterministic pre-filters (regex/keywords) in C# run first; the LLM judge handles the semantic judgment with `temperature = 0` and structured output. |

---

## Phase Plan (detailed)

### Phase 0: Scaffold — COMPLETED ✅
- [x] .NET 11 solution with 4 src projects + 2 test projects.
- [x] Domain contracts in `Core` (agents, scoring, job profile).
- [x] `ScoreCalculator` and `VerdictDeterminer` implemented and tested.
- [x] Frontend React 19 + Vite + TS + Zustand + SignalR (scaffold).
- [ ] **Pending review:** formal documentation of the scoring synthesizer contract (see notes below).

### Phase 1: Persistence & JD Parser ← IN PROGRESS
- [x] EF Core + entities (`Users`, `JobDescriptions`, `InterviewSessions`, `Questions`, `Answers`, `StudyGuides`) + columns for embeddings (2026-11-13).
- [~] Initial EF Core migration with SQL Server/Azure SQL provider and native vector column/index. Migration generated; applying it to the real DB pending.
- [x] `QuestionRegistry` implemented in `Infrastructure` with exact dedup by normalized hash.
- [~] Semantic dedup > 0.92 implemented with `IEmbeddingGenerator`, `VectorMath` and `EF.Functions.VectorDistance`; real embeddings provider and end-to-end test pending.
- [x] Per-question session checkpoint implemented with `SessionCheckpoint`, `ISessionRepository`, `SqlSessionRepository` and `SessionCheckpointService` (spec 011 Task 5).
- [x] Session history with verdicts implemented with `InterviewSessionSummary`, `ISessionHistoryRepository`, `SqlSessionHistoryRepository` and `SessionHistoryService` (spec 011 Task 6).
- [x] PII Sanitizer core implemented: `RegexPiiSanitizer` with masking, detect-only and custom patterns (spec 006 Tasks 1-3).
- [~] PII Sanitizer wired before the LLM in JD Parser and before embeddings/persistence in QuestionRegistry; candidate answers pending (spec 006 Task 4).
- [x] PDF/DOCX/TXT text extractor implemented with `IJdDocumentTextExtractor`, `JdDocumentTextExtractor`, PdfPig and DocumentFormat.OpenXml. MarkItDown was discarded due to vulnerabilities in its dependencies.
- [x] `IJdParserAgent` LLM implemented with `ILlmCompletionClient`, structured output and temperature 0 (`LlmJdParserAgent`).
- [x] `IJdParserAgent` heuristic fallback without LLM implemented with `HeuristicJdParserAgent` and `FallbackJdParserAgent`.
- [ ] Real LLM provider for `ILlmCompletionClient` (Azure OpenAI/OpenAI/Bedrock/Ollama).
- [ ] Semantic Cache with Redis + HNSW (spec 007).
- [x] API endpoint to upload a JD and get `JobProfile` implemented with `POST /api/jd/parse`.

### Phase 2: Interview Loop
- [x] `IInterviewerAgent` implemented (`InterviewerAgent`) with SkillPlanner and `DifficultyController` in C#.
- [x] Anti-repetition integrated into the flow: `IQuestionRegistry` + regeneration max. 3 attempts.
- [x] SignalR hub backend implemented (`InterviewHub`) with per-session groups and REST interview endpoints.
- [x] React components: JD upload, profile review, interview chat, feedback, progress, history and SignalR client.
- [x] Study guide component at completion: Markdown download, browser print/export to PDF and per-session persistence.
- [x] E2E smoke backend + frontend dev server; JD parse, 8-question interview, final answer, guide and history verified.
- [ ] Formal WCAG 2.1 AA audit and additional chat/store tests.
- [x] Session checkpoint with `EfInterviewerSessionStore` and answers/guides repositories; offline mode uses an in-memory store.
- [x] Persistence of sanitized answers when SQL Server/Azure SQL is configured.

### Phase 3 completed
- [x] `IArchitectureEvaluator` hybrid: deterministic regex pre-filters + LLM judge with anchored 1–5 rubric.
- [x] `ISecurityRigorJudge` hybrid: PII guardrails, relevance, rate limiting, resilience.
- [x] `IScoringSynthesizer` integrated into the loop: `overall = TD*0.6 + PR*0.4`, verdict and immediate feedback.
- [~] JSON Schema validation of `EvaluationPayload` at the API boundaries — output is already serialized/validated in C#, formal schema pending.
- [x] Configurable LLM provider via `ILlmCompletionClient`; in offline demo it uses the heuristic judge fallback.

### Phase 4: Study Guide & History
- [x] `IStudyGuideGenerator` implemented: priority in C# (`failureFrequency × jdWeight × gap`), max 7 topics.
- [~] Markdown + PDF export of the study guide: Markdown download and print-to-PDF in the UI; server-side PDF with a formal layout pending.
- [~] Session history with verdicts and associated guides: `SessionHistoryService`, history/guide GET endpoints and History UI implemented; application to the real DB and WCAG audit pending.
- [ ] Retake flow: same JD, 100% new questions (dedup active).

---

## Validation notes: is what was implemented what was planned?

### ✅ Aligned with the user's vision
- .NET 11 + React stack as requested.
- 6 agents with clear roles, including the **JD Parser** (to extract skills from the JD file) and the **Study Guide Generator** (for the detailed post-interview guide).
- Anti-repetition across sessions of the same user+JD (explicit user requirement).
- Weighted scoring `0.6/0.4` and PASS/NEEDS_PRACTICE/FAIL verdicts.

### ⚠️ Pending clarification
1. **Database:** ✅ Resolved by user decision: run on AWS or Azure. SQL Server/Azure SQL was selected because EF Core 11 has native `vector(1536)` support and a cosine vector index; AWS can use RDS SQL Server if required.
2. **LLM provider:** still to choose between Azure OpenAI, direct OpenAI, Bedrock or another. This affects `Infrastructure` and API keys configuration.
3. **Authentication MVP:** spec 011 assumes an anonymous `userId` persistent per browser token; confirm whether MVP stays without login.
4. **Applying the initial migration:** a real Azure SQL / AWS SQL Server connection string is still needed to run `dotnet ef database update`.

### 📄 Documentation of what is already implemented (in code)
- `backend/src/InterviewDrill.Core/Scoring/ScoreCalculator.cs`: `overallScore = TechnicalDepth*0.6 + ProductionReadiness*0.4`, range 0-100, rounding to 2 decimals.
- `backend/src/InterviewDrill.Core/Scoring/VerdictDeterminer.cs`: PASS ≥80, NEEDS_PRACTICE 60-79.99, FAIL <60.
- `backend/src/InterviewDrill.Core/Contracts/IAgents.cs`: interfaces for the 6 agents + `IQuestionRegistry`.
- `backend/tests/InterviewDrill.Core.Tests/Scoring/ScoreCalculatorTests.cs`: 12 tests (formulas, boundaries, invalid weights).
- `backend/tests/InterviewDrill.Core.Tests/Scoring/VerdictDeterminerTests.cs`: 8 tests (exact boundaries, invalid thresholds).
- `backend/tests/InterviewDrill.Agents.Tests/AgentContractTests.cs`: 2 tests (agent contracts defined).
