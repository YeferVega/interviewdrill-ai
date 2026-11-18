namespace InterviewDrill.Core.Caching;

/// <summary>
/// Contrato de caché semántico para respuestas de LLM.
/// Permite evitar re-inferencia de preguntas semánticamente similares,
/// reduciendo costos de API y mejorando latencia.
/// </summary>
public interface ISemanticCache
{
    /// <summary>
    /// Busca una respuesta en caché por similitud semántica de la consulta.
    /// </summary>
    Task<CacheResult> GetAsync(
        string query,
        string? namespaceName = null,
        double? similarityThreshold = null,
        CancellationToken ct = default);

    /// <summary>
    /// Almacena una respuesta en caché con su embedding.
    /// </summary>
    Task SetAsync(
        string query,
        string response,
        string? namespaceName = null,
        int? ttlSeconds = null,
        CancellationToken ct = default);

    /// <summary>
    /// Invalida una entrada de caché específica.
    /// </summary>
    Task InvalidateAsync(string query, string? namespaceName = null, CancellationToken ct = default);

    /// <summary>
    /// Obtiene estadísticas de uso del caché (hit/miss rate).
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(string? namespaceName = null, CancellationToken ct = default);
}

/// <summary>
/// Resultado de una consulta al caché semántico.
/// </summary>
public record CacheResult(
    bool Hit,
    string? Response,
    double? Similarity,
    string? CachedAt);

/// <summary>
/// Estadísticas de uso del caché semántico.
/// </summary>
public record CacheStatistics(
    int TotalQueries,
    int Hits,
    int Misses,
    double HitRate,
    int TotalEntries);
