using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using CelSerEngine.Shared.Services.MemoryScan;
using CelSerEngine.WpfReact.ComponentControllers.ScanResultItems;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using static CelSerEngine.Core.Native.Enums;

namespace CelSerEngine.WpfReact.ComponentControllers.App;

public enum MemoryScanFilterOptions
{
    Yes,
    No,
    DontCare
}

public enum MemoryType
{
    Image,
    Private,
    Mapped
}

public class AppController : ReactControllerBase, IDisposable
{
    private readonly ReactJsRuntime _reactJsRuntime;
    private readonly ILogger<AppController> _logger;
    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly IMemoryScanService _memoryScanService;
    private readonly INativeApi _nativeApi;
    private readonly MainWindow _mainWindow;
    private readonly IProgress<float> _progressBarUpdater;
    private float _progressBarValue;
    private CancellationTokenSource? _scanCancellationTokenSource;
    private IList<ModuleInfo> _modules;

    [InjectComponent]
    public ScanResultItemsController ScanResultItemsController { get; set; } = default!;

    public AppController(
        ReactJsRuntime reactJsRuntime,
        ILogger<AppController> logger,
        ProcessSelectionTracker processSelectionTracker,
        IMemoryScanService memoryScanService,
        INativeApi nativeApi,
        MainWindow mainWindow)
    {
        _reactJsRuntime = reactJsRuntime;
        _logger = logger;
        _processSelectionTracker = processSelectionTracker;
        _memoryScanService = memoryScanService;
        _nativeApi = nativeApi;
        _mainWindow = mainWindow;
        _progressBarUpdater = new Progress<float>(newValue =>
        {
            if (newValue - _progressBarValue >= 1 || (newValue == 0 && newValue != _progressBarValue))
            {
                _progressBarValue = newValue;
                _ = UpdateFrontEndProgressBarAsync();
            }
        });
        _modules = [];

        _processSelectionTracker.OnChange += SelectedProcessChanged;
    }

    public override void OnComponentRegisteredMethods()
    {
        var process = Process.GetProcessesByName("SmallGame").First();
        var selectedProcess = new ProcessAdapter(process);
        _processSelectionTracker.SelectedProcess = selectedProcess;
        selectedProcess.ProcessHandle = _nativeApi.OpenProcess(selectedProcess.Process.Id);


        var psNative = new PsNative(_nativeApi);
        var sw = Stopwatch.StartNew();
        psNative.Start(selectedProcess.ProcessHandle, 0x2381AFF7AF8, 5, 8096);
        sw.Stop();
        
    }

    public async Task FirstScanAsync(MemoryScanSettings memoryScanSettings)
    {
        var userInput = memoryScanSettings.ScanValue;

        if (memoryScanSettings.ScanCompareType == ScanCompareType.ValueBetween)
        {
            userInput = $"{memoryScanSettings.FromValue}-{memoryScanSettings.ToValue}";
        }

        var scanConstraint = new ScanConstraint(memoryScanSettings.ScanCompareType, memoryScanSettings.ScanValueType, userInput)
        {
            StartAddress = IntPtr.Parse(memoryScanSettings.StartAddress, System.Globalization.NumberStyles.HexNumber),
            StopAddress = IntPtr.Parse(memoryScanSettings.StopAddress, System.Globalization.NumberStyles.HexNumber)
        };

        foreach (var memoryType in memoryScanSettings.MemoryTypes)
        {
            switch (memoryType)
            {
                case MemoryType.Private:
                    scanConstraint.AllowedMemoryTypes.Add(MEMORY_TYPE.MEM_PRIVATE);
                    break;
                case MemoryType.Image:
                    scanConstraint.AllowedMemoryTypes.Add(MEMORY_TYPE.MEM_IMAGE);
                    break;
                case MemoryType.Mapped:
                    scanConstraint.AllowedMemoryTypes.Add(MEMORY_TYPE.MEM_MAPPED);
                    break;
            }
        }

        if (memoryScanSettings.Writable == MemoryScanFilterOptions.Yes)
        {
            scanConstraint.IncludedProtections |= MemoryProtections.Writable;
        }
        if (memoryScanSettings.Executable == MemoryScanFilterOptions.Yes)
        {
            scanConstraint.IncludedProtections |= MemoryProtections.Executable;
        }
        if (memoryScanSettings.CopyOnWrite == MemoryScanFilterOptions.Yes)
        {
            scanConstraint.IncludedProtections |= MemoryProtections.CopyOnWrite;
        }

        if (memoryScanSettings.Writable == MemoryScanFilterOptions.No)
        {
            scanConstraint.ExcludedProtections |= MemoryProtections.Writable;
        }
        if (memoryScanSettings.Executable == MemoryScanFilterOptions.No)
        {
            scanConstraint.ExcludedProtections |= MemoryProtections.Executable;
        }
        if (memoryScanSettings.CopyOnWrite == MemoryScanFilterOptions.No)
        {
            scanConstraint.ExcludedProtections |= MemoryProtections.CopyOnWrite;
        }

        _scanCancellationTokenSource = new CancellationTokenSource();
        var token = _scanCancellationTokenSource.Token;
        _logger.LogInformation("First scan started...");
        var sw = Stopwatch.StartNew();
        var results = await _memoryScanService.ScanProcessMemoryAsync(scanConstraint, _processSelectionTracker.SelectedProcessHandle, _progressBarUpdater, token);
        sw.Stop();
        _logger.LogInformation("Scan completed found: {count} addresses in: {duration} ms", results.Count, sw.ElapsedMilliseconds);
        _progressBarUpdater.Report(100);
        ScanResultItemsController.ScanResultItems = results.Select(x => new MemorySegment(x)).ToList();
        _progressBarUpdater.Report(0);
        _scanCancellationTokenSource = null;
    }

