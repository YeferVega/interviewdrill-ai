# Deployment — InterviewDrill AI

Guide to deploy the .NET 11 API + React 19 frontend and connect the database on **Azure SQL Database** or **AWS RDS for SQL Server**. Replace `TU_*` with your values.

---

## 1. Configuration concepts

- **Runtime DB choice:** the API uses SQL Server **only** if the `INTERVIEWDRILL_CONNECTION_STRING` environment variable is set; otherwise it uses *in-memory* (demo/CI). See `IMPLEMENTATION_STATUS.md` and spec 011.
- **LLM providers:** the `LLM` section of `appsettings.json` (or env `LLM__Provider`, `LLM__OpenAICompatible__ApiKey`, etc.). By default `OpenAICompatible` with the NVIDIA SuperVL base model. See spec 012.
- **No secrets in the repo:** `appsettings.json`/`appsettings.Development.json` ship with `ApiKey: ""`. Locally, use **user-secrets** (outside the repo); in production, **environment variables** or your platform's key vault.
- **Default base model:** `nvidia/nemotron-3.5-super-vl-120b-a12b` (`MaxTokens=16384`, `TopP=0.95`, `EnableThinking=true`). For the real E2E gate, `thinking=OFF, max_tokens=4096` is forced via env.

### Store the NVIDIA API key in user-secrets (local)
```powershell
dotnet user-secrets set "LLM:OpenAICompatible:ApiKey" "nvapi-..." --project backend/src/InterviewDrill.Api
# o por variable de entorno:
$env:NVIDIA_API_KEY = "nvapi-..."
```
`Program.cs` resolves the key via config/user-secrets or via the `NVIDIA_API_KEY`/`OPENAI_API_KEY`/`OPENCODE_API_KEY` env variables if the field is empty. The API **never** hardcodes secrets.

### Key variables
| Variable | Example | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `INTERVIEWDRILL_CONNECTION_STRING` | *(see below per cloud)* | Enables SQL Server |
| `LLM__Provider` | `OpenAICompatible` (NVIDIA/OpenAI/OpenCode) / `AzureOpenAI` / `Bedrock` / `Ollama` / `Heuristic` | LLM provider |
| `LLM__OpenAiCompatible__BaseUrl` | `https://integrate.api.nvidia.com/v1` (NVIDIA) / `https://api.openai.com/v1` (OpenAI) / `https://opencode.ai/inference/openai/v1` (OpenCode) | OpenAI-compatible endpoint |
| `LLM__OpenAiCompatible__ApiKey` | `nvapi-…` (NVIDIA) / `sk-…` (OpenAI) / `oc_sk_…` (OpenCode) | **Env only; rotate any exposed key** |
| `LLM__OpenAiCompatible__Model` | `nvidia/nemotron-3.5-lightning-30b-a3b` (NIM; **lowercase, hyphenated** IDs) / `gpt-4o-mini` (OpenAI) / `glm-5.3-flash-free` (OpenCode) | Model |
| `LLM__OpenAiCompatible__MaxTokens` / `TopP` / `EnableThinking` | `16384` / `0.95` / `true` (NVIDIA thinking example); defaults 4096/null/null | Thinking body (chat_template_kwargs + top_p) |
| Env aliases | `OPENCODE_API_KEY` / `NVIDIA_API_KEY` / `OPENAI_API_KEY` | Read by `scripts/llm-check.ps1` |
| `LLM__AzureOpenAI__Endpoint` / `ApiKey` / `DeploymentName` | | Only if you use Azure OpenAI |
| `LLM__Bedrock__Region` / `AccessKey` / `SecretKey` / `ModelId` | | Only if you use Bedrock (requires SigV4 signing, see spec 012) |
| `LLM__Ollama__BaseUrl` / `Model` | `http://localhost:11434` / `llama3` | Only if you use Ollama |

> On Linux/PaaS, the `LLM__AzureOpenAI__Endpoint` syntax (double underscore) is equivalent to `LLM:AzureOpenAI:Endpoint`.

