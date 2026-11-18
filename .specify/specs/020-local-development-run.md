# Specification 020: Local Development Run (NVIDIA SuperVL)

## Overview
Enable a local test to run and review the complete system with the provider that **does connect** (NVIDIA SuperVL via user-secrets), without touching the browser, plus a runner that brings up API+frontend for manual review.

## Stack
- **.NET 11 / ASP.NET Core** + **React 19 / Vite**
- **User-secrets** for the API key (`LLM:OpenAICompatible:ApiKey`, not versioned)
- **PowerShell** for the startup/test scripts (`scripts/run-local.ps1`, `scripts/local-check.ps1`)
- **NVIDIA NIM** OpenAI-compatible: `https://integrate.api.nvidia.com/v1/chat/completions`, model `nvidia/nemotron-3.5-super-vl-120b-a12b`

## User Stories
- **As a** developer, **I want** a command that brings up API+frontend with the real model, **so that** I review the complete flow in the browser.
- **As a** developer, **I want** a command that runs an end-to-end test and shows me the verdict/scores without opening the browser, **so that** I confirm NVIDIA connects and the judges respond.

## Functional Requirements
### FR1: Local runner (`scripts/run-local.ps1`)
- [x] Starts the API on `http://127.0.0.1:5210` (Development, OpenAI-compatible provider via user-secrets) and the frontend on `http://127.0.0.1:5174`.
- [x] Waits for `/api/health` and the frontend home before printing URLs.
- [x] `-Heuristic` forces no-network mode; by default it uses NVIDIA SuperVL.
- [x] Ctrl+C stops both processes (cleanup in `finally`).

### FR2: Local end-to-end test (`scripts/local-check.ps1`)
- [x] Starts a temporary API, parses a JD → shows `role/seniority/skills/profileSource`.
- [x] Starts a session → shows the first generated question (with `skillTarget`).
- [x] Submits a strong technical answer → shows verdict, `OverallScore`, `TechnicalDepth`, `ProductionReadiness`, covered/missing, advice, and the next question.
- [x] Uses `EnableThinking=false, MaxTokens=4096` for bounded runtimes.
- [x] Stops the API when finished (`finally`).

## Non-Functional Requirements
- [x] No secrets in the repo (placeholders only; API key in user-secrets).
- [x] No impact on the CI gate (uses local ports and cleans up processes).
- [x] Deterministic in `-Heuristic` mode (no network).

## Acceptance Criteria (verified 2026-11-16)
- [x] `local-check.ps1`: JD parse `source=llm`, skills=[RAG,Kafka,observability,Polly,PII sanitization]; question with skill=Kafka; answer → verdict `fail` with `TechnicalDepth=41.25`, `ProductionReadiness=79.23`, `OverallScore=56.44` + follow-up. The "fail" verdict is correct because the test answer was generic (it did not cover Kafka), which demonstrates that the judges are truly evaluating with the real model.
- [x] `run-local.ps1`: starts API+frontend and stops with Ctrl+C.

## Out of Scope
- Real database in the runner (uses in-memory, unless `INTERVIEWDRILL_CONNECTION_STRING` is exported).
- Cloud deployment (spec/plan/tasks 019).

## Dependencies
- **Spec 012** (OpenAI-compatible, NVIDIA SuperVL), **Spec 005/009/003/004/010** (pipeline), **Spec 017** (gates).

## Risks
| Risk | Mitigation |
|------|------------|
| The NVIDIA key is not in user-secrets → `source=heuristic` | `dotnet user-secrets set "LLM:OpenAICompatible:ApiKey" "nvapi-..."` |
| NVIDIA SuperVL thinking → timeouts | `-WithLlm`/`local-check` use `EnableThinking=false, MaxTokens=4096` |
| Key exposed in chat/repo | Rotate; env/user-secrets only (Constitution) |
