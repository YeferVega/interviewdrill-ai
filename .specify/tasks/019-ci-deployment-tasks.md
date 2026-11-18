# Task List: 019-ci-deployment

## Sprint/Iteration: Phase 6 — CI/CD, Deployment and Coverage Closure

### Tasks

#### Task 1: Cover the PDF extraction happy path
- **Description**: Generate a real PDF with QuestPDF (dependency already included) containing the JD text and pass it through `JdDocumentTextExtractor.ExtractAsync`; Assert that the PdfPig text includes the content. Covers the `ExtractPdf` branch, which was at 0%.
- **Priority**: Medium
- **Estimated Time**: 1 hour
- **Dependencies**: Spec 009 (extractor)
- **Status**: [x] Complete — `ExtractPdf` and `JdDocumentTextExtractor` at 100%.

#### Task 2: Cover appsettings reading in DbContextFactory
- **Description**: With a temporary CWD pointing to `../InterviewDrill.Api/appsettings.json`, validate that `GetDefaultConnectionString` reads `ConnectionStrings:DefaultConnection`. No env var and no appsettings → default fallback.
- **Priority**: Medium
- **Estimated Time**: 1 hour
- **Dependencies**: None
- **Status**: [x] Complete — `InterviewDrillDbContextFactory` 95.8% (paths: env, appsettings-file, fallback).

#### Task 3: Add the CI pipeline (.github/workflows/ci.yml)
- **Description**: Jobs `backend` (build + tests + `coverage.runsettings` + upload), `frontend` (npm ci + oxlint + vitest + vite build), `e2e` on `windows-latest` running `scripts/e2e-smoke.ps1` in in-memory mode.
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Specs 017/018
- **Status**: [x] Complete — workflow created; gates aligned with the Definition of Done.

#### Task 4: Write the deployment guide (DEPLOYMENT.md)
- **Description**: Environment variable matrix, Azure SQL connection string (incl. Managed Identity) and AWS RDS, `dotnet ef database update` commands, `persistence='sql-server'` verification, and `-UseDatabase` validation.
- **Priority**: High
- **Estimated Time**: 2 hours
- **Dependencies**: Specs 011/012/013
- **Status**: [x] Complete

#### Task 5: Document SDD unit 019
- **Description**: Create spec/plan/tasks 019 and update `IMPLEMENTATION_STATUS.md` (counters and CI/Deploy section), `.specify/memory/constitution.md` (CI gate), and `00-MASTER-PLAN.md`.
- **Priority**: Medium
- **Estimated Time**: 1 hour
- **Dependencies**: Tasks 1-4
- **Status**: [x] Complete

### Definition of Done
- [x] Remaining coverage gaps closed or documented as out-of-scope
- [x] CI pipeline with backend/frontend/e2e jobs, aligned with the Constitution's gates
- [x] Deployment guide with Azure SQL and AWS RDS (variables + connection strings + verification)
- [x] Suite 241/241 (Core 35 + Agents 81 + Infrastructure 113 + Api 12) and in-memory E2E OK
- [x] All SDD artifacts (spec/plan/tasks/constitution/status/master plan) updated
