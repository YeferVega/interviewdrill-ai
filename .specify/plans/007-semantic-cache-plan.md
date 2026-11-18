# Technical Plan: Semantic Cache Module (.NET 11)

## Architecture Overview
The Semantic Cache is an intelligent caching system that stores LLM responses for semantically similar questions, reducing API costs and improving latency. Implemented in **C# / .NET 11** within `InterviewDrill.Infrastructure`. Uses Redis when available, in-memory fallback when not.

```
┌─────────────────────────────────────────────────────────┐
│              Semantic Cache Pipeline                    │
├─────────────────────────────────────────────────────────┤
│ 1. Query Embedding Generation                           │
│ 2. Similarity Search (HNSW / cosine similarity)         │
│ 3. Threshold Check (default 0.92)                       │
│ 4. Cache Hit: Return cached response                    │
│ 5. Cache Miss: Forward to LLM, store result             │
│ 6. TTL Management & Eviction                            │
└─────────────────────────────────────────────────────────┘
```

## Components

### 1. ISemanticCache (`InterviewDrill.Core.Caching/ISemanticCache.cs`)
**Responsibility:** Semantic cache interface in Core.
- `Get(query, namespace?, threshold?, ttl?)` → `CacheResult`
- `Set(query, response, namespace?, ttl?)`
- `Invalidate(query, namespace?)`
- `GetStatistics(namespace?)`

### 2. SemanticCacheService (`InterviewDrill.Infrastructure.Caching/SemanticCacheService.cs`)
**Responsibility:** Concrete implementation.
- Redis as distributed cache when available
- In-memory fallback when there is no connection string
- HNSW vector search with EF Core `vector(1536)`

### 3. IEmbeddingGenerator (`InterviewDrill.Core.Caching/IEmbeddingGenerator.cs`)
**Responsibility:** Interface for embedding generation.
- `HashEmbeddingGenerator` as the default implementation (without heavy external dependencies)

## Data Flow

```
Query ──> EmbeddingGenerator ──> VectorStore.Search()
                                           │
                                   ¿Similitud > threshold?
                                   ┌───────┴───────┐
                                   Sí              No
                                   │               │
                                   v               v
                         Cache Hit         Forward to LLM
                         (return cached)   (store result in cache)
```

## API Design (C#)

```csharp
// Interface en Core
public interface ISemanticCache
{
    Task<CacheResult> Get(string query, string? namespace = null, double? threshold = null, int? ttlSeconds = null, CancellationToken ct = default);
    Task Set(string query, string response, string? namespace = null, int? ttlSeconds = null, CancellationToken ct = default);
    Task Invalidate(string query, string? namespace = null, CancellationToken ct = default);
    Task<CacheStatistics> GetStatistics(string? namespace = null, CancellationToken ct = default);
}

public record CacheResult(bool Hit, string? Response, double? Similarity, string? CachedAt);
public record CacheStatistics(int TotalQueries, int Hits, int Misses, double HitRate, int TotalEntries);
```

## Technology Choices

| Technology | Rationale |
|------------|-----------|
| **C# 15 / .NET 11** | Authoritative stack |
| **Redis** | Distributed cache (optional, with in-memory fallback) |
| **EF Core + SQL Server vector(1536)** | Embeddings persistence with HNSW search |
| **HashEmbeddingGenerator** | Embeddings without heavy external dependencies (fallback) |
| **xUnit** | Testing framework |

## File Structure (.NET)

```
backend/
├── src/
│   ├── InterviewDrill.Core.Caching/
│   │   ├── ISemanticCache.cs
│   │   ├── IEmbeddingGenerator.cs
│   │   ├── CacheResult.cs
│   │   └── CacheStatistics.cs
│   └── InterviewDrill.Infrastructure.Caching/
│       ├── SemanticCacheService.cs
│       └── HashEmbeddingGenerator.cs
└── tests/
    └── InterviewDrill.Infrastructure.Tests/
        └── (semantic cache tests - pendientes)
```

## Implementation Status

| Task | Status | Evidence |
|------|--------|----------|
| ISemanticCache interface | ❌ Not started | Does not exist in Core |
| SemanticCacheService | ❌ Not started | Does not exist in Infrastructure |
| Redis integration | ❌ Not started | Not configured |
| HNSW vector search | ❌ Not started | EF Core migrations with vector(1536) created but not applied |
| Threshold 0.92 configurable | ❌ Not started | Not implemented |
| TTL and invalidation | ❌ Not started | Not implemented |
| Hit/miss statistics | ❌ Not started | Not implemented |
| Namespaces | ❌ Not started | Not implemented |
| Unit tests | ❌ Not started | All 7 tasks in `[ ] Not started` |
| Search < 50ms P95 | ❌ Not started | Not implemented |

## Testing Strategy

### Unit Tests
- **SimilarityCalculator**: Cosine similarity, edge cases
- **VectorStore**: Search, insert, delete
- **EvictionPolicy**: LRU, TTL expiration
- **CacheStatistics**: Metrics tracking

### Integration Tests
- Cache hit scenario
- Cache miss scenario
- TTL expiration
- Namespace isolation
- Concurrent access

### Performance Tests
- Latency < 50ms for search
- Throughput with 10K+ entries
- Memory usage tracking

## Risks

| Risk | Mitigation |
|------|------------|
| False positives (incorrect responses cached) | High threshold (0.92+), manual validation |
| Memory overflow with many entries | LRU eviction policy, size limits in Redis |
| Stale data | Configurable TTL, manual invalidation |
| Redis unavailable | Transparent in-memory fallback |
