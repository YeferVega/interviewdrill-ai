using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace InterviewDrill.Infrastructure.Resilience;

/// <summary>
/// Políticas de resiliencia para clientes LLM basadas en Polly.
/// Incluye retry con backoff exponencial, manejo de HTTP 429 (rate limiting),
/// circuit breaker y timeout de solicitud HTTP.
/// </summary>
public static class PollyPolicies
{
    /// <summary>
    /// Número máximo de intentos de reintento.
    /// </summary>
    public const int MaxRetryAttempts = 3;

    /// <summary>
    /// Tiempo de espera inicial entre reintentos (segundos).
    /// </summary>
    public const double InitialBackoffSeconds = 2.0;

    /// <summary>
    /// Tiempo máximo de espera entre reintentos (segundos).
    /// </summary>
    public const double MaxBackoffSeconds = 8.0;

    /// <summary>
    /// Umbral de fallos consecutivos para abrir el circuit breaker.
    /// </summary>
    public const int CircuitBreakerFailureThreshold = 5;

    /// <summary>
    /// Duración de apertura del circuit breaker (segundos).
    /// </summary>
    public const int CircuitBreakerDurationSeconds = 30;

    /// <summary>
    /// Timeout de solicitud HTTP en segundos.
    /// </summary>
    public const int HttpTimeoutSeconds = 30;

    /// <summary>
    /// Crea una política de retry con backoff exponencial para HTTP 429 y fallos transitorios.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outcome, timespan, retryCount, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode ?? HttpStatusCode.InternalServerError;
                    var retryAfter = outcome.Result?.Headers.RetryAfter?.Delta ?? timespan;
                    System.Diagnostics.Debug.WriteLine(
                        $"[Polly] Retry {retryCount}/{MaxRetryAttempts} after {timespan.TotalSeconds}s. Status: {statusCode}. Retry-After: {retryAfter.TotalSeconds}s");
                });
    }

    /// <summary>
    /// Crea una política de circuit breaker para evitar cascada de fallos cuando el LLM está caído.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: CircuitBreakerFailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(CircuitBreakerDurationSeconds),
                onBreak: (outcome, timespan, context) =>
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Polly] Circuit breaker opened for {timespan.TotalSeconds}s. Last status: {outcome.Result?.StatusCode}");
                },
                onReset: context =>
                {
                    System.Diagnostics.Debug.WriteLine("[Polly] Circuit breaker reset. Resuming normal operation.");
                });
    }

    /// <summary>
    /// Crea una política de timeout para evitar bloqueos indefinidos en solicitudes HTTP.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(HttpTimeoutSeconds));
    }

    /// <summary>
    /// Combina todas las políticas de resiliencia en una sola política envuelta.
    /// Orden de aplicación: Timeout -> Circuit Breaker -> Retry.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetResilientPolicy()
    {
        return Policy.WrapAsync(GetRetryPolicy(), GetCircuitBreakerPolicy(), GetTimeoutPolicy());
    }
}
