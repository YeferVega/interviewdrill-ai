# Implementation Record — History Endpoint + React History UI

## Task Reference
- `.specify/tasks/011-persistence-antirepetition-tasks.md` Task 6
- `.specify/tasks/008-frontend-ui-tasks.md` Tasks 9-11
- Phase 4 Study Guide & History

## Changes Made
### Backend architecture correction
- Moved `ISessionHistoryRepository` and `SessionHistorySourceRecord` from Infrastructure to Core.
- Fixed dependency direction violation: `InterviewDrill.Agents` now depends only on Core, not Infrastructure.

### Backend contracts
- Added `InterviewSessionRecordStatus`.
- Added `StartedAt` to `InterviewerSessionState`.
- In-memory interviewer store exposes all states and completed-at values.
- In-memory study guide repository exposes guide ID/createdAt for history.

### Backend persistence
- Added `InMemorySessionHistoryRepository`.
- Updated `SqlSessionHistoryRepository` to map EF status to Core enum.
- Updated `SessionHistoryService` to consume Core repository contract.

### API endpoints
Added:
```http
GET /api/interview/sessions?userId={userId}
GET /api/interview/sessions/{sessionId}/study-guide
```
Registered DI:
- `ISessionHistoryRepository`
- `ISessionHistoryService`
- `VerdictDeterminer`

### Frontend
- Added `HistoryPage`.
- Added history state to Zustand store:
  - history list
  - loading state
  - selected session
  - selected guide markdown
- Added `openView('interview'|'history')`.
- Added API clients:
  - `getInterviewHistory`
  - `getStudyGuide`
- Added top navigation:
  - Entrevista
  - Historial
- Added responsive history card layout and accessibility labels.
- Added pure date formatting utility and tests.

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests:              32 passed
InterviewDrill.Agents.Tests:            36 passed
InterviewDrill.Infrastructure.Tests:    42 passed
InterviewDrill.Api.Tests:               11 passed
Total:                                  121/121 passed
```

```text
cd frontend
npm run lint
npm run test
Test Files  5 passed (5)
Tests       13 passed (13)

npm run build
build OK
JS gzip 90.13 kB
```

## Notes
- History endpoint works offline through in-memory history repository.
- SQL/Azure SQL mode uses persisted sessions/answers/guides.
- New API tests verify started sessions appear in history, unknown user returns empty list and missing guide returns 404.
- The UI currently uses the anonymous persistent user ID stored in localStorage, matching MVP auth decision.

## Review Checklist
- [x] Code follows project standards
- [x] Backend tests pass
- [x] Frontend tests pass
- [x] No lint/build errors
- [x] Documentation updated
- [ ] Real E2E browser test with backend running
- [ ] Formal WCAG audit
- [ ] Applied migration in Azure SQL/AWS SQL
