using Microsoft.Extensions.Logging;

namespace OmniEurope.Blazor.Tests;

/// <summary>A logger that hands every entry, at every level, to <paramref name="record"/>.</summary>
internal sealed class CallbackLogger(Action<LogLevel, string, Exception?> record) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        record(logLevel, formatter(state, exception), exception);
}
