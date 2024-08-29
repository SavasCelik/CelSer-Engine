using Microsoft.Extensions.Logging;

namespace CelSerEngine.WpfBlazor.Loggers;
public class BlazorLogger : ILogger
{
    private readonly string _categoryName;
    private readonly BlazorLogManager _logManager;

    public BlazorLogger(string categoryName, BlazorLogManager logManager)
    {
        _categoryName = categoryName;
        _logManager = logManager;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = $"[{DateTime.Now.ToLongTimeString()} {logLevel}{(logLevel == LogLevel.Debug ? $" {_categoryName}" : "")}] - {formatter(state, exception)}";
        _logManager.AppendLog(message);
    }
}