using System.Diagnostics;
using InterviewDrill.Core.Observability;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Caching;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Infrastructure.Caching;

/// <summary>
/// Implementación de caché semántico en memoria con soporte para namespaces y TTL.
/// Usa embeddings generados por IEmbeddingGenerator y similitud coseno para
/// encontrar respuestas previamente cacheadas para consultas similares.
/// </summary>
public sealed class SemanticCacheService : ISemanticCache
{
    private static readonly ActivitySource Activity = ActivitySourceProvider.LlmActivitySource;

    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IPiiSanitizer _piiSanitizer;
    private readonly Dictionary<string, List<CacheEntry>> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    private int _totalQueries;
    private int _hits;
    private int _misses;

    public SemanticCacheService(IEmbeddingGenerator embeddingGenerator, IPiiSanitizer piiSanitizer)
    {
        _embeddingGenerator = embeddingGenerator;
        _piiSanitizer = piiSanitizer;
    }

    public async Task<CacheResult> GetAsync(
        string query,
        string? namespaceName = null,
        double? similarityThreshold = null,
        CancellationToken ct = default)
    {
        namespaceName ??= "default";
        similarityThreshold ??= 0.92;

        using var activity = Activity.StartActivity("SemanticCache.GetAsync", ActivityKind.Client);
        activity?.SetTag("cache.namespace", namespaceName);
        activity?.SetTag("cache.threshold", similarityThreshold.Value);
        activity?.SetTag("cache.operation", "get");

        lock (_lock)
        {
            _totalQueries++;
        }

        // Sanitizar consulta antes de procesar
        var sanitizedQuery = _piiSanitizer.Sanitize(query).Text;

        var queryEmbedding = await _embeddingGenerator.GenerateAsync(sanitizedQuery, ct);

        lock (_lock)
        {
            if (!_entries.TryGetValue(namespaceName, out var entries) || entries.Count == 0)
            {
                _misses++;
                activity?.SetTag("cache.result", "miss");
                activity?.Stop();
                return new CacheResult(false, null, null, null);
            }

            var bestMatch = entries
                .Where(e => !e.IsExpired)
                .Select(e => new
                {
                    Entry = e,
                    Similarity = CosineSimilarity(queryEmbedding, e.Embedding)
                })
                .Where(x => x.Similarity >= similarityThreshold.Value)
                .OrderByDescending(x => x.Similarity)
                .FirstOrDefault();

            if (bestMatch is not null)
            {
                _hits++;
                bestMatch.Entry.LastAccessedAt = DateTimeOffset.UtcNow;
                bestMatch.Entry.AccessCount++;

                activity?.SetTag("cache.result", "hit");
                activity?.SetTag("cache.similarity", bestMatch.Similarity);
                activity?.Stop();

                return new CacheResult(true, bestMatch.Entry.Response, bestMatch.Similarity, bestMatch.Entry.CreatedAt.ToString("o"));
            }

            _misses++;
            activity?.SetTag("cache.result", "miss");
            activity?.Stop();

            return new CacheResult(false, null, null, null);
        }
    }

    public async Task SetAsync(
        string query,
        string response,
        string? namespaceName = null,
        int? ttlSeconds = null,
        CancellationToken ct = default)
    {
        namespaceName ??= "default";

        using var activity = Activity.StartActivity("SemanticCache.SetAsync", ActivityKind.Client);
        activity?.SetTag("cache.namespace", namespaceName);
        activity?.SetTag("cache.operation", "set");

        // Sanitizar respuesta antes de cachear
        var sanitizedResponse = _piiSanitizer.Sanitize(response).Text;

        // Sanitizar query antes de generar el embedding (consistente con GetAsync)
        var sanitizedQuery = _piiSanitizer.Sanitize(query).Text;
        var queryEmbedding = await _embeddingGenerator.GenerateAsync(sanitizedQuery, ct);

        var entry = new CacheEntry
        {
            Query = sanitizedQuery,
            Response = sanitizedResponse,
            Embedding = queryEmbedding,
            Namespace = namespaceName,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = ttlSeconds.HasValue ? DateTimeOffset.UtcNow.AddSeconds(ttlSeconds.Value) : null,
            LastAccessedAt = DateTimeOffset.UtcNow,
            AccessCount = 1
        };

        lock (_lock)
        {
            if (!_entries.ContainsKey(namespaceName))
            {
                _entries[namespaceName] = [];
            }

            _entries[namespaceName].Add(entry);

            // Aplicar límite máximo de entradas por namespace (LRU simple)
            const int MaxEntriesPerNamespace = 10000;
            if (_entries[namespaceName].Count > MaxEntriesPerNamespace)
            {
                _entries[namespaceName] = _entries[namespaceName]
                    .OrderByDescending(e => e.LastAccessedAt)
                    .Take(MaxEntriesPerNamespace)
                    .ToList();
            }
        }

        activity?.SetTag("cache.result", "stored");
        activity?.Stop();
    }

    public Task InvalidateAsync(string query, string? namespaceName = null, CancellationToken ct = default)
    {
        namespaceName ??= "default";

        lock (_lock)
        {
            if (_entries.TryGetValue(namespaceName, out var entries))
            {
                _entries[namespaceName] = entries
                    .Where(e => !string.Equals(e.Query, query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        return Task.CompletedTask;
    }

    public Task<CacheStatistics> GetStatisticsAsync(string? namespaceName = null, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var totalQueries = _totalQueries;
            var hits = _hits;
            var misses = _misses;
            var totalEntries = _entries.Values.Sum(list => list.Count(e => !e.IsExpired));

            var hitRate = totalQueries > 0 ? (double)hits / totalQueries : 0;

            return Task.FromResult(new CacheStatistics(
                TotalQueries: totalQueries,
                Hits: hits,
                Misses: misses,
                HitRate: Math.Round(hitRate, 4),
                TotalEntries: totalEntries));
        }
    }

    private static double CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            return 0;

        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        for (var i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private sealed class CacheEntry
    {
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string Namespace { get; set; } = "default";
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset LastAccessedAt { get; set; }
        public int AccessCount { get; set; }

        public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value;
    }
}
