# Technical Plan 017: E2E Quality Gates & Integration Suite

## Objective
Consolidate `scripts/e2e-smoke.ps1` as the **single E2E gate** with two modes (in-memory and `-UseDatabase` SQL Server Express), including an automatic anti-repetition check across sessions.

## Approach
1. **Parameterize the script** with `[switch]$UseDatabase`:
   - Without the flag → `INTERVIEWDRILL_CONNECTION_STRING = $null` (in-memory).
   - With the flag → export the connection string to `localhost\SQLEXPRESS;Database=InterviewDrill_Testing`.
2. **Refactor the interview loop** into `Invoke-InterviewRun($ApiUrl,$UserId,$Profile)` that returns the `sessionId`, the ordered list of question texts and the `studyGuideMarkdown`.
3. **Mode assertion** by reading `/api/health` → `persistence` field (`in-memory` / `sql-server`).
4. **Anti-repetition gate**: invoke `Invoke-InterviewRun` twice with the same `userId`+`profile` and check that the intersection of questions is empty.
5. **Real DB verification**: the 0 overlap between two distinct requests/sessions, with a per-request Scoped `DbContext`, demonstrates real persistence of `Questions` and reading via `SqlQuestionRepository`.

## Decisions
- **A single script, two modes** (do not duplicate a second script): avoids divergence and complies with DRY.
- **A GUID `userId` per run**: idempotence with respect to historical data in the DB.
- **Do not require exact questions**: only require non-repetition (structural determinism, not content determinism).
- **Health as mode proof**: `/api/health` already exposes `persistence` (spec 013).

## Risks and mitigation
| Risk | Mitigation |
|--------|------------|
| FK violation when creating the aggregate in a real DB | Confirmed by code reading: `EfInterviewerSessionStore.SaveAsync` materializes User→JobDescription→Session with intermediate `SaveChangesAsync` calls |
| AppLocker/WDAC blocks Infrastructure unit tests | The E2E does not depend on `InterviewDrill.Infrastructure.Tests.dll`; it launches the API per process |
| Frontend does not start (port in use) | `Wait-For-Http` with timeout; the process is stopped in `finally` |

## Exit criteria (gate)
- [x] `e2e-smoke.ps1` (in-memory) green
- [x] `e2e-smoke.ps1 -UseDatabase` green
- [x] Both report `0 overlap with session 1`
- [x] `dotnet build` 0 errors; runnable tests (Core/Agents/Api) green
