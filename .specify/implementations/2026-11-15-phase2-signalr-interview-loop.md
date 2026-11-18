# Implementation Record — Phase 2 SignalR + Interview Loop

## Task Reference
- `.specify/tasks/005-interviewer-agent-tasks.md`
- **Task 6**: Connect SignalR hub for real time.
- Related:
  - Phase 2 answer processing
  - PII Task 4 partial pipeline integration for answers

## Changes Made
### Core contracts
- `backend/src/InterviewDrill.Core/Contracts/Evaluation.cs`:
  - added optional `SkillTarget` to `CandidateResponse`.
- `backend/src/InterviewDrill.Core/Contracts/InterviewFlow.cs`:
  - added `LastQuestionId` to `InterviewerSessionState`.
- `backend/src/InterviewDrill.Core/Contracts/InterviewAnswerProcessor.cs`:
  - added `AnswerSubmission`, `InterviewAnswerResult` and `IInterviewAnswerProcessor`.
- `backend/src/InterviewDrill.Core/Contracts/InterviewerExceptions.cs`:
  - added `AnswerRequiredException`.

### Agents
- `InterviewerAgent.cs`:
  - stores `LastQuestionId` for answer evaluation.
- `InMemoryQuestionRegistry.cs`:
  - offline question deduplication when no SQL connection string is configured.
- `HeuristicArchitectureEvaluator.cs`:
  - deterministic technical depth scoring.
- `HeuristicSecurityRigorJudge.cs`:
  - deterministic production-readiness and resilience scoring.
- `ScoringSynthesizer.cs`:
  - C# weighted scoring and verdict synthesis using Core scoring utilities.
- `InterviewAnswerProcessor.cs`:
  - sanitizes candidate answer before evaluators.
  - evaluates answer architecture/security and synthesizes feedback.
  - asks Interviewer for next question.

### API
- `Program.cs`:
  - added SignalR.
  - DI for interviewer, evaluators, scoring synthesizer and answer processor.
  - conditional SQL persistence vs in-memory registry based on `INTERVIEWDRILL_CONNECTION_STRING`.
  - `POST /api/interview/sessions`
  - `POST /api/interview/sessions/{sessionId}/answers`
- `InterviewHub.cs`:
  - SignalR hub with join/leave group, start session and submit answer.
- `Models/InterviewApiModels.cs`:
  - API DTOs for interview endpoints.
- Infrastructure:
  - `HashEmbeddingGenerator` for offline/deterministic embeddings.

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests:              32 passed
InterviewDrill.Agents.Tests:            27 passed
InterviewDrill.Infrastructure.Tests:    41 passed
InterviewDrill.Api.Tests:                8 passed
Total:                                  108/108 passed
```

## Notes
- SignalR hub backend is implemented and compile-verified; frontend browser client remains Phase 2.
- Current interview loop can run fully offline:
  - heuristic JD parser
  - heuristic question generator
  - deterministic evaluators
  - C# scoring
  - in-memory question registry
- When `INTERVIEWDRILL_CONNECTION_STRING` is configured, `QuestionRegistry` switches to SQL/Azure-backed exact + semantic dedup.
- PII sanitization now happens before evaluators receive candidate answers.
- The evaluator panel is heuristic/deterministic for now; hybrid LLM-as-judge remains Phase 3 hardening.

## Review Checklist
- [x] Code follows project standards
- [x] All tests pass
- [x] No compile errors
- [x] Documentation updated
- [x] Task/spec/status files updated
- [ ] SignalR client tests added in React
- [ ] Hybrid LLM evaluator integration pending
- [ ] Performance impact assessed with real Azure SQL and LLM
