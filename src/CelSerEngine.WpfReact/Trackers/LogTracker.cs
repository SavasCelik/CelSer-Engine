using Microsoft.Extensions.Logging;

namespace CelSerEngine.WpfReact.Trackers;

public record LogItem(DateTime Timestamp, LogLevel Level, string CategoryName, string Message);

public sealed class LogTracker
{
    public event Action<LogItem>? OnLogReceived;

    public void AppendLog(LogItem logItem) => OnLogReceived?.Invoke(logItem);
}
