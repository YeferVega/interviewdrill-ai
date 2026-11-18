# Task List: 007-semantic-cache

## Sprint/Iteration: Phase 1 — Persistence & JD Parser

### Tasks

#### Task 1: Implement SemanticCacheService in InterviewDrill.Infrastructure
- **Priority**: High
- **Status**: [x] Complete — `SemanticCacheService` implemented with in-memory fallback, namespaces, TTL, hit/miss statistics, PII sanitization before caching. Compatible with SQL Server Express (embeddings as byte[]).

#### Task 2: Implement HNSW vector search with EF Core
- **Priority**: High
- **Status**: [~] In progress — EF Core migrations created with varbinary(max) columns; HNSW search via VectorMath.CosineSimilarity in memory (not natively supported in SQL Server Express). Cache implementation uses in-memory cosine similarity.

#### Task 3: Implement configurable similarity threshold (default 0.92)
- **Priority**: High
- **Status**: [x] Complete — Threshold 0.92 by default, configurable per call in `SemanticCacheService.GetAsync()`.

#### Task 4: Implement TTL and manual invalidation
- **Priority**: Medium
- **Status**: [x] Complete — Optional TTL in `SetAsync()`, `InvalidateAsync()` for specific entries.

#### Task 5: Implement hit/miss rate statistics
- **Priority**: Medium
- **Status**: [x] Complete — `GetStatisticsAsync()` returns TotalQueries, Hits, Misses, HitRate, TotalEntries.

#### Task 6: Implement namespaces for isolation
- **Priority**: Medium
- **Status**: [x] Complete — Default namespace "default", complete isolation between namespaces.

#### Task 7: Unit tests for the Semantic Cache
- **Priority**: High
- **Status**: [x] Complete — `SemanticCacheServiceTests.cs` created with 10 unit tests passing (miss, hit, threshold, TTL, invalidation, namespaces, statistics, PII).

#### Task 8: Fix SQL Server Express incompatibility (vector → varbinary)
- **Priority**: High
- **Status**: [x] Complete — `QuestionEntity.Embedding` changed from `SqlVector<float>` to `byte[]`; `DbContext` uses `varbinary(max)`; migrations regenerated (`20261116133001_InitialCreate`); `InterviewDrill_Testing` database created on `localhost\SQLEXPRESS`.

### Definition of Done
- [x] Stores answers with embeddings (byte[] serialized from float[])
- [x] Computes cosine similarity between embeddings (in memory, VectorMath)
- [x] Returns cached answer when similarity > 0.92 (configurable)
- [x] Supports manual invalidation and TTL
- [x] Provides hit/miss statistics
- [x] Supports namespaces
- [x] Unit tests passing (10 tests in SemanticCacheServiceTests.cs)
- [x] Cache lookup < 50ms P95 (in-memory operation)
- [x] Compatible with SQL Server Express (no VECTOR type, no vector indexes)
