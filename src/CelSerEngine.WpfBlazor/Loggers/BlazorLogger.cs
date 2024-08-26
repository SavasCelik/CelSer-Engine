using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = $"{logLevel}: {_categoryName}: {formatter(state, exception)}";
        _logManager.AppendLog(message);
    }
}