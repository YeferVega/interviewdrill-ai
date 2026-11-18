# Implementation Record — 011 Persistence & Anti-Repetition Task 3

## Task Reference
- `.specify/tasks/011-persistence-antirepetition-tasks.md`
- **Task 3**: Implement `QuestionRegistry` for exact anti-repetition via normalized hash `(UserId, RawTextHash)`.

## Changes Made
- `backend/src/InterviewDrill.Core/Contracts/IAgents.cs`: updated `IQuestionRegistry.RegisterAsync` to require `sessionId`, because `Questions` rows are linked to an interview session in the persistence schema.
- `backend/src/InterviewDrill.Infrastructure/Persistence/IQuestionRepository.cs`: added repository abstraction for question existence and insertion.
- `backend/src/InterviewDrill.Infrastructure/Persistence/SqlQuestionRepository.cs`: added EF Core SQL Server/Azure SQL implementation querying `Questions` by `(UserId, JobDescriptionHash, NormalizedQuestionHash)`.
- `backend/src/InterviewDrill.Infrastructure/Persistence/QuestionRegistry.cs`: implemented exact duplicate detection using normalized question text + SHA-256 hash.
- `backend/tests/InterviewDrill.Infrastructure.Tests/Persistence/QuestionRegistryTests.cs`: added unit tests using fake repository to validate hash normalization, duplicate rejection, and per-user isolation.

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests:              26 passed
InterviewDrill.Agents.Tests:             2 passed
InterviewDrill.Infrastructure.Tests:     6 passed
Total:                                  34/34 passed
```

## Notes
- The registry intentionally uses normalized hashing instead of raw question text so trivial formatting changes do not defeat exact deduplication.
- `WasAlreadyAskedAsync` and `RegisterAsync` both compute the same normalized hash.
- Semantic deduplication remains Task 4; for now the registry checks exact duplicates only.
- SQL EF query can be benchmarked against real Azure SQL later; the lookup is index-backed by the generated unique composite index.

## Review Checklist
- [x] Code follows project standards
- [x] All tests pass
- [x] No linting/build errors
- [x] Documentation updated
- [x] Contract and task records updated
- [ ] Performance impact assessed (pending real Azure SQL benchmark)
