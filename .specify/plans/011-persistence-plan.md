# Technical Plan: Persistence & Anti-Repetition (.NET 11)

## Architecture Overview
Persistence layer with EF Core over SQL Server/Azure SQL. Implemented in **C# / .NET 11** within `InterviewDrill.Infrastructure.Persistence`. Includes an in-memory fallback when there is no connection string. Guarantees question anti-repetition for the same user+JD.

```
┌─────────────────────────────────────────────────────────┐
│              Persistence Layer Pipeline                 │
├─────────────────────────────────────────────────────────┤
│ 1. EF Core Entities (Users, JobDescriptions, etc.)      │
│ 2. QuestionRegistry (dedup exacta + semántica)          │
│ 3. Session Checkpoint (sobrevive reinicios)             │
│ 4. Session History (veredictos + guías)                 │
│ 5. In-Memory Fallback (sin connection string)           │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. EF Core Entities (`InterviewDrill.Infrastructure.Persistence/Entities/`)
**Responsibility:** Domain model with relationships.
- `Users` (persistent anonymous Id per browser token in MVP)
- `JobDescriptions` (Id, RawTextHash, ParsedProfile JSON, CreatedAt)
- `InterviewSessions` (Id, UserId, JobDescriptionId, StartedAt, EndedAt, Status, InterviewerStateJson)
- `Questions` (Id, SessionId, Text, Difficulty, SkillTarget, Embedding vector, AskedAt)
- `Answers` (Id, QuestionId, Text, EvaluationPayload JSON, AnsweredAt)
- `StudyGuides` (Id, SessionId, Markdown, CreatedAt)

### 2. IQuestionRegistry (`InterviewDrill.Core.Persistence/IQuestionRegistry.cs`)
**Responsibility:** Interface for the question registry and anti-repetition.
- `HasBeenAsked(userId, rawTextHash, questionText)` → `bool`
- `RegisterQuestion(userId, rawTextHash, questionText, embedding)`

### 3. InterviewDrillDbContext (`InterviewDrill.Infrastructure.Persistence/InterviewDrillDbContext.cs`)
**Responsibility:** EF Core DbContext with vector columns.

### 4. Repositories (`InterviewDrill.Infrastructure.Persistence/Repositories/`)
- `SqlQuestionRepository`, `EfInterviewerSessionStore`, `SqlInterviewAnswerRepository`, `SqlStudyGuideRepository`, `SqlSessionHistoryRepository`
- In-memory implementations when there is no connection string

## Data Flow

```
Usuario + JD ──> QuestionRegistry (dedup exacta por hash normalizado)
                                           │
                                           v
                                  ¿Pregunta repetida?
                                  ┌───────────┴───────────┐
                                  Sí                      No
                                  │                       │
                                  v                       v
                         Regenerar (máx. 3)          Emitir pregunta
                                  │                       │
                                  └───────────┬───────────┘
                                              v
                                    Persistir en EF Core
                                              │
                                              v
                                    Session Checkpoint (InterviewerStateJson)
                                              │
                                              v
                                    Session History (veredictos + guías)
```

## API Design (C#)

```csharp
// Interfaces en Core
public interface IQuestionRepository { ... }
public interface IQuestionRegistry { ... }
public interface IInterviewerSessionStore { ... }
public interface IInterviewAnswerRepository { ... }
public interface IStudyGuideRepository { ... }
public interface ISessionHistoryRepository { ... }

// Implementaciones en Infrastructure
public sealed class SqlQuestionRepository : IQuestionRepository { ... }
public sealed class EfInterviewerSessionStore : IInterviewerSessionStore { ... }
public sealed class InMemoryQuestionRegistry : IQuestionRegistry { ... }
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack |
| **EF Core** | ORM for SQL Server/Azure SQL |
| **SQL Server / Azure SQL** | Native `vector(1536)` support and cosine vector index |
| **Redis** | Optional for hot embeddings cache |
| **In-Memory Fallback** | When there is no connection string |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   ├── InterviewDrill.Core.Persistence/
│   │   ├── IQuestionRepository.cs
│   │   ├── IQuestionRegistry.cs
│   │   ├── IInterviewerSessionStore.cs
│   │   ├── IInterviewAnswerRepository.cs
│   │   ├── IStudyGuideRepository.cs
│   │   └── ISessionHistoryRepository.cs
│   ├── InterviewDrill.Infrastructure.Persistence/
│   │   ├── InterviewDrillDbContext.cs
│   │   ├── Entities/
│   │   ├── Repositories/
│   │   └── Migrations/
│   └── InterviewDrill.Api/
│       └── Controllers/ (endpoints de sesión, respuestas, historial, guía)
└── tests/
    └── InterviewDrill.Infrastructure.Tests/
        └── (persistence tests)
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| EF Core entities | ✅ Complete | Users, JobDescriptions, InterviewSessions, Questions, Answers, StudyGuides |
| Initial EF Core migration | ~[~] In progress | Migration created and verified by model; applying it against SQL Server/Azure SQL pending a connection string |
| Vector columns for embeddings | ✅ Complete | Questions table with native vector support |
| QuestionRegistry (exact dedup) | ✅ Complete | `IQuestionRepository` for testing without a real DB; EF SQL Server query ready |
| Semantic dedup (cosine > 0.92) | ~[~] In progress | Logic, contract and EF query implemented; real embeddings provider and end-to-end test pending |
| Session checkpoint | ✅ Complete | `SessionCheckpoint`, `EfInterviewerSessionStore`, `InterviewerStateJson` |
| Session history with verdicts | ~[~] Partial | Service, EF repository and unit tests implemented; API/final integration pending |
| Anti-repetition and persistence tests | ❌ Not started | All 7 tests in `[ ] Not started` |

## Testing Strategy

### Unit Tests
- **QuestionRegistry**: The same question is not emitted twice (exact), a paraphrase is rejected (> 0.92), same JD with a different user has no cross dedup
- **SessionStore**: An interrupted session resumes on the correct question
- **SessionHistory**: The history returns sessions sorted with verdicts

### Integration Tests (pending, require a real DB)
- Initial migration applies cleanly on an empty DB
- Anti-repetition with real embeddings and DB
- An interrupted session resumes with a real DB
- History with a real DB

### Coverage Target
- > 85% coverage

## Risks

| Risk | Mitigation |
|------|------------|
| Migration does not apply on a real DB | Migration created and verified by model; a real connection string is still missing |
| Semantic dedup without real embeddings | In-memory fallback and logic implemented; real validation pending |
| SQL Server/Azure SQL unavailable | In-memory fallback for all repositories |

## Dependencies

- **Redis (optional)**: Hot embeddings cache in MVP
- **SQL Server/Azure SQL**: Durable persistence (optional in MVP with in-memory fallback)
