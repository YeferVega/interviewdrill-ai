using System.Text;
using InterviewDrill.Core.Security;
using InterviewDrill.Infrastructure.Logging;
using InterviewDrill.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace InterviewDrill.Infrastructure.Tests.Logging;

public sealed class PiiSanitizingLoggerProviderTests
{
    private static readonly RegexPiiSanitizer Sanitizer = new();

    [Fact]
    public void CreateLogger_ReturnsPiiSanitizingLogger()
    {
        var inner = new CapturingLoggerProvider();
        using var provider = new PiiSanitizingLoggerProvider(Sanitizer, inner);

        var logger = provider.CreateLogger("cat");

        Assert.IsType<PiiSanitizingLogger>(logger);
    }

    [Fact]
    public void Dispose_DisposesInnerProvider()
    {
        var inner = new CapturingLoggerProvider();
        var provider = new PiiSanitizingLoggerProvider(Sanitizer, inner);

        provider.Dispose();

        Assert.True(inner.Disposed);
    }
}

public sealed class PiiSanitizingLoggerTests
{
    private static readonly IPiiSanitizer Sanitizer = new RegexPiiSanitizer();

    [Fact]
    public void Log_SanitizesEmailBeforeDelegating()
    {
        var inner = new CapturingLogger();
        var logger = new PiiSanitizingLogger(inner, Sanitizer);

        logger.Log(LogLevel.Information, default, "Contacto alice@example.com listo", null, (s, _) => s);

        Assert.Contains("[EMAIL]", inner.Messages[0]);
        Assert.DoesNotContain("alice@example.com", inner.Messages[0]);
    }

    [Fact]
    public void Log_SanitizesMultiplePiiTypes()
    {
        var inner = new CapturingLogger();
        var logger = new PiiSanitizingLogger(inner, Sanitizer);

        var msg = "Email bob@x.com, phone +1 555-123-4567, ip 192.168.1.10";
        logger.Log(LogLevel.Warning, default, msg, null, (s, _) => s);

        var written = inner.Messages[0];
        Assert.Contains("[EMAIL]", written);
        Assert.Contains("[PHONE]", written);
        Assert.Contains("[IP]", written);
    }

    [Fact]
    public void Log_SanitizesFormatterResultEvenWhenStateIsComplex()
    {
        var inner = new CapturingLogger();
        var logger = new PiiSanitizingLogger(inner, Sanitizer);

        var state = new { Payload = "ok" };
        logger.Log(
            LogLevel.Information,
            default,
            state,
            null,
            (_, _) => "El cliente carlos@example.com reportó el fallo");

        Assert.Contains("[EMAIL]", inner.Messages[0]);
    }

    [Fact]
    public void BeginScope_SanitizesStateBeforeDelegating()
    {
        var inner = new CapturingLogger();
        var logger = new PiiSanitizingLogger(inner, Sanitizer);

        using (logger.BeginScope("session alice@example.com"))
        {
        }

        Assert.Contains("[EMAIL]", inner.ScopedStates[0]);
    }

    [Fact]
    public void IsEnabled_DelegatesToInnerLogger()
    {
        var inner = new CapturingLogger { MinLevel = LogLevel.Warning };
        var logger = new PiiSanitizingLogger(inner, Sanitizer);

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
    }

    [Fact]
    public void Log_WhenLevelDisabled_DoesNotWrite()
    {
        var inner = new CapturingLogger { MinLevel = LogLevel.Critical };
        var logger = new PiiSanitizingLogger(inner, Sanitizer);

        logger.Log(LogLevel.Information, default, "x", null, (s, _) => s);

        Assert.Empty(inner.Messages);
    }
}

public sealed class SimpleOptionsMonitorTests
{
    private sealed class TestOptions
    {
        public int Counter { get; set; }
    }

    [Fact]
    public void CurrentValue_ReturnsProvidedInstance()
    {
        var value = new TestOptions { Counter = 7 };
        var monitor = new SimpleOptionsMonitor<TestOptions>(value);

        Assert.Same(value, monitor.CurrentValue);
        Assert.Equal(7, monitor.CurrentValue.Counter);
    }

    [Fact]
    public void Get_IgnoresName_AndReturnsSameValue()
    {
        var value = new TestOptions { Counter = 1 };
        var monitor = new SimpleOptionsMonitor<TestOptions>(value);

        Assert.Same(value, monitor.Get("whatever"));
        Assert.Same(value, monitor.Get(null));
    }

    [Fact]
    public void OnChange_ReturnsNull()
    {
        var monitor = new SimpleOptionsMonitor<TestOptions>(new TestOptions());

        Assert.Null(monitor.OnChange((_, _) => { }));
    }
}

internal sealed class CapturingLogger : ILogger
{
    public List<string> Messages { get; } = new();
    public List<string> ScopedStates { get; } = new();
    public LogLevel MinLevel { get; init; } = LogLevel.Trace;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        ScopedStates.Add(state?.ToString() ?? string.Empty);
        return new NoopScope();
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= MinLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public void Dispose() { }
    }
}

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public bool Disposed { get; private set; }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger();

    public void Dispose() => Disposed = true;
}
