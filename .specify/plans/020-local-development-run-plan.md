# Technical Plan 020: Local Development Run (NVIDIA SuperVL)

## Objective
Provide two local routes with no external dependencies other than the NVIDIA key in user-secrets:
1. `scripts/run-local.ps1`: starts API + frontend for browser review.
2. `scripts/local-check.ps1`: console end-to-end test with readable output (verdict + scores), using the real model.

## Approach
- **Default mode = NVIDIA** (`LLM__Provider=OpenAICompatible` + model `nvidia/nemotron-3.5-super-vl-120b-a12b`), key from user-secrets (no hardcoding).
- **Bound the latency in the check**: `EnableThinking=false, MaxTokens=4096` (SuperVL with thinking on is slow and can hit the 30s Polly timeout).
- **No shared state**: `local-check` starts the API and shuts it down when finished; `run-local` leaves processes alive and kills them with Ctrl+C (`finally`).
- **Deterministic mode escape hatch**: `-Heuristic` in `run-local` (no network) for tests without spending tokens.

## Decisions
- Do not use a DB in the local runner by default (in-memory); optionally by exporting `INTERVIEWDRILL_CONNECTION_STRING`.
- Reuse the `Wait-For-Http` from the E2E (no duplicated logic: same pattern).
- The scripts are documented as part of gates/dev in `DEPLOYMENT.md` and spec 020.

## Steps for the user (local review with NVIDIA)
```powershell
# 1) asegurar llave en user-secrets (fuera del repo)
dotnet user-secrets set "LLM:OpenAICompatible:ApiKey" "nvapi-..." --project backend/src/InterviewDrill.Api

# 2) prueba end-to-end por consola (verifica que NVIDIA conecta)
dotnet build backend/InterviewDrill.slnx --nologo
powershell -ExecutionPolicy Bypass -File scripts/local-check.ps1

# 3) revisión en navegador (API+frontend con el modelo real)
powershell -ExecutionPolicy Bypass -File scripts/run-local.ps1
# abrir http://127.0.0.1:5174 ; Ctrl+C para detener
```

## Risks
| Risk | Mitigation |
|--------|------------|
| Missing key ⇒ falls back to heuristic | The check shows `source=llm`; if it shows `heuristic`, set the user-secrets |
| NVIDIA tokens per review | Use `-Heuristic` for quick tests; `-WithLlm`/check only when you want to validate the real model |
| Port 5210/5174 in use | Change it with `-ApiUrl` / `npm run dev -- --port` |
