# Specification 017: E2E Quality Gates & Integration Suite

## Overview
Define and maintain the end-to-end quality-gate suite that validates the complete system in two modes: **in-memory** (demo/CI) and **SQL Server Express / real DB** (`-UseDatabase`). The suite is the Constitution's final gate: a phase is not considered complete without green E2E (both modes).

## Stack
- **.NET 11 / C# 15** (API + SignalR)
- **PowerShell** (`scripts/e2e-smoke.ps1`) as the E2E smoke/integration orchestrator
- **SQL Server Express** on `localhost\SQLEXPRESS` (DB `InterviewDrill_Testing`) for `-UseDatabase` mode
- **QuestPDF** for the PDF guide download

## User Stories
- **As an** engineer, **I want** a script that brings up backend+frontend and walks the complete flow, **so that** I can confirm the platform works end-to-end.
- **As an** engineer, **I want** to run the same flow against the real DB, **so that** I validate FKs, aggregate persistence, and anti-repetition by reading from SQL.
- **As a** reviewer, **I want** anti-repetition verified automatically between two sessions, **so that** point 5 of the North Star is met.

## Functional Requirements

### FR1: Execution modes
- [x] Default mode: in-memory (`INTERVIEWDRILL_CONNECTION_STRING` null) → health reports `persistence = "in-memory"`.
- [x] Real DB mode: `-UseDatabase` → exports `INTERVIEWDRILL_CONNECTION_STRING` to `localhost\SQLEXPRESS;Database=InterviewDrill_Testing;Trusted_Connection=True` → health reports `persistence = "sql-server"`.

### FR2: E2E steps
- [x] Wait for backend (`/api/health`) and frontend (Vite dev server).
- [x] Mode assertion: the `persistence` field of health matches the chosen mode.
- [x] Parse a JD → `JobProfile` with `requiredSkills`.
- [x] Run the complete adaptive interview until `interviewCompleted` (max. 8 questions).
- [x] Obtain `studyGuideMarkdown` on close.
- [x] History returns the session with `status = Completed`.
- [x] `GET .../study-guide` returns markdown; `GET .../study-guide.pdf` returns bytes with a `%PDF-` header.

### FR3: Anti-repetition gate
- [x] Run a **second** session with the same `userId`+`JobProfile`.
- [x] Verify that session 2's `sessionId` differs from session 1's.
- [x] Assertion: the intersection of question texts between session 1 and session 2 is empty (0 overlap).
- [x] With `-UseDatabase`, the 0 overlap between different sessions/requests proves that session 1's questions were **persisted in the DB** and session 2 consulted them (DbContext Scoped per request).

## Non-Functional Requirements
- [x] Runnable in CI and locally without LLM credentials (Heuristic mode).
- [x] Deterministic in structure (does not require exact questions, requires that they not repeat).
- [x] Each run uses a unique `userId` (GUID) to avoid colliding with data from previous runs.

## Acceptance Criteria
- [x] `scripts/e2e-smoke.ps1` (no flag) passes and reports `persistence='in-memory'`.
- [x] `scripts/e2e-smoke.ps1 -UseDatabase` passes and reports `persistence='sql-server'`.
- [x] Both modes print `E2E anti-repetition OK ... 0 overlap with session 1`.
- [x] The real DB `InterviewDrill_Testing` contains the aggregate (Users, JobDescriptions, InterviewSessions, Questions, Answers, StudyGuides) after a `-UseDatabase` run.

## Out of Scope
- Load/stress (performance) testing — covered by unit/integration tests, not by the E2E smoke.
- Real browser coverage (Lighthouse/WCAG) — lives in spec 008.

## Dependencies
- **Spec 005** Interviewer Agent (adaptive loop, anti-repetition)
- **Spec 009** JD Parser (`/api/jd/parse`)
- **Spec 010** Study Guide Generator + **Spec 011** Persistence (`/study-guide`, `/study-guide.pdf`)
- **Spec 013** Observability/Health (`/api/health`)

## Risks
| Risk | Mitigation |
|------|------------|
| `-UseDatabase` mode cannot create the aggregate due to an FK | Verified: `EfInterviewerSessionStore.SaveAsync` materializes User→JobDescription→Session with intermediate `SaveChangesAsync` calls; `-UseDatabase` E2E approved |
| Previous runs pollute dedup | Unique GUID `userId` per run |
| Host AppLocker/WDAC blocks execution of .NET unit test assemblies | E2E launches the API as a process (`dotnet run`) plus the frontend; does not depend on the Infrastructure.Tests assembly |
