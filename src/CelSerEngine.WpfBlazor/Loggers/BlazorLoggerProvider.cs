using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CelSerEngine.WpfBlazor.Loggers;

public class BlazorLoggerProvider : ILoggerProvider
{
    private readonly BlazorLogManager _logManager;
    private readonly ConcurrentDictionary<string, BlazorLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public BlazorLoggerProvider(BlazorLogManager logManager) => _logManager = logManager;

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new BlazorLogger(name, _logManager));

    public void Dispose() => _loggers.Clear();
}