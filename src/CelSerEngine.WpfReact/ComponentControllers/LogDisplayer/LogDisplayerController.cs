using CelSerEngine.WpfReact.Trackers;

namespace CelSerEngine.WpfReact.ComponentControllers.LogDisplayer;

public sealed class LogDisplayerController : ReactControllerBase, IDisposable
{
    private readonly ReactJsRuntime _reactJsRuntime;
    private readonly LogTracker _logTracker;

    public LogDisplayerController(ReactJsRuntime reactJsRuntime, LogTracker logTracker)
    {
        _reactJsRuntime = reactJsRuntime;
        _logTracker = logTracker;

        _logTracker.OnLogReceived += HandleLogReceived;
    }

    private async void HandleLogReceived(LogItem logItem)
    {
        await _reactJsRuntime.InvokeVoidAsync(
            ComponentId,
            "addLogItem",
            new LogItemDto(logItem.Timestamp.ToLongTimeString(), logItem.Level, logItem.CategoryName, logItem.Message));
    }

    public void Dispose()
    {
        _logTracker.OnLogReceived -= HandleLogReceived;
    }
}
