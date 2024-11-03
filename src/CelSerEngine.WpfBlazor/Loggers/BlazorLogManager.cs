namespace CelSerEngine.WpfBlazor.Loggers;

public class BlazorLogManager
{
    public event Action<string>? OnLogReceived;

    public void AppendLog(string message) => OnLogReceived?.Invoke(message);
}