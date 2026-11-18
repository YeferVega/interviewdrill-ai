# Implementation Record — 011 Persistence & Anti-Repetition Task 2

## Task Reference
- `.specify/tasks/011-persistence-antirepetition-tasks.md`
- **Task 2**: Initial EF Core migration with SQL Server/Azure SQL, unique index `(UserId, RawTextHash)` and cosine vector index over `Questions.Embedding`.

## Changes Made
- `.specify/specs/011-persistence-antirepetition.md`: updated the provider to SQL Server/Azure SQL, corrected the pending semantic-similarity acceptance threshold from 0.85 to 0.92, and marked the migration as created but pending application to a real database.
- `backend/src/InterviewDrill.Infrastructure/InterviewDrill.Infrastructure.csproj`: replaced the SQLite provider with `Microsoft.EntityFrameworkCore.SqlServer` 11.0.0 and added `Microsoft.EntityFrameworkCore.Design` to generate migrations.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Entities/QuestionEntity.cs`: changed `float[]? Embedding` to `SqlVector<float>? Embedding`.
- `backend/src/InterviewDrill.Infrastructure/Persistence/InterviewDrillDbContext.cs`: configured `vector(1536)` and a cosine vector index with `HasMetric("cosine")`.
- `backend/src/InterviewDrill.Infrastructure/Persistence/InterviewDrillDbContextFactory.cs`: added a design-time factory for `dotnet ef` using `INTERVIEWDRILL_CONNECTION_STRING`.
- `backend/src/InterviewDrill.Infrastructure/Persistence/Migrations/*InitialCreate.cs`: generated the initial EF Core migration for SQL Server/Azure SQL.
- Deleted `backend/src/InterviewDrill.Infrastructure/Persistence/Converters/FloatArrayJsonConverter.cs`: no longer needed with the native vector type.
- Replaced SQLite test with model tests verifying native vector mapping, cosine vector index and migration presence.

## Testing
```text
dotnet build backend/InterviewDrill.slnx
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test backend/InterviewDrill.slnx
InterviewDrill.Core.Tests: 26 passed
InterviewDrill.Agents.Tests: 2 passed
InterviewDrill.Infrastructure.Tests: 3 passed
Total: 31/31 passed
```

## Notes
- `HasVectorIndex` / vector index support is marked experimental by EF 11 (`EF9105`); suppressed narrowly at the index configuration because the selected cloud runtime requires it for Azure SQL/SQL Server 2025 vector support.
- Migration is generated and statically tested by EF model assertions, but "apply against real database" remains pending because no live SQL Server/Azure SQL connection string is configured in this environment.
- Provider selected by user directive: run on AWS or Azure. Azure SQL / SQL Server 2025 is the best fit for EF Core 11 native vector APIs; AWS can host SQL Server RDS if needed.

## Review Checklist
- [x] Code follows project standards
- [x] All tests pass
- [x] No linting/build errors
- [x] Documentation updated
- [x] Migration generated with native vector column and cosine index
- [ ] Applied to live Azure SQL / AWS SQL Server database (pending connection string)
