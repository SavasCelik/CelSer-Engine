using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace CelSerEngine.WpfBlazor;
public class EngineSession
{
    private readonly ILogger<EngineSession> _logger;
    private ProcessAdapter? _selectedProcess;

    public ProcessAdapter? SelectedProcess { 
        get => _selectedProcess; 
        set 
        {
            _selectedProcess?.Dispose();
            _selectedProcess = value;
            _logger.LogInformation("Selected process changed to {processName}", value?.DisplayString);
            NotifyStateChanged();
        } 
    }
    public SafeProcessHandle SelectedProcessHandle => _selectedProcess?.ProcessHandle ?? new SafeProcessHandle();

    public event Action? OnChange;

    public EngineSession(ILogger<EngineSession> logger)
    {
        _logger = logger;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
