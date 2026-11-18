# Implementation Record — 011 Persistence & Anti-Repetition

## Task Reference
- `.specify/tasks/011-persistence-antirepetition-tasks.md`
- **Task 1**: Create EF Core entities (`Users`, `JobDescriptions`, `InterviewSessions`, `Questions`, `Answers`, `StudyGuides`) + columns for embeddings.

## Changes Made
- `backend/src/InterviewDrill.Infrastructure/InterviewDrill.Infrastructure.csproj`: added `Microsoft.EntityFrameworkCore.Sqlite` 11.0.0 for development/testing persistence model.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Entities/UserEntity.cs`: created user entity with browser token and audit timestamps.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Entities/JobDescriptionEntity.cs`: created JD entity with `RawTextHash` and parsed `JobProfile` JSON.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Entities/InterviewSessionEntity.cs`: created session entity with status, start/end and relationships.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Entities/QuestionEntity.cs`: created question entity with `UserId`, `JobDescriptionHash`, `NormalizedQuestionHash`, `Difficulty`, `SkillTarget` and optional `Embedding`.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Entities/AnswerEntity.cs`: created answer entity with text and evaluation JSON.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Entities/StudyGuideEntity.cs`: created study guide entity linked one-to-one to session.
- `backend/src/InterviewDrill.Infrastructure/Persistence/InterviewDrillDbContext.cs`: added EF Core `DbContext`, relationships, unique indexes for `(UserId, RawTextHash)` and `(UserId, JobDescriptionHash, NormalizedQuestionHash)`, and embedding conversion.
- `backend/src/InterviewDrill.Infrastructure/Persistence/QuestionHashing.cs`: added normalized question text hashing helper for exact anti-repetition.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Converters/FloatArrayJsonConverter.cs`: added EF value converter/comparer for `float[]?` embedding persistence in SQLite.
- `backend/tests/InterviewDrill.Infrastructure.Tests/...`: created infrastructure test project and EF SQLite tests.

## Testing
- Added `InterviewDrill.Infrastructure.Tests` xUnit project.
- `dotnet build backend/InterviewDrill.slnx`: succeeded with 0 warnings and 0 errors.
- `dotnet test backend/InterviewDrill.slnx`: passed.
- Test totals: **30/30**
  - `InterviewDrill.Core.Tests`: 26 passed
  - `InterviewDrill.Agents.Tests`: 2 passed
  - `InterviewDrill.Infrastructure.Tests`: 2 passed

## Notes
- SQLite was initially used for development/testing before the cloud provider decision was made.
- Follow-up: Task 2 replaced SQLite with SQL Server/Azure SQL native vector support. See `.specify/implementations/2026-11-13-011-persistence-task2-migration.md`.
- The native `vector(1536)` column and cosine vector index are now configured for SQL Server/Azure SQL.
- Exact duplicate question prevention is represented by the unique composite index `(UserId, JobDescriptionHash, NormalizedQuestionHash)`.
- Session checkpoint behavior remains pending Task 5 in the task list.

## Review Checklist
- [x] Code follows project standards
- [x] All tests pass
- [x] No linting errors from dotnet build
- [x] Documentation updated
- [ ] Performance impact assessed (pending Task 2/7: real provider, vector indexes, query benchmarks)
