using CelSerEngine.WpfReact.Trackers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CelSerEngine.WpfReact.Loggers;

public class DefaultLoggerProvider : ILoggerProvider
{
    private readonly LogTracker _logManager;
    private readonly ConcurrentDictionary<string, DefaultLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public DefaultLoggerProvider(LogTracker logManager) => _logManager = logManager;

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new DefaultLogger(name, _logManager));

    public void Dispose() => _loggers.Clear();
}
