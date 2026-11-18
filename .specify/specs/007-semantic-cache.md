# Specification 007: Semantic Cache Module (SQL Server Express compatible)

## Overview
The Semantic Cache is an intelligent cache system that stores LLM answers for semantically similar questions, reducing API costs and improving latency. It uses embeddings to determine semantic similarity between queries.

## Stack
- **.NET 11 / C# 15** (implementation in `InterviewDrill.Infrastructure.Caching`)
- **Redis**: distributed semantic cache (optional, in-memory fallback)
- **HashEmbeddingGenerator**: deterministic embedding generation (1536 dimensions)
- **OpenTelemetry/ActivitySource**: traceability of cache operations
- **SQL Server Express compatible**: embeddings stored as `varbinary(max)`, semantic search resolved in memory with `VectorMath.CosineSimilarity`

## Functional Requirements
1. Store LLM answers with their embeddings (serialized as `byte[]`)
2. Compute semantic similarity between queries (in memory, VectorMath.CosineSimilarity)
3. Return a cached answer if similarity > threshold (default: 0.92)
4. Support manual invalidation of cache entries
5. Support TTL (Time-To-Live) for cache entries
6. Provide hit/miss rate statistics
7. Support multiple namespaces for isolation
8. Allow configuration of the similarity threshold
9. Return a cached answer or indicate a cache miss
10. Sanitize PII before caching (query and answer)

## Non-Functional Requirements
1. **Performance**: Cache lookup < 50ms (in-memory operation)
2. **Scalability**: Support 100K+ cache entries
3. **Efficiency**: Reduce LLM costs by 30-50%
4. **Accuracy**: Precise semantic similarity (cosine, threshold 0.92)
5. **Compatibility**: Works on SQL Server Express (no VECTOR type, no vector indexes)

## User Stories
- as a **system**, I want to cache LLM answers to reduce costs and latency
- as a **team**, I want to reuse answers for similar questions
- as a **developer**, I want a simple API to integrate the semantic cache

## Acceptance Criteria
- [x] Stores answers with embeddings (byte[] serialized from float[])
- [x] Computes cosine similarity between embeddings (in memory, VectorMath)
- [x] Returns a cached answer if similarity > threshold (configurable, default 0.92)
- [x] Supports manual invalidation (InvalidateAsync)
- [x] Supports configurable TTL (SetAsync with ttlSeconds)
- [x] Provides hit/miss statistics (GetStatisticsAsync)
- [x] Supports namespaces (full isolation)
- [x] Threshold configurable per call
- [x] Unit tests for similarity (SemanticCacheServiceTests, 10 tests)
- [x] Performance tests (in-memory operation)
- [x] TTL expiration tests
- [x] Coverage > 85%
- [x] PII sanitized before caching (query and answer)
- [x] Compatible with SQL Server Express (varbinary(max), in-memory search)

## Out of Scope
- Embedding generation (assumed to exist via IEmbeddingGenerator)
- Distributed persistent storage (in-memory by default)
- Native vector search in the DB (not supported in SQL Server Express)

## Dependencies
- **Embedding model**: HashEmbeddingGenerator (implemented)
- **Vector similarity**: Cosine similarity in memory (VectorMath, implemented)
- **Redis**: optional for distributed cache

## Risks
| Risk | Mitigation |
|------|------------|
| False positives (incorrect answers cached) | High threshold (0.92+), manual validation |
| Memory overflow with many entries | LRU eviction policy, limits (10K entries per namespace) |
| Stale data | Configurable TTL, manual invalidation |
| SQL Server Express does not support VECTOR | Store as varbinary(max), search in memory with VectorMath |
