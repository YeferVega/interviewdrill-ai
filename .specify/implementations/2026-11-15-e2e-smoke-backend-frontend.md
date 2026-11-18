# Implementation Record — E2E Smoke Backend + Frontend

## Task Reference
- `.specify/tasks/005-interviewer-agent-tasks.md`
- `.specify/tasks/010-study-guide-generator-tasks.md`
- `.specify/tasks/011-persistence-antirepetition-tasks.md`

## Changes Made
- Added `scripts/e2e-smoke.ps1` that runs the real backend API and frontend dev server together.
- Updated `frontend/index.html` title/lang/description to InterviewDrill.
- Fixed a real functional gap discovered by the E2E smoke:
  - `InterviewerAgent` did not close the interview at the product's maximum of 8 questions.
  - Added `InterviewerAgent.MaxQuestions = 8`.
  - `NextQuestionAsync` now clears last-question state and returns null once 8 questions have been asked.
- Added unit test:
  - `NextQuestionAsync_ReturnsNullAtEightAskedQuestions`.

## E2E Smoke Covers
1. Backend API starts on `http://127.0.0.1:5210`.
2. Frontend Vite dev server starts on `http://127.0.0.1:5174`.
3. Frontend HTML contains InterviewDrill brand/title.
4. JD parse endpoint returns required skills.
5. Interview start endpoint returns sessionId and first question.
6. Answer loop submits production-quality answers until completion.
7. Completion response includes `studyGuideMarkdown`.
8. History endpoint returns the session as `Completed`.
9. Study guide endpoint returns persisted markdown.
10. Processes are stopped in `finally`.

## Running the smoke test
```powershell
powershell -ExecutionPolicy Bypass -File scripts/e2e-smoke.ps1
```

## Result
```text
Backend API is ready at http://127.0.0.1:5210/api/health
Frontend dev server is ready at http://127.0.0.1:5174
E2E smoke passed: backend API, frontend server, JD parse, adaptive interview, guide and history.
```

## Unit/Integration Tests After Fix
```text
InterviewDrill.Core.Tests:              32 passed
InterviewDrill.Agents.Tests:            37 passed
InterviewDrill.Infrastructure.Tests:    42 passed
InterviewDrill.Api.Tests:               11 passed
Total:                                  122/122 passed
```

## Notes
- This smoke test verifies backend + frontend servers together over HTTP, not visual browser automation.
- Playwright/browser interaction and formal WCAG audit remain pending.
- The 8-question completion fix aligns implementation with `000-product-vision.md` and `005-interviewer-agent.md`.

## Review Checklist
- [x] Code follows project standards
- [x] Tests pass after functional fix
- [x] E2E smoke passes
- [x] Documentation updated
- [ ] Browser UI interaction tests with Playwright
- [ ] Formal accessibility audit
