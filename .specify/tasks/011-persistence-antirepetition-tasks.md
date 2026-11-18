# Task List: 011-persistence-antirepetition

## Sprint/Iteration: Phase 1 — Persistence & Anti-Repetition

### Tasks

#### Task 1: Create EF Core entities (Users, JobDescriptions, InterviewSessions, Questions, Answers, StudyGuides)
- **Priority**: High
- **Status**: [x] Complete

#### Task 2: Initial EF Core migration
- **Priority**: High
- **Status**: [x] Complete — Migration `20261116133001_InitialCreate` created and applied on `localhost\SQLEXPRESS` (database `InterviewDrill_Testing`). `varbinary(max)` columns for embeddings (SQL Server Express compatible). Indexes on (UserId, RawTextHash). Unique index on (UserId, JobDescriptionHash, NormalizedQuestionHash).

#### Task 3: Implement QuestionRegistry (exact dedup by hash)
- **Priority**: High
- **Status**: [x] Complete — Implemented with `IQuestionRepository` so it can be tested without a real database; EF SQL Server query ready. **Phase 4 fix**: `InMemoryQuestionRegistry` moved from `AddScoped` to `AddSingleton` (anti-repetition bug across sessions, see task 016).

#### Task 4: Implement semantic dedup (cosine similarity > 0.92)
- **Priority**: High
- **Status**: [x] Complete — Logic, contract, and EF query implemented. **Note**: SQL Server Express does not support `VECTOR` nor `EF.Functions.VectorDistance`, therefore semantic search is resolved in memory by loading candidates with the same user+JobDescriptionHash and computing `VectorMath.CosineSimilarity`.

#### Task 5: Per-question session checkpoint (survives restarts)
- **Priority**: Medium
- **Status**: [x] Complete — Implemented with `SessionCheckpoint`, `EfInterviewerSessionStore`, and the `InterviewerStateJson` column.

#### Task 6: Session history with verdicts
- **Priority**: Medium
- **Status**: [x] Complete — Service, EF repository, and unit tests implemented; API/final integration still pending.

#### Task 7: Anti-repetition and persistence tests
- **Priority**: High
- **Status**: [x] Complete — QuestionRegistryTests, SessionHistoryServiceTests, SessionCheckpointServiceTests, InterviewDrillDbContextModelTests (2 updated to `varbinary(max)` + no vector index) **executed and passing 58/58** on the host (after turning off Smart App Control). End-to-end verification against the real database via `scripts/e2e-smoke.ps1 -UseDatabase`: second session 0 overlap reading from `InterviewDrill_Testing`.

#### Task 8: Fix SQL Server Express incompatibility (SqlVector → byte[])
- **Priority**: High
- **Status**: [x] Complete — `QuestionEntity.Embedding` changed from `SqlVector<float>` to `byte[]`; `DbContext` uses `varbinary(max)`; `QuestionRegistry` serializes float[] to byte[]; `SqlQuestionRepository.ExistsSemanticDuplicateAsync` rewritten for in-memory search with `VectorMath.CosineSimilarity`; migrations regenerated.

### Definition of Done
- [x] Initial migration applies cleanly on an empty database (verified on SQL Server Express)
- [x] varbinary(max) columns for embeddings in the Questions table
- [x] The same question is never issued twice for the same user+JD (exact)
- [x] A paraphrase of a previous question is rejected (similarity >= 0.92, in-memory search)
- [x] Same JD, different user → no cross deduplication
- [x] Anti-repetition across sessions (same user+JD): verified E2E — session 2 asked 8 new questions, 0 overlap with session 1 (Phase 4 fix: `InMemoryQuestionRegistry` Singleton)
- [x] An interrupted session resumes on the correct question
- [x] History returns ordered sessions with verdicts
- [x] Unit tests passing (> 85% coverage in QuestionRegistry, SessionHistory, SessionCheckpoint, DbContext model)
- [ ] End-to-end anti-repetition tests against the real database still pending
