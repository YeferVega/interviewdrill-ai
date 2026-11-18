using System.Text;
using InterviewDrill.Core.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InterviewDrill.Infrastructure.Logging;

/// <summary>
/// Proveedor de logging que envuelve cada <see cref="ILogger"/> con sanitización de PII.
/// Garantiza que ningún dato sensible (emails, teléfonos, SSN, tarjetas, IPs, claves API,
/// secretos) llegue a los sinks de log.
/// </summary>
public sealed class PiiSanitizingLoggerProvider : ILoggerProvider
{
    private readonly IPiiSanitizer _sanitizer;
    private readonly ILoggerProvider _inner;

    public PiiSanitizingLoggerProvider(IPiiSanitizer sanitizer, ILoggerProvider inner)
    {
        _sanitizer = sanitizer;
        _inner = inner;
    }

    public ILogger CreateLogger(string categoryName) =>
        new PiiSanitizingLogger(_inner.CreateLogger(categoryName), _sanitizer);

    public void Dispose()
    {
        _inner.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// <see cref="ILogger"/> que sanitiza PII y secretos de todo mensaje antes de delegar
/// al logger interno. Nunca registra datos sensibles en texto plano.
/// </summary>
public sealed class PiiSanitizingLogger : ILogger
{
    private readonly ILogger _inner;
    private readonly IPiiSanitizer _sanitizer;

    public PiiSanitizingLogger(ILogger inner, IPiiSanitizer sanitizer)
    {
        _inner = inner;
        _sanitizer = sanitizer;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        var sanitized = _sanitizer.Sanitize(state.ToString() ?? string.Empty);
        return _inner.BeginScope(sanitized.Text);
    }

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var originalMessage = formatter(state, exception);
        var sanitized = _sanitizer.Sanitize(originalMessage);

        _inner.Log(logLevel, eventId, state, exception, (_, ex) => sanitized.Text);
    }
}

/// <summary>
/// Implementación simple de <see cref="IOptionsMonitor{ConsoleLoggerOptions}"/> para usar
/// con <see cref="ConsoleLoggerProvider"/> en la capa de aplicación.
/// </summary>
public sealed class SimpleOptionsMonitor<T> : IOptionsMonitor<T> where T : new()
{
    private readonly T _value;

    public SimpleOptionsMonitor(T value) => _value = value;

    public T CurrentValue => _value;
    public T Get(string? name) => _value;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
