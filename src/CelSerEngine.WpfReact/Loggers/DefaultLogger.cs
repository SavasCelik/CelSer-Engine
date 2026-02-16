using CelSerEngine.WpfReact.Trackers;
using Microsoft.Extensions.Logging;

namespace CelSerEngine.WpfReact.Loggers;

public class DefaultLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogTracker _logManager;

    public DefaultLogger(string categoryName, LogTracker logManager)
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
        _logManager.AppendLog(new LogItem(DateTime.Now, logLevel, _categoryName, formatter(state, exception)));
    }
}