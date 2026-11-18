# InterviewDrill AI

Technical interview simulator with AI agents, personalized from the **Job Description** of a real job opening: immediate feedback per answer, a prioritized study guide at the end, and always-new questions when you practice again (anti-repetition across sessions). The interface and the interview itself are **fully bilingual (Spanish / English)**.

**Stack:** .NET 11 (C# 15) + ASP.NET Core + SignalR · React 19 + TypeScript + Vite + Zustand · EF Core + SQL Server (Express / Azure SQL / AWS RDS) · OpenAI-compatible LLM providers (NVIDIA NIM default), Azure OpenAI/Foundry, Bedrock (skeleton), Ollama.

## Features

- **6-agent pipeline** — JD Parser → Interviewer (adaptive, never repeats) → Architecture Evaluator + Security & Rigor Judge (parallel) → Scoring Synthesizer → Study Guide Generator (see `AGENTS.md`).
- **Hybrid judging (Zero-Math)** — deterministic pre-filters + LLM judges with anchored 1–5 rubrics; all 0–100 scores computed in C#. `overall = TechnicalDepth×0.6 + ProductionReadiness×0.4` → PASS / NEEDS_PRACTICE / FAIL.
- **Anti-repetition** — exact + semantic dedup (cosine similarity) persisted per user + JD; verified by the E2E gate (0 overlap between sessions).
- **Bilingual (spec 021)** — UI toggle ES/EN in the header; interview language (questions, judge feedback, study guide) chosen at JD upload or profile review; travels as `language: "es" | "en"` in the API and persists in the session.
- **Study guide** — prioritized by failure frequency × JD skill weight, with resources, exercises and time estimates; exportable to Markdown/PDF.
- **Quality gates** — 265/265 backend tests + 19/19 frontend tests, coverage gate, E2E smoke (default / `-UseDatabase` / `-WithLlm`), CI with **secret scan**.

## Project Structure

```
OpenCode_SpecKit/
├── .specify/                    # SDD Knowledge Base (specs, plans, tasks, logs — 000…021)
│   └── memory/constitution.md   # Project principles (AUTHORITATIVE)
├── backend/                     # .NET 11 solution (InterviewDrill.slnx)
│   ├── src/InterviewDrill.Core/          # Contracts, domain, scoring (Zero-Math)
│   ├── src/InterviewDrill.Agents/        # Agent loop, difficulty controller, judges, synthesizer
│   ├── src/InterviewDrill.Infrastructure/# EF Core SQL Server, LLM providers, document extractors, PDF
│   ├── src/InterviewDrill.Api/           # ASP.NET Core REST + SignalR hub
│   └── tests/                            # xUnit: Core 35 · Agents 94 · Infrastructure 121 · Api 15
├── frontend/                    # React 19 + Vite + Zustand + SignalR (ES/EN i18n)
├── scripts/                     # run-local · local-check · e2e-smoke · llm-check
├── src/, tests/, server/        # ⚠️ LEGACY TypeScript prototype (reference only — do not reuse)
├── IMPLEMENTATION_STATUS.md     # Live source of truth
├── DEPLOYMENT.md                # Local run + Azure SQL / AWS RDS guides
├── DATABASE_SETUP.md            # SQL Server Express step-by-step
└── AGENTS.md                    # Multi-agent architecture contract
```

## Quick Start (local, with NVIDIA SuperVL)

> **Prerequisites:** .NET 11 SDK, Node 22+, and an NVIDIA API key (`nvapi-…`). No key? Everything still runs in deterministic **Heuristic** mode with zero network calls.

```powershell
# 1) Store the API key OUTSIDE the repo (user-secrets)
dotnet user-secrets set "LLM:OpenAICompatible:ApiKey" "nvapi-YOUR_KEY" --project backend/src/InterviewDrill.Api

# 2) Build + run the full stack (API http://127.0.0.1:5210 · frontend http://127.0.0.1:5174)
dotnet build backend/InterviewDrill.slnx --nologo
powershell -ExecutionPolicy Bypass -File scripts/run-local.ps1
# offline deterministic mode: ./scripts/run-local.ps1 -Heuristic
```

```powershell
# Console end-to-end proof (JD parse → question → real judge verdict)
powershell -ExecutionPolicy Bypass -File scripts/local-check.ps1

# Quality gates
powershell -ExecutionPolicy Bypass -File scripts/e2e-smoke.ps1                    # in-memory + heuristic
powershell -ExecutionPolicy Bypass -File scripts/e2e-smoke.ps1 -UseDatabase      # SQL Server Express
powershell -ExecutionPolicy Bypass -File scripts/e2e-smoke.ps1 -WithLlm          # real NVIDIA model

# Test suites
dotnet test backend/InterviewDrill.slnx --nologo      # 265 tests
npm test --prefix frontend                              # 19 tests
```

In development, Vite proxies `/api` and `/hubs` to `http://127.0.0.1:5210`. Override with `VITE_API_PROXY_TARGET` if the API uses another port. The E2E gate verifies this proxy end-to-end.

## LLM Providers

`LLM:Provider` (default `OpenAICompatible`) selects the client:

| Provider | Config key | Notes |
|---|---|---|
| **OpenAI-compatible** | `LLM:OpenAICompatible` | NVIDIA NIM (default model `nvidia/nemotron-3.5-super-vl-120b-a12b`), OpenAI, OpenCode gateway. Aliases: `Nvidia`, `OpenAI`, `OpenCode`. `MaxTokens`/`TopP`/`EnableThinking` supported; `reasoning_content` fallback for thinking models |
| Azure OpenAI / Foundry | `LLM:AzureOpenAI` | Endpoint + deployment |
| AWS Bedrock | `LLM:Bedrock` | SigV4 skeleton (pending real signing) |
| Ollama | `LLM:Ollama` | Local free models |
| Heuristic | `LLM:Provider=Heuristic` | Deterministic fallback, no network |

Model IDs for NVIDIA are **lowercase with hyphens**. If the provider fails (timeout, 5xx, bad JSON), calls map to `LlmUnavailableException` and agents degrade gracefully to heuristic mode — never a 500 to the user.

## Languages (ES / EN)

- **Interface:** header toggle, persisted per browser (`<html lang>` synced). Add a key to both dictionaries in `frontend/src/i18n/strings.ts` — parity is enforced by tests.
- **Interview:** radio *Español / English* at JD upload and profile review; sent as `language` on session start and each answer; stored in the session state (no DB migration). Questions, judge prose, advice and study guide are generated in the chosen language, with real LLMs or heuristics.
- **Contract stability:** scores, verdicts and canonical technical keywords are language-neutral; the immutable `EvaluationPayload` JSON schema never changes.

## API Endpoints

```http
GET  /api/health
POST /api/jd/parse                                   # text, or multipart file (PDF/DOCX/TXT)
POST /api/interview/sessions                         # { userId, profile, language? }
POST /api/interview/sessions/{id}/answers            # { userId, answerText, language? }
GET  /api/interview/sessions?userId={userId}         # history
GET  /api/interview/sessions/{id}/study-guide        # markdown
GET  /api/interview/sessions/{id}/study-guide.pdf    # PDF export
WS   /hubs/interview                                 # live questions/feedback (REST fallback automatic)
```

Persistence is automatic: sessions, answers and guides are stored in SQL Server when `INTERVIEWDRILL_CONNECTION_STRING` is set (see `DATABASE_SETUP.md`); otherwise in-memory (demo/CI).

## Security & Secrets Policy

- **Never put API keys in `appsettings*.json`** — the repo keeps empty placeholders only. Local: `dotnet user-secrets` (stored at `%APPDATA%\Microsoft\UserSecrets\…`, outside the repo). Cloud/CI: environment variables (`NVIDIA_API_KEY`, `LLM__OpenAICompatible__ApiKey`, …).
- `.gitignore` blocks env files, secrets files, private keys/certificates, cloud credentials, local database dumps and IDE junk.
- CI runs a **secret scan** on every push/PR and fails the build on key patterns (`nvapi-…`, `sk-…`, `ghp_…`, `AKIA…`, PEM private keys).
- If a key is ever pasted in chat/tickets, **rotate it immediately**.

## SDD Workflow

1. **Specify** what (`.specify/specs/`) → 2. **Plan** how (`.specify/plans/`) → 3. **Tasks** breakdown (`.specify/tasks/`) → 4. **Implement** + tests (logs in `.specify/implementations/`) → 5. **Constitution** guardrails (`.specify/memory/constitution.md`).

See `AGENTS.md` for the full multi-agent contract and `IMPLEMENTATION_STATUS.md` for the live state of every unit.
