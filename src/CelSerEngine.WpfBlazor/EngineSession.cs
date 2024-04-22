using Microsoft.Win32.SafeHandles;

namespace CelSerEngine.WpfBlazor;
public class EngineSession
{
    private ProcessAdapter? _selectedProcess;

    public ProcessAdapter? SelectedProcess { 
        get => _selectedProcess; 
        set 
        {
            _selectedProcess?.Dispose();
            _selectedProcess = value;
        } 
    }
    public SafeProcessHandle SelectedProcessHandle => _selectedProcess?.ProcessHandle ?? new SafeProcessHandle();
}
