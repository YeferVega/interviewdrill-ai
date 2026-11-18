# Project Constitution — InterviewDrill AI

## Product Vision (North Star)

A platform that **simulates personalized technical interviews** starting from a **Job Description (JD)**:
1. The user uploads a JD (PDF/DOCX/TXT) → the system extracts role, seniority and skills.
2. An adaptive interview is conducted with questions generated from those skills.
3. Each answer receives **immediate structured feedback** (EvaluationPayload).
4. At the end, a **detailed study guide** of the topics to reinforce is generated.
5. If the user repeats the interview for the same JD, they **never receive questions already asked** (persistent registry).

## Core Principles

### 1. Code Quality
- Write clean, readable, and maintainable code
- Follow consistent naming conventions and coding standards
- Keep functions small and focused on a single responsibility
- Avoid code duplication (DRY principle)
- **Immutable data contracts** for all agent communication, validated with JSON Schema
- **Strongly-typed interfaces** (C# records/interfaces) for all agent payloads
- **Zero-Math principle**: all arithmetic (scores, weights, averages) is computed in C# code, NEVER delegated to LLMs

### 2. Testing Standards
- Tests for all new features and bug fixes (xUnit)
- Coverage above 80% for critical paths (scoring, verdicts, anti-repetition)
- Coverage measured with `--collect:"XPlat Code coverage"` and `backend/coverage.runsettings`, which **excludes generated code** (EF migrations, compiler-generated attributes) so the metric reflects hand-written logic. Repositories are unit-tested with `Microsoft.EntityFrameworkCore.InMemory` (test-only); SQL Server behavior is covered by the `-UseDatabase` E2E gate.
- Arrange-Act-Assert pattern, descriptive test names
- **Test all verdict scenarios** (PASS, NEEDS_PRACTICE, FAIL) and boundary conditions (59.99, 60, 79.99, 80)
- **Validate JSON Schema compliance** for all evaluation payloads
- **Golden dataset**: calibrated sample answers with expected verdicts to detect evaluator drift
- **E2E quality gate** (`scripts/e2e-smoke.ps1`): boots backend + frontend, parses a JD, runs a full adaptive interview, and asserts guide/history/PDF. Must pass in **both** modes: default (in-memory) and `-UseDatabase` (SQL Server Express `InterviewDrill_Testing`). The health endpoint's `persistence` field is asserted to match the chosen mode. `-UseDatabase` resolves the connection string with precedence: `INTERVIEWDRILL_CONNECTION_STRING` (for Azure/AWS or any other instance) first, then automatic fallback to local SQLEXPRESS.
- **Anti-repetition gate**: the E2E runs a **second** session with the same user+JD and asserts **zero** overlap with the first session's questions (proves question-history persistence across requests/sessions, in-memory and real DB).

### 3. User Experience Consistency
- Intuitive, consistent interfaces; WCAG 2.1 AA minimum
- **Immediate feedback** after each candidate response with structured evaluation
- **Scoring transparency**: show TechnicalDepth and ProductionReadiness breakdown
- **Adaptive difficulty**: next question complexity adjusts to candidate performance
- **Clear verdict communication** (PASS/NEEDS_PRACTICE/FAIL) with actionable advice
- **Detailed study guide** at the end of every interview, ordered by priority

### 4. Performance Requirements
- P95 latency < 2s for scoring synthesis; < 5s for LLM question generation
- **Semantic cache** (Redis) for repeated question/evaluation patterns to reduce LLM costs
- **Rate limiting handling** with exponential backoff for HTTP 429 (Polly)
- **Concurrent evaluation**: evaluators run in parallel (Task.WhenAll)

### 5. Security
- Validate and sanitize all user inputs
- **Mandatory PII sanitization** before LLM inference or vectorization (`RegexPiiSanitizer`)
- **No hardcoded secrets**: LLM/provider credentials (NVIDIA NIM `nvapi-…`, OpenAI `sk-…`, OpenCode `oc_sk_…`, Azure, Bedrock) come only from environment variables / `dotnet user-secrets` (`UserSecretsId` in csproj); `appsettings.json`/`.Development.json` keep empty `ApiKey: ""`. If a secret is ever pasted/shared in clear, **rotate it immediately**.
- **Reference base model**: `nvidia/nemotron-3.5-super-vl-120b-a12b` via `OpenAICompatible`; NIM IDs in lowercase with hyphens. The real E2E gate uses `-WithLlm` with `EnableThinking=false` to keep latency bounded.
- **Pluggable OpenAI-compatible provider**: `OpenAiCompatibleLlmClient` serves NVIDIA NIM, OpenAI and OpenCode through the same `chat/completions` body with `MaxTokens/TopP/EnableThinking` and fallback to `reasoning_content`. Foundry (`AzureOpenAILlmClient`) and Bedrock remain open. NVIDIA NIM model IDs use **lowercase with hyphens** (`nvidia/nemotron-3.5-lightning-30b-a3b`). Verified live: NVIDIA 200, OpenAI/OpenCode 429 (no balance/quota); exposed keys → rotate.
- **Data masking** for sensitive info in logs and evaluation payloads
- **Prompt-injection guardrails**: candidate answers are data, never instructions for judge agents
- **Audit logging** for all evaluation decisions and scoring changes

### 6. Documentation
- Keep documentation up-to-date with code changes
- Document architectural decisions and trade-offs (`.specify/`)
- Maintain IMPLEMENTATION_STATUS.md as the live source of truth

### 7. Collaboration
- Conventional commits; code review before merging
- Specification-Driven Development: spec → plan → tasks → implement

## Technology Stack (AUTHORITATIVE)

| Layer | Technology |
|---|---|
| **Backend** | .NET 11 / C# 15, ASP.NET Core, SignalR (real-time interview) |
| **Agent orchestration** | Microsoft Agent Framework / Semantic Kernel (Concurrent, Sequential, Handoff patterns) |
| **Persistence** | EF Core + SQL Server/Azure SQL; embeddings as native `vector(1536)` on Azure SQL / SQL Server 2025+, or `varbinary(max)` + in-memory cosine (`VectorMath.CosineSimilarity`) on SQL Server Express (no native VECTOR support). Redis optional (semantic cache, session state) |
| **Document parsing** | UglyToad.PdfPig + DocumentFormat.OpenXml + TXT reader (JD/resume → text); MarkItDown rejected due vulnerable transitive dependencies |
| **Resilience/Observability** | Polly, ActivitySource/OpenTelemetry |
| **Frontend** | React 19 + TypeScript + Vite + Zustand + SignalR client |
| **Tests** | xUnit (backend), Vitest (frontend) |

## Scalable Architecture (Dependency Direction)

```text
Api  →  Agents  →  Core
Api  →  Infrastructure  →  Core
Agents  →  Core (nunca al revés)
```

- **Core** contains only contracts, immutable records and pure scoring (Zero-Math). It does not reference any other backend project.
- **Agents** contains the agent orchestration logic. It depends **only** on Core (interfaces). It knows nothing about DB, cache, or LLM provider implementations.
- **Infrastructure** contains concrete implementations: EF Core, repositories, Redis, PDF/DOCX/TXT document extractors, LLM providers, semantic cache.
- **Api** ties everything together: ASP.NET Core 11, SignalR hubs, controllers. It orchestrates Agents + Infrastructure.

> ⚠️ The TypeScript/Node.js prototype in `src/`, `tests/`, `server/` is **legacy reference**. It contains scoring logic already tested (174 tests) that was ported to C# in `InterviewDrill.Core`. All new development happens in `backend/` (.NET) and `frontend/` (React). The legacy code will not be modified except to extract proven logic.

## LLM-as-Judge Standards (Evaluation Quality)

1. **Hybrid evaluation**: cheap deterministic checks (regex/keyword pre-filters) run FIRST; the LLM judge handles semantic judgment. Never ship regex-only scoring — it produces false positives (e.g., negations like "avoid being tied to a provider").
2. **Anchored rubrics**: judges score on small anchored scales (1–5 per criterion with explicit level definitions). The 0–100 scores are computed in code from rubric results (Zero-Math).
3. **Reasoning-first**: the judge's JSON schema places `reasoning` before any score field.
4. **Deterministic judging**: `temperature = 0` for all evaluator agents; structured output with strict JSON schema.
5. **One evaluator, one concern**: Architecture Evaluator and Security & Rigor Judge remain separate agents.
6. **Calibration**: new judge prompts are validated against the golden dataset before release.

## Multi-Agent Architecture

### Agent Responsibilities
- **JD Parser Agent**: extracts role, seniority, skills and responsibilities from the uploaded Job Description into a structured `JobProfile`.
- **Interviewer Agent**: generates questions from the `JobProfile` (never from a fixed static bank), adapts difficulty, never repeats questions across sessions.
- **Architecture Evaluator**: assesses TechnicalDepth (0–100) via hybrid rubric evaluation.
- **Security & Rigor Judge**: assesses ProductionReadiness (0–100), guardrails, PII, resilience.
- **Scoring Synthesizer**: consolidates evaluations, computes weighted score, generates feedback.
- **Study Guide Generator**: at interview end, aggregates all evaluations and produces a prioritized, detailed study guide.

### Data Flow Contract
1. Candidate uploads JD → JD Parser → `JobProfile`
2. Interviewer Agent → Candidate (question generated from `JobProfile`, deduplicated against history)
3. Candidate → Evaluators (technical response)
4. Architecture Evaluator + Security Judge → Scoring Synthesizer (parallel evaluation)
5. Scoring Synthesizer → Candidate (structured feedback with verdict)
6. Interview end → Study Guide Generator → Candidate (detailed study guide)

### Evaluation Criteria
**TechnicalDepth concepts**: Zero-Math, KernelFunction, HNSW, Kafka, ActivitySource, Polly, RAG, Semantic Cache
**ProductionReadiness requirements**: PII sanitization, relevance guardrails, rate limiting (429), latency P95/P99, human-in-the-loop

### Scoring Contract (immutable)
```
overallScore = (TechnicalDepth * 0.6) + (ProductionReadiness * 0.4)
PASS: >= 80 | NEEDS_PRACTICE: 60–79.99 | FAIL: < 60
```
The `EvaluationPayload` JSON Schema remains the immutable output contract (see AGENTS.md).

## Quality Gates (CI/CD)
- GitHub Actions workflow `.github/workflows/ci.yml` runs on `push`, `pull_request` and `workflow_dispatch`. Jobs:
  - **backend**: `dotnet build -c Release` + `dotnet test … --collect:"XPlat code coverage" --settings backend/coverage.runsettings`.
  - **frontend**: `npm run lint`, `npm run test` (Vitest), `npm run build` (tsc + vite).
  - **e2e** (windows-latest): `scripts/e2e-smoke.ps1` in in-memory mode (anti-repetition).
- The `-UseDatabase` variant requires SQL Server; it runs locally or on self-hosted runners (not in the hosted pipeline). See `DEPLOYMENT.md` for Azure SQL / AWS RDS.
- The pipeline mirrors the Definition of Done: build with 0 errors, green tests (Core/Agents/Infrastructure/Api), E2E approved, no anti-repetition regression.

## Definition of Done
- Code implemented and tested; documentation updated; code review completed
- JSON Schema validation passes for all agent payloads
- Scoring formula and verdict logic verified with boundary tests
- LLM-judge prompts validated against golden dataset (when applicable)
- No question repetition verified across sessions for the same user+JD (automated by `scripts/e2e-smoke.ps1`, in-memory and `-UseDatabase` modes)
- When persistence is exercised, `dotnet ef database update` has applied cleanly against the target SQL Server and the `-UseDatabase` E2E gate passes