### LLM connectivity validation (local)
Before running the app, verify that the provider and key respond:
```powershell
# OpenCode (plan Go requiere suscripción para uso sostenido vía API externa)
$env:OPENCODE_API_KEY="oc_sk_...";  powershell -ExecutionPolicy Bypass -File scripts/llm-check.ps1 -Provider OpenCode
# NVIDIA NIM (requiere nvapi-... con saldo)
$env:NVIDIA_API_KEY="nvapi-...";    powershell -ExecutionPolicy Bypass -File scripts/llm-check.ps1 -Provider Nvidia
# OpenAI (requiere sk-... con saldo)
$env:OPENAI_API_KEY="sk-...";       powershell -ExecutionPolicy Bypass -File scripts/llm-check.ps1 -Provider OpenAI
```
> ⚠️ Validated with the user's key: **OpenCode authentication passes**, but the OpenCode free-tier returned `429 FreeUsageLimitError` on all `*-free` models. Sustained external use requires an **OpenCode Go subscription** (`https://opencode.ai/go`).
Then, to validate the app end-to-end with a real LLM (instead of Heuristic):
```powershell
# NVIDIA NIM (IDs de modelo en minúsculas con guiones; modelos verificados: super-vl / lightning)
$env:LLM__Provider = "OpenAICompatible"
$env:LLM__OpenAICompatible__BaseUrl = "https://integrate.api.nvidia.com/v1"
# La llave puede ir en user-secrets (recomendado en local) o por env:
$env:LLM__OpenAICompatible__ApiKey  = "nvapi-..."   # rota cualquier llave expuesta
$env:LLM__OpenAICompatible__Model   = "nvidia/nemotron-3.5-super-vl-120b-a12b"
$env:LLM__OpenAICompatible__MaxTokens = "4096"
$env:LLM__OpenAICompatible__EnableThinking = "false"   # thinking ON es demasiado lento para el gate
powershell -ExecutionPolicy Bypass -File scripts/e2e-smoke.ps1 -WithLlm
```

Real evidence (2026-11-16):
- NVIDIA `nvapi-…` + `nvidia/nemotron-3.5-super-vl-120b-a12b` → **HTTP 200** via `/api/jd/parse` with `profileSource=llm` (~7-8s). PascalCase/end-of-life IDs return 404/410.
- Full `-WithLlm` E2E (SuperVL, thinking OFF, 4096) green including anti-repetition.
- OpenAI `sk-proj-…` → **429 insufficient_quota** (valid key, no balance; OpenAI format, not NVIDIA).
- OpenCode `oc_sk_…` → **429 FreeUsageLimitError** (valid key, no Go / daily quota exhausted).
> ⚠️ Keys go **always** via environment variable or **user-secrets** (recommended locally, nothing stays in the repo). Never hardcode (Constitution). If a key was shared in plaintext, **rotate it**.

### Local runner and test (NVIDIA SuperVL) — spec/plan/tasks 020
Bring everything up locally with the provider that **does connect** (NVIDIA), without hardcoding secrets:

```powershell
# 1) Asegurar la llave NVIDIA en user-secrets (fuera del repo)
dotnet user-secrets set "LLM:OpenAICompatible:ApiKey" "nvapi-..." --project backend/src/InterviewDrill.Api

# 2) Prueba end-to-end por consola: parse JD + sesión + veredicto del juez real
dotnet build backend/InterviewDrill.slnx --nologo
powershell -ExecutionPolicy Bypass -File scripts/local-check.ps1

# 3) Revisar en el navegador (API + frontend con el modelo real)
powershell -ExecutionPolicy Bypass -File scripts/run-local.ps1
# abre http://127.0.0.1:5174 ; Ctrl+C para detener
# modo determinista sin red: ./scripts/run-local.ps1 -Heuristic
```

### Languages (spec 021)
- **UI**: ES/EN toggle in the top bar (persisted in the browser).
- **Interview**: *Interview language* selector on JD upload and on profile review; it is sent as `language: "es" | "en"` in `POST /api/interview/sessions` and `POST /api/interview/sessions/{id}/answers`. It controls the language of questions, judge feedback, and the study guide (with a real LLM or heuristic). Without `language`, the historical behavior is kept.
`local-check.ps1` uses SuperVL with `EnableThinking=false, MaxTokens=4096` (thinking ON is too slow). It verifies that `/api/jd/parse` returns `profileSource=llm` and that the judges emit `overallScore/technicalDepth/productionReadiness/verdict`. See spec 020.

> **Vite proxy**: the browser calls the API via `frontend/vite.config.ts` (`/api → http://127.0.0.1:5210`, overridable with `VITE_API_PROXY_TARGET`). If you see **502 Bad Gateway** on `/api/*`, the API is not on that port or the Vite server is stale: relaunch it with `run-local.ps1`. The E2E now verifies this proxy (`GET :5174/api/health`).

---

## 2. EF Core migrations against the target DB

Apply the migrations BEFORE running the `-UseDatabase` E2E or deploying the API.

```powershell
dotnet tool install --global dotnet-ef

# Apuntar al servidor destino
$env:INTERVIEWDRILL_CONNECTION_STRING = "<connection string from step 3 or 4>"

# Ejecuta las migraciones (Users, JobDescriptions, InterviewSessions,
#   Questions(varbinary(max)), Answers, StudyGuides)
dotnet ef database update --project backend/src/InterviewDrill.Infrastructure --startup-project backend/src/InterviewDrill.Api
```

