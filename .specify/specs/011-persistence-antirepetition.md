# Specification 011: Persistence & Anti-Repetition

## Sprint/Iteration: Phase 1 — Persistence & Anti-Repetition

## Overview
Domain model with EF Core for persisting users, JDs, sessions, questions, answers, and study guides. Includes question anti-repetition (exact hash + semantic similarity) and session checkpointing.

## Stack
- **.NET 11 / C# 15** (implemented in `InterviewDrill.Infrastructure.Persistence`)
- **EF Core** with SQL Server (localdb for testing, SQL Server Express/Azure SQL/AWS RDS for production)
- **In-memory fallback** when no connection string is configured
- **`varbinary(max)` columns** for embeddings (SQL Server Express does not support the `VECTOR` type)
- **In-memory semantic search** with `VectorMath.CosineSimilarity` (no DB vector indexes)

## Domain Entities
- `UserEntity`: Id, BrowserToken (unique), JobDescriptions, Sessions
- `JobDescriptionEntity`: Id, UserId, RawTextHash, ParsedProfileJson, Sessions
- `InterviewSessionEntity`: Id, UserId, JobDescriptionHash, Profile, StartedAt, EndedAt, Status, Questions, StudyGuide
- `QuestionEntity`: Id, SessionId, UserId, JobDescriptionHash, NormalizedQuestionHash, Text, Difficulty, SkillTarget, Embedding (varbinary(max)), AskedAt, Answers
- `AnswerEntity`: Id, QuestionId, Text, EvaluationPayloadJson, AnsweredAt
- `StudyGuideEntity`: Id, SessionId, Markdown, CreatedAt

## Key Functionality

### FR1: EF Core entities
- [x] Users, JobDescriptions, InterviewSessions, Questions, Answers, StudyGuides
- [x] Relationships: User → JobDescriptions → InterviewSessions → Questions → Answers → StudyGuides
- [x] `varbinary(max)` columns for embeddings in Questions (SQL Server Express compatible)
- [x] Indexes on (UserId, RawTextHash) for per-user+JD dedup

### FR2: Initial EF Core migration
- [x] Migration `20261116133001_InitialCreate` created and applied
- [x] Indexes on relevant columns for fast queries
- [x] Unique index on (UserId, JobDescriptionHash, NormalizedQuestionHash) for exact dedup
- [x] Index on (SessionId, AskedAt) for chronological order

### FR3: QuestionRegistry (exact dedup by hash)
- [x] Check whether a question (normalized hash) was already emitted for (UserId, RawTextHash)
- [x] Lookup < 100ms P95
- [x] Implemented with `IQuestionRepository` to test without a real DB

### FR4: Semantic dedup (cosine similarity > 0.92)
- [x] Compare candidate question embedding vs. embeddings of previous questions
- [x] Reject if similarity >= 0.92 (VectorMath.CosineSimilarity in memory)
- [x] Regenerate (max. 3 attempts) on collision
- [x] **Note**: SQL Server Express does not support `VECTOR` or `VectorDistance`, therefore semantic search is resolved in memory by loading candidates from the same user+JDHash

### FR5: Session checkpoint per question
- [x] Save session state after each question to resume after a restart
- [x] Implemented with `SessionCheckpoint`, `EfInterviewerSessionStore` and the `InterviewerStateJson` column

### FR6: Session history with verdicts
- [x] Query the user's list of sessions, ordered, with global verdict and associated guide
- [x] Service, EF repository, and unit tests implemented

### FR7: PII sanitization in persistence
- [x] QuestionRepository: sanitizes text before persisting (via InterviewAnswerProcessor)
- [x] InterviewAnswerRepository: receives already-sanitized text
- [x] SessionStore: sanitizes LastSkillTarget and state before serializing
- [x] StudyGuideRepository: sanitizes Markdown before persisting

## Acceptance Criteria
- [x] Initial migration applies cleanly on an empty DB (verified on SQL Server Express localhost\SQLEXPRESS)
- [x] varbinary(max) columns for embeddings in the Questions table
- [x] The same question is never emitted twice for the same user+JD (exact) — unit tests
- [x] A paraphrase of a previous question is rejected (similarity >= 0.92) — unit tests, in-memory search
- [x] Same JD, different user → no cross-user deduplication — unit tests
- [x] An interrupted session resumes at the correct question — EF store implemented
- [x] History returns ordered sessions with verdicts — endpoint/UI/history tests implemented
- [x] Unit tests passing (> 85% coverage)
- [x] PII sanitized before every persistence operation

## Out of Scope
- Native vector search in the DB (not supported on SQL Server Express)
- Distributed persistent storage (in-memory by default)

## Dependencies
- **Spec 006**: PII Sanitizer (persistence integration)
- **Spec 007**: Semantic Cache (embeddings, in-memory search)
- **Spec 009**: JD Parser (RawTextHash for dedup)

## Risks
| Risk | Mitigation |
|------|------------|
| SQL Server Express does not support VECTOR | Store as varbinary(max), search in memory with VectorMath |
| Migration fails on an existing DB | Delete the DB and regenerate the migration from the corrected model |
| Memory overflow with in-memory search | Limit candidates per user+JDHash, pagination in the future |
