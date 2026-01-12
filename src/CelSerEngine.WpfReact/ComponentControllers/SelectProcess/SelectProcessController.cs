using CelSerEngine.Core.Native;
using System.Diagnostics;

namespace CelSerEngine.WpfReact.ComponentControllers.SelectProcess;

public class SelectProcessController : ReactControllerBase
{
    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly INativeApi _nativeApi;
    private readonly MainWindow _mainWindow;

    public SelectProcessController(ProcessSelectionTracker processSelectionTracker, INativeApi nativeApi, MainWindow mainWindow)
    {
        _processSelectionTracker = processSelectionTracker;
        _nativeApi = nativeApi;
        _mainWindow = mainWindow;
    }

    public ProcessDto[] GetProcesses()
    {
        var processes = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .Select(p => new ProcessAdapter(p))
            .Where(pa => pa.MainModule != null)
            .Select(pa => new ProcessDto
            {
                DisplayText = pa.DisplayString,
                IconBase64Source = pa.IconBase64Source ?? string.Empty,
                ProcessId = pa.Process.Id
            })
            .ToArray();

        return processes;
    }

    public void SetSelectedProcessById(int processId)
    {
        var process = Process.GetProcessById(processId);
        _processSelectionTracker.SelectedProcess = new ProcessAdapter(process)
        {
            ProcessHandle = _nativeApi.OpenProcess(processId)
        };

        _mainWindow.CloseProcessSelector();
    }
}