> **Note:** the migrations use `varbinary(max)` for the embeddings (compatible with SQL Server on-prem/Express, Azure SQL and RDS). Semantic deduplication is resolved in memory with `VectorMath.CosineSimilarity`; it does not depend on `VECTOR` indexes.

---

## 3. Azure (Azure SQL Database + App Service)

### Connection string
```
Server=tcp:YOUR_SERVER.database.windows.net,1433;Database=InterviewDrill_Production;User ID=YOUR_USER;Password=<YOUR_PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```
Or with **Managed Identity** (recommended):
```
Server=tcp:YOUR_SERVER.database.windows.net,1433;Database=InterviewDrill_Production;Authentication=Active Directory Default;Encrypt=True;
```

### Steps
1. **Azure SQL Database** (Serverless/Solo-SQL for development): create the server + firewall rules for the app.
2. **App Service** (.NET 11, Linux OS):
   - App configuration: `INTERVIEWDRILL_CONNECTION_STRING`, `ASPNETCORE_ENVIRONMENT=Production`, `LLM__Provider=AzureOpenAI`, `LLM__AzureOpenAI__*`.
   - If Managed Identity: enable the system-assigned ID and create the DB user + the `db_datawriter/db_datareader` role.
3. **Publish**:
   ```powershell
   dotnet publish backend/src/InterviewDrill.Api -c Release -o ./publish
   ```
4. **Frontend**: `cd frontend && npm ci && npm run build`; upload `frontend/dist` to a Static Web App or serve it behind the same App Service.
5. **Verify health**:
   ```powershell
   curl https://TU_APP.azurewebsites.net/api/health
   # -> { "status":"ok", "persistence":"sql-server" }
   ```

---

## 4. AWS (RDS for SQL Server + ECS/App Runner)

### Connection string
```
Server=YOUR_ENDPOINT.xxxx.us-east-1.rds.amazonaws.com,1433;Database=InterviewDrill_Production;User ID=YOUR_USER;Password=<YOUR_PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### Steps
1. **RDS SQL Server** in a VPC; a Security Group allowing 1433 from the app's subnet.
2. **Secrets Manager**: store the connection string; inject it into the container (App Runner/ECS) as `INTERVIEWDRILL_CONNECTION_STRING`.
3. **LLM (optional) with Bedrock**: `LLM__Provider=Bedrock`, `LLM__Bedrock__Region=us-east-1`. ⚠️ `BedrockLlmClient` throws `NotImplementedException` until `Amazon.Runtime` + `Amazon.BedrockRuntime` and SigV4 signing are added (spec 012, Out of Scope).
4. **Publish** an image or use `dotnet publish` as in Azure.
5. **Verify health** the same way as Azure (`persistence: "sql-server"`).

---

## 5. Quality validation after deployment

```powershell
# E2E de anti-repetición contra la BD REAL (usa la cadena de entorno)
$env:INTERVIEWDRILL_CONNECTION_STRING = "<target connection string>"
powershell -ExecutionPolicy Bypass -File scripts/e2e-smoke.ps1 -UseDatabase
```

The gate checks:
- `/api/health` reports `persistence='sql-server'`.
- Two sessions with the same user+JD: **0 question overlap** (dedup reading from the DB).

### E2E matrix supported by `scripts/e2e-smoke.ps1`
| Mode | Command | `persistence` | LLM | Requirements |
|------|---------|---------------|-----|------------|
| Deterministic in-memory (default, CI) | `./scripts/e2e-smoke.ps1` | `in-memory` | Heuristic | None (it clears environment keys) |
| Real DB | `./scripts/e2e-smoke.ps1 -UseDatabase` | `sql-server` | Heuristic | Migrated DB + `INTERVIEWDRILL_CONNECTION_STRING` |
| Real DB + real LLM | `./scripts/e2e-smoke.ps1 -UseDatabase -WithLlm` | `sql-server` | SuperVL (NIM) | the above + `NVIDIA_API_KEY` or user-secrets; the gate uses `thinking=OFF, max_tokens=4096` |

---

## 6. Observability and resilience in production

- **OpenTelemetry** (spec 013): the `OpenTelemetry` and `Resilience` sections in `appsettings.json` (ServiceName, Exporter, MaxRetryAttempts, CircuitBreaker…). The default exporter is `Console`; switch to OTLP/Azure Monitor in production.
- **PII-free logging**: `PiiSanitizingLoggerProvider` wraps the sinks so sensitive data is never dumped (spec 014).
- **LLM policy**: `OpenAICompatible` (NVIDIA NIM / OpenAI / OpenCode, SuperVL base model) → `AzureOpenAI`/Foundry → `Ollama` (free local) → `Heuristic` (offline), depending on your keys (spec 012). `Bedrock` remains open with a SigV4 skeleton.