    public async Task NextScanAsync(MemoryScanSettings memoryScanSettings)
    {
        var userInput = memoryScanSettings.ScanValue;

        if (memoryScanSettings.ScanCompareType == ScanCompareType.ValueBetween)
        {
            userInput = $"{memoryScanSettings.FromValue}-{memoryScanSettings.ToValue}";
        }

        _scanCancellationTokenSource = new CancellationTokenSource();
        var token = _scanCancellationTokenSource.Token;
        _logger.LogInformation("Next scan started...");
        var sw = Stopwatch.StartNew();
        var scanConstraint = new ScanConstraint(memoryScanSettings.ScanCompareType, memoryScanSettings.ScanValueType, userInput);
        var results = await _memoryScanService.FilterMemorySegmentsByScanConstraintAsync(
            ScanResultItemsController.ScanResultItems.Cast<IMemorySegment>().ToList(),
            scanConstraint,
            _processSelectionTracker.SelectedProcessHandle,
            _progressBarUpdater,
            token);
        sw.Stop();
        _logger.LogInformation("Scan completed found: {count} addresses in: {duration} ms", results.Count, sw.ElapsedMilliseconds);
        _progressBarUpdater.Report(100);
        ScanResultItemsController.ScanResultItems = results.Select(x => new MemorySegment(x)).ToList();
        _progressBarUpdater.Report(0);
        _scanCancellationTokenSource = null;
    }

    public void NewScan()
    {
        ScanResultItemsController.ScanResultItems.Clear();
    }

    public async Task CancelScanAsync()
    {
        if (_scanCancellationTokenSource != null)
        {
            await _scanCancellationTokenSource.CancelAsync();
        }
    }

    public void OpenProcessSelector()
    {
        _mainWindow.OpenProcessSelector();
    }

    private async Task UpdateFrontEndProgressBarAsync()
    {
        await _reactJsRuntime.InvokeVoidAsync(ComponentId, "updateProgressBar", _progressBarValue);
    }

    private async void SelectedProcessChanged()
    {
        await UpdateSelectedProcessText();
        UpdateModules();
    }

    private async Task UpdateSelectedProcessText()
    {
        var selectedProcess = _processSelectionTracker.SelectedProcess;
        await _reactJsRuntime.InvokeVoidAsync(ComponentId, "updateSelectedProcessText", selectedProcess?.DisplayString ?? "");
    }

    private void UpdateModules()
    {
        _modules = _nativeApi.GetProcessModules(_processSelectionTracker.SelectedProcessHandle);
        var moduleNames = _modules.Select(x => Path.GetFileName(x.Name)).ToList();
    }

    public void Dispose()
    {
        _processSelectionTracker.OnChange -= SelectedProcessChanged;
    }
}
