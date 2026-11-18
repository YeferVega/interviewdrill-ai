# Specification 019: CI/CD, Deployment & Coverage Closure

## Overview
Consolidate the project's operation: a continuous-integration pipeline that reproduces the Constitution's gates (build, tests + honest coverage, frontend lint/test/build, in-memory E2E with anti-repetition), a deployment guide for Azure SQL and AWS RDS, and the closure of the last coverage gaps (PDF happy path and reading of `appsettings.json` by the factory).

## Stack
- **CI**: GitHub Actions (`.github/workflows/ci.yml`), ubuntu jobs (backend/frontend) + windows-latest (E2E smoke that launches `dotnet` and `cmd.exe`).
- **Deployment**: Azure SQL + App Service; AWS RDS for SQL Server + App Runner/ECS.
- **Coverage**: `dotnet test --collect:"XPlat code coverage" --settings backend/coverage.runsettings`.

## User Stories
- **As a** team, **I want** a pipeline that blocks the merge if build/test/E2E fail, **so that** the Constitution's Definition of Done is enforced automatically.
- **As an** operator, **I want** clear steps to deploy on Azure or AWS with its connection string, **so that** the API runs against a real DB.
- **As a** QA, **I want** a reproducible E2E gate in CI (in-memory mode), **so that** anti-repetition is verified in every PR without SQL Server.

## Functional Requirements
### FR1: CI pipeline (`.github/workflows/ci.yml`)
- [x] `backend`: restore + build (Release) + `dotnet test --collect --settings coverage.runsettings` + coverage artifact upload.
- [x] `frontend`: `npm ci` + `npm run lint` + `npm test` (vitest) + `npm run build` (tsc + vite).
- [x] `e2e` (windows-latest): backend build + `npm ci` + `scripts/e2e-smoke.ps1` in in-memory mode, anti-repetition.

### FR2: Deployment (Azure SQL / AWS RDS)
- [x] Environment variable matrix (`INTERVIEWDRILL_CONNECTION_STRING`, `LLM__*`).
- [x] Connection string per provider (Azure SQL / Managed Identity; AWS RDS).
- [x] `dotnet ef database update` steps before deploying.
- [x] Verification `GET /api/health` → `persistence='sql-server'`.
- [x] Post-deploy validation with `scripts/e2e-smoke.ps1 -UseDatabase`.

### FR3: Coverage closure
- [x] `JdDocumentTextExtractor.ExtractPdf` happy path (generates a real PDF with QuestPDF and extracts it with PdfPig) → 100%.
- [x] `InterviewDrillDbContextFactory.GetDefaultConnectionString` reading `appsettings.json` → 95.8%.
- [x] Error paths: `ExtractAsync` null / invalid PDF / invalid DOCX → `JdDocumentException`.

## Non-Functional Requirements
- [x] CI requires no secrets: runs in Heuristic/in-memory mode.
- [x] CI requires no SQL Server: the DB gate (`-UseDatabase`) is local/self-hosted.

## Acceptance Criteria
- [x] `.github/workflows/ci.yml` syntactically correct and referenced in the docs.
- [x] Local suite **241/241** (Core 35 + Agents 81 + Infrastructure 113 + Api 12).
- [x] Honest coverage (excluding generated): Agents ~90.8%, Infrastructure ~72.9%, `JdDocumentTextExtractor` 100%.
- [x] In-memory E2E green (anti-repetition 0 overlap).
- [x] `DEPLOYMENT.md` documents Azure and AWS with their connection strings and steps.

## Out of Scope
- Automated deployment from CI (no artifacts are published to Azure/AWS in this spec; the API is not deployed in the workflow).
- `-UseDatabase` gate in hosted CI (requires self-hosted SQL Server).
- Signed builds.

## Dependencies
- **Spec 011** (persistence/EF), **Spec 013** (OpenTelemetry/Resilience), **Spec 017** (E2E gate), **Spec 018** (coverage).

## Risks
| Risk | Mitigation |
|------|------------|
| .NET 11 still pre-release on runners | `setup-dotnet` with `include-prerelease: true`; SDK pin note |
| The E2E uses `cmd.exe`/`Start-Process` | Job on `windows-latest` |
| Duplication of gates between CI and local | CI invokes exactly `scripts/e2e-smoke.ps1` and `dotnet test ... coverage.runsettings` |
