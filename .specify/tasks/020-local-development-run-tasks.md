# Task List: 020-local-development-run

## Sprint/Iteration: Phase 8 — Local Runner and Test with NVIDIA SuperVL

### Tasks

#### Task 1: Create `scripts/run-local.ps1` (API+frontend)
- **Description**: Starts the API (Development, OpenAI-compatible NVIDIA via user-secrets) on `http://127.0.0.1:5210` and the Vite frontend on `5174`, waits for health, prints the URLs, and stops both with Ctrl+C. Includes `-Heuristic` for no-network mode.
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: Spec 012 (OpenAI-compatible NVIDIA)
- **Status**: [x] Complete — tested; brings up both servers and cleans up in `finally`.

#### Task 2: Create `scripts/local-check.ps1` (end-to-end console test)
- **Description**: Temporary API + JD parse + start session + strong answer; prints `source`, the generated question, the verdict, OverallScore/TechnicalDepth/ProductionReadiness, covered/missing items, and the next question. Uses `EnableThinking=false, MaxTokens=4096`.
- **Priority**: High
- **Estimated Time**: 1.5 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — executed: `source=llm`, correct skills, Kafka question generated, the judges returned verdict `fail` with scores (56.44 / 41.25 / 79.23) and a follow-up; API stopped.

#### Task 3: Document the local path
- **Description**: Create spec/plan/tasks 020 (SDD parity) and a section in `DEPLOYMENT.md` with `dotnet user-secrets set ...` + `run-local.ps1` / `local-check.ps1`.
- **Priority**: Medium
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 1-2
- **Status**: [x] Complete — spec/plan/tasks 020 created; section in `DEPLOYMENT.md` (Local runner and test with NVIDIA SuperVL).

#### Task 4: Verify the absence of secrets
- **Description**: Confirm that no NVIDIA/OpenAI/OpenCode key remains in the repo (placeholders + user-secrets only).
- **Priority**: High
- **Estimated Time**: 0.25 hours
- **Dependencies**: Tasks 1-2
- **Status**: [x] Complete — `dotnet user-secrets list` shows `nvapi-***`; repo scan with no real keys (only `nvapi-fake` in tests).

### Definition of Done
- [x] Local API+frontend runner with NVIDIA SuperVL by default (user-secrets) and optional `-Heuristic`
- [x] End-to-end console test shows evidence of NVIDIA connectivity (`source=llm`) and the judge's verdict
- [x] No secrets in the repo; key in user-secrets
- [x] Spec/plan/tasks 020 created; `DEPLOYMENT.md` with the local path
- [x] Does not affect CI gates (local ports, cleans up processes)
