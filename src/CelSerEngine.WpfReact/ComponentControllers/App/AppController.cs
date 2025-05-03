using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Shared.Services.MemoryScan;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
    private readonly IProgress<float> _progressBarUpdater;
    private float _progressBarValue;
    private CancellationTokenSource? _scanCancellationTokenSource;
    private ScanResultItemsTable _scanResultItemsTable;
    private List<MemorySegment> _trackedItems;

    public AppController(
        ReactJsRuntime reactJsRuntime,
        ILogger<AppController> logger,
        ProcessSelectionTracker processSelectionTracker,
        IMemoryScanService memoryScanService,
        INativeApi nativeApi)
    {
        _reactJsRuntime = reactJsRuntime;
        _logger = logger;
        _processSelectionTracker = processSelectionTracker;
        _memoryScanService = memoryScanService;
        _progressBarUpdater = new Progress<float>(newValue =>
        {
            if (newValue - _progressBarValue >= 1 || (newValue == 0 && newValue != _progressBarValue))
            {
                _progressBarValue = newValue;
                _ = UpdateFrontEndProgressBarAsync();
            }
        });
        _scanResultItemsTable = new ScanResultItemsTable();
        _trackedItems = [];

        _processSelectionTracker.OnChange += UpdateSelectedProcessTextAsync;
        var process = Process.GetProcessesByName("SmallGame").First();
        var selectedProcess = new ProcessAdapter(process);
        _processSelectionTracker.SelectedProcess = selectedProcess;
        selectedProcess.ProcessHandle = nativeApi.OpenProcess(selectedProcess.Process.Id);
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
        _scanResultItemsTable.ScanResultItems = results.Select(x => new MemorySegment(x)).ToList();
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
            _scanResultItemsTable.ScanResultItems.Cast<IMemorySegment>().ToList(),
            scanConstraint,
            _processSelectionTracker.SelectedProcessHandle,
            _progressBarUpdater,
            token);
        sw.Stop();
        _logger.LogInformation("Scan completed found: {count} addresses in: {duration} ms", results.Count, sw.ElapsedMilliseconds);
        _progressBarUpdater.Report(100);
        _scanResultItemsTable.ScanResultItems = results.Select(x => new MemorySegment(x)).ToList();
        _progressBarUpdater.Report(0);
        _scanCancellationTokenSource = null;
    }

    public void NewScan()
    {
        _scanResultItemsTable.ScanResultItems.Clear();
    }

    public async Task CancelScanAsync()
    {
        if (_scanCancellationTokenSource != null)
        {
            await _scanCancellationTokenSource.CancelAsync();
        }
    }

    public object GetScanResultItems(int page, int pageSize)
    {
        var scanResultItems = _scanResultItemsTable.GetScanResultItems(page, pageSize)
            .Select(x => new ScanResultItemReact
            {
                Address = x.Address.ToString("X8"),
                Value = x.Value,
                PreviousValue = x.InitialValue
            });

        return new { Items = scanResultItems, TotalCount = _scanResultItemsTable.ScanResultItems.Count };
    }

    public void AddTrackedItem(string memoryAddressHexString, int pageIndex, int pageSize)
    {
        var visibleScanResultItems = _scanResultItemsTable.GetScanResultItems(pageIndex, pageSize);
        var selectedItem = visibleScanResultItems
            .Where(x => x.Address.ToString("X8") == memoryAddressHexString)
            .FirstOrDefault();

        if (selectedItem != null)
        { 
            _trackedItems.Add(new MemorySegment(selectedItem));
        }
    }

    public object[] GetTrackedItems()
    {
        return _trackedItems.Select(x => new 
        {
            Description = "Description",
            Address = x.Address.ToString("X8"),
            Value = x.Value
        }).ToArray();
    }

    private async Task UpdateFrontEndProgressBarAsync()
    {
        await _reactJsRuntime.InvokeVoidAsync(ComponentId, "updateProgressBar", _progressBarValue);
    }

    private async void UpdateSelectedProcessTextAsync()
    {
        var selectedProcess = _processSelectionTracker.SelectedProcess;
        await _reactJsRuntime.InvokeVoidAsync(ComponentId, "updateSelectedProcessText", selectedProcess?.DisplayString ?? "");
    }

    public void Dispose()
    {
        _processSelectionTracker.OnChange -= UpdateSelectedProcessTextAsync;
    }
}
