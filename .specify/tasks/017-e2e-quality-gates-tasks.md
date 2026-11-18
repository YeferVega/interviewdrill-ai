# Task List: 017-e2e-quality-gates

## Sprint/Iteration: Phase 4 — E2E Integration Suite and real database

### Tasks

#### Task 1: Parametrize `e2e-smoke.ps1` with `-UseDatabase`
- **Description**: Add `[CmdletBinding()] param([switch]$UseDatabase)` and set/clear `INTERVIEWDRILL_CONNECTION_STRING` depending on the mode. Validate syntax with the PowerShell parser before running.
- **Priority**: High
- **Estimated Time**: 0.5 hours
- **Dependencies**: None
- **Status**: [x] Complete — `param()` at the top (first statement), configurable mode, mode messages printed.

#### Task 2: Refactor the interview loop into `Invoke-InterviewRun`
- **Description**: Extract the start+answer-loop flow into a function that returns `sessionId`, the ordered list of question texts, and `studyGuideMarkdown`, so it can be reused for a second session.
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: Task 1
- **Status**: [x] Complete — function reused for sessions 1 and 2.

#### Task 3: Mode assertion via `/api/health`
- **Description**: Read the `persistence` field from health and require `sql-server` in database mode and `in-memory` in default mode.
- **Priority**: High
- **Estimated Time**: 0.5 hours
- **Dependencies**: Task 1
- **Status**: [x] Complete — checks added after `Wait-For-Http`.

#### Task 4: Anti-repetition gate between two sessions
- **Description**: Run a second session with the same `userId`+`profile`; verify a different `sessionId` and that the intersection of questions between sessions 1 and 2 is empty (0 overlap).
- **Priority**: High
- **Estimated Time**: 1 hour
- **Dependencies**: Tasks 2, 3
- **Status**: [x] Complete — in the real database mode: "session 2 asked 8 distinct questions, 0 overlap with session 1".

#### Task 5: Verify real aggregate persistence in SQL Server Express
- **Description**: Confirm that the database wiring creates the complete aggregate (User/JobDescription/Session/Questions/Answers/StudyGuide). Inspection of `EfInterviewerSessionStore.SaveAsync` + `-UseDatabase` run.
- **Priority**: High
- **Estimated Time**: 0.5 hours
- **Dependencies**: Task 4
- **Status**: [x] Complete — `SaveAsync` materializes the aggregate with intermediate `SaveChangesAsync` calls; the `-UseDatabase` run passed with `persistence='sql-server'`.

#### Task 6: Reconcile the Constitution with provider compatibility
- **Description**: Update `memory/constitution.md`: persistence stack (varbinary + in-memory cosine on SQL Server Express; native `vector(1536)` only on Azure SQL/SQL Server 2025+), E2E quality gates (in-memory + `-UseDatabase`), and Definition of Done.
- **Priority**: Medium
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 1-5
- **Status**: [x] Complete

#### Task 7: Update `IMPLEMENTATION_STATUS.md` and `DATABASE_SETUP.md`
- **Description**: Document the E2E suite with a real database, the new counters (specs/plans/tasks 000-017), and how to run the API and the E2E against `InterviewDrill_Testing`.
- **Priority**: Medium
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 1-6
- **Status**: [x] Complete — both documents updated.

#### Task 8: Configurable connection string in `-UseDatabase` (Option B)
- **Description**: Instead of pinning `localhost\SQLEXPRESS`, make `-UseDatabase` use `INTERVIEWDRILL_CONNECTION_STRING` first when it is defined (for Azure/AWS or another instance) and, if not, fall back by default to the local SQLEXPRESS/InterviewDrill_Testing instance. The child API inherits the variable via `Start-Process`.
- **Priority**: Medium
- **Estimated Time**: 0.5 hours
- **Dependencies**: Tasks 1-5
- **Status**: [x] Complete — fallback validated (`usando INTERVIEWDRILL_CONNECTION_STRING del entorno` with the variable set; `fallback localhost\SQLEXPRESS` without it); E2E green in both cases.

#### Task 9: Run the 58 Infrastructure xUnit tests on the host
- **Description**: After turning off the host's Windows Defender Application Control / Smart App Control, run `dotnet test` for the `InterviewDrill.Infrastructure.Tests` project and confirm the result.
- **Priority**: Medium
- **Estimated Time**: 0.25 hours
- **Dependencies**: Task 5
- **Status**: [x] Complete — **58/58 passing** (total suite 147/147). Documented that the previous block was an OS policy, not the code.

### Definition of Done
- [x] `e2e-smoke.ps1` parametrized with valid syntax
- [x] Automated anti-repetition gate between two sessions
- [x] `-UseDatabase` mode verifies `persistence='sql-server'` and real aggregate persistence
- [x] Configurable connection string with fallback (Option B)
- [x] 58 Infrastructure xUnit tests executed and passing on the host (147/147)
- [x] Constitution reconciled (stack + quality gates + DoD)
- [x] `IMPLEMENTATION_STATUS.md` and `DATABASE_SETUP.md` updated
- [x] Build 0 errors; all Core/Agents/Api/Infrastructure tests with no regression
