using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;

namespace CelSerEngine.WpfReact.ComponentControllers.PointerScanner;

public class PointerScannerController : ReactControllerBase, IDisposable
{
    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly TrackedItemNotifier _trackedItemNotifier;
    private readonly PointerScannerWindow _pointerScannerWindow;
    private readonly INativeApi _nativeApi;
    private readonly ILogger<PointerScannerController> _logger;
    private readonly List<Pointer> _pointerScanResults;
    private CancellationTokenSource? _scanCancellationTokenSource;
    private readonly bool _useFileStorage;
    private PointerScanResultReader? _pointerScanResultReader;

    public PointerScannerController(
        INativeApi nativeApi,
        ILogger<PointerScannerController> logger,
        ProcessSelectionTracker processSelectionTracker,
        TrackedItemNotifier trackedItemNotifier,
        PointerScannerWindow pointerScannerWindow)
    {
        _nativeApi = nativeApi;
        _logger = logger;
        _pointerScanResults = [];
        _processSelectionTracker = processSelectionTracker;
        _trackedItemNotifier = trackedItemNotifier;
        _pointerScannerWindow = pointerScannerWindow;
        _useFileStorage = true;
    }

    public async Task<string> SelectStorage()
    {
        if (_useFileStorage)
        {
            var fileName = "";
            await _pointerScannerWindow.Dispatcher.InvokeAsync(() =>
            {

                var saveFileDlg = new SaveFileDialog
                {
                    DefaultExt = PointerScanner2.PointerListExtName, // Default file extension
                    Filter = $"Pointer List|*{PointerScanner2.PointerListExtName}" // Filter files by extension
                };

                var result = saveFileDlg.ShowDialog();
                if (result == true)
                {
                    fileName = saveFileDlg.FileName;
                }
            });

            return fileName;
        }

        return "memory";
    }

    public async Task StartPointerScan(PointerScanOptionsDto pointerScanOptionsDto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pointerScanOptionsDto.StoragePath);

        if (!IntPtr.TryParse(pointerScanOptionsDto.ScanAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var searchedAddress))
        {
            throw new ArgumentException("Invalid scan address format. Please provide a valid hexadecimal address.");
        }

        _scanCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _scanCancellationTokenSource.Token;
        var scanOptions = new PointerScanOptions
        {
            SearchedAddress = searchedAddress,
            MaxOffset = pointerScanOptionsDto.MaxOffset,
            MaxLevel = pointerScanOptionsDto.MaxLevel,
            RequireAlignedPointers = pointerScanOptionsDto.RequireAlignedPointers,
            MaxParallelWorkers = pointerScanOptionsDto.MaxParallelWorkers,
            LimitToMaxOffsetsPerNode = pointerScanOptionsDto.LimitToMaxOffsetsPerNode,
            MaxOffsetsPerNode = pointerScanOptionsDto.MaxOffsetsPerNode,
            PreventLoops = pointerScanOptionsDto.PreventLoops,
            AllowThreadStacksAsStatic = pointerScanOptionsDto.AllowThreadStacksAsStatic,
            ThreadStacks = pointerScanOptionsDto.ThreadStacks,
            StackSize = pointerScanOptionsDto.StackSize,
            AllowReadOnlyPointers = pointerScanOptionsDto.AllowReadOnlyPointers,
            OnlyOneStaticInPath = pointerScanOptionsDto.OnlyOneStaticInPath,
            OnlyResidentMemory = pointerScanOptionsDto.OnlyResidentMemory,
        };

        if (_useFileStorage)
        {
            await FileStoragePointerScan(scanOptions, pointerScanOptionsDto.StoragePath, cancellationToken);
        }
        else
        {
            await InMemoryPointerScan(scanOptions, cancellationToken);
        }
    }

    private async Task FileStoragePointerScan(PointerScanOptions pointerScanOptions, string fileName, CancellationToken cancellationToken)
    {
        var pointerScanner = new DefaultPointerScanner(_nativeApi, pointerScanOptions);
        _logger.LogDebug("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
            pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset.ToString("X"), pointerScanOptions.SearchedAddress.ToString("X"));
        var stopwatch = Stopwatch.StartNew();
        await pointerScanner.StartPointerScanAsync(_processSelectionTracker.SelectedProcessHandle, StorageType.File, fileName);
        stopwatch.Stop();
        _logger.LogInformation("Pointer scan completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        _pointerScanResultReader = new PointerScanResultReader(fileName);
    }

    private async Task InMemoryPointerScan(PointerScanOptions pointerScanOptions, CancellationToken cancellationToken)
    {
        var pointerScanner = new DefaultPointerScanner(_nativeApi, pointerScanOptions);
        _logger.LogDebug("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
            pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset.ToString("X"), pointerScanOptions.SearchedAddress.ToString("X"));
        var stopwatch = Stopwatch.StartNew();
        _pointerScanResults.AddRange(await pointerScanner.StartPointerScanAsync(_processSelectionTracker.SelectedProcessHandle, cancellationToken: cancellationToken));
        stopwatch.Stop();
        _logger.LogInformation("Pointer scan completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
    }

    public PointerScanResultsPageDto GetPointerScanResults(int page, int pageSize)
    {
        if (_pointerScanResultReader == null && _pointerScanResults.Count == 0)
            return new PointerScanResultsPageDto();

        Pointer[] pointerScanResultsPage;
        var totalCount = 0;

        if (_useFileStorage)
        {
            pointerScanResultsPage = _pointerScanResultReader!.ReadPointers(page * pageSize, pageSize);
            totalCount = _pointerScanResultReader.TotalItemCount;
        }
        else
        {
            pointerScanResultsPage = GetPointerScanResultItemsByPage(page, pageSize).ToArray();
            totalCount = _pointerScanResults.Count;
        }

        _nativeApi.UpdateAddresses(_processSelectionTracker.SelectedProcessHandle, pointerScanResultsPage);

        return new PointerScanResultsPageDto
        {
            Items = pointerScanResultsPage
            .Select(x => new PointerScanResultDto
            {
                ModuleNameWithBaseOffset = x.ModuleNameWithBaseOffset,
                Offsets = x.Offsets.Select(x => x.ToString("X")).Reverse().ToArray(),
                PointingToWithValue = $"{x.PointingTo.ToString("X8")} = {x.Value}"
            }).ToList(),
            TotalCount = totalCount
        };
    }

    public async Task Rescan(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return;

        if (!IntPtr.TryParse(address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var searchedAddress))
        {
            throw new ArgumentException("Invalid scan address format. Please provide a valid hexadecimal address.");
        }

        var pointerScanner = new DefaultPointerScanner(_nativeApi, new PointerScanOptions());
        var foundPointers = await pointerScanner.RescanPointersAsync(_pointerScanResults, searchedAddress, _processSelectionTracker.SelectedProcessHandle);
        _pointerScanResults.Clear();
        _pointerScanResults.AddRange(foundPointers);
    }

    public async Task CancelScanAsync()
    {
        var cts = Interlocked.Exchange(ref _scanCancellationTokenSource, null);

        if (cts != null)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }
    }

    public void ApplySingleSorting(TableSorting[] tableSortings)
    {
        // [{id:"offsets[0]", desc:false},{id:"offsets[1]", desc:false},{id:"moduleNameWithBaseOffset", desc:true}]
        // using List.Sort
        if (tableSortings.Length == 0)
        {
            return;
        }

        var tableSorting = tableSortings[0];
        if (tableSorting.Id == "moduleNameWithBaseOffset")
        {
            _pointerScanResults.Sort((a, b) =>
            {
                int comparison = string.Compare(a.ModuleNameWithBaseOffset, b.ModuleNameWithBaseOffset, StringComparison.OrdinalIgnoreCase);
                return tableSorting.Desc ? -comparison : comparison;
            });
        }
        else if (tableSorting.Id.StartsWith("offsets["))
        {
            _pointerScanResults.Sort((a, b) =>
            {
                int comparison = ComparePointersByOffset(a, b, tableSorting); ;
                return tableSorting.Desc ? -comparison : comparison;
            });
        }
    }

    public void ApplyMultipleSorting(TableSorting[] tableSortings)
    {
        // [{id:"offsets[0]", desc:false},{id:"offsets[1]", desc:false},{id:"moduleNameWithBaseOffset", desc:true}]
        // using List.Sort

        if (tableSortings.Length == 0)
        {
            return;
        }

        _pointerScanResults.Sort((a, b) =>
        {
            foreach (var sorting in tableSortings)
            {
                int comparison = 0;
                switch (sorting.Id)
                {
                    case "moduleNameWithBaseOffset":
                        comparison = string.Compare(a.ModuleNameWithBaseOffset, b.ModuleNameWithBaseOffset, StringComparison.OrdinalIgnoreCase);
                        break;
                    default:
                        if (sorting.Id.StartsWith("offsets["))
                        {
                            comparison = ComparePointersByOffset(a, b, sorting);
                        }
                        break;
                }

                if (comparison != 0)
                {
                    return sorting.Desc ? -comparison : comparison;
                }
            }

            return 0;
        });
    }

    private int ComparePointersByOffset(Pointer a, Pointer b, TableSorting tableSorting)
    {
        var indexStr = tableSorting.Id[8..^1];
        if (int.TryParse(indexStr, out int index))
        {
            var aOffset = index < a.Offsets.Count ? a.Offsets[a.Offsets.Count - 1 - index] : -1;
            var bOffset = index < b.Offsets.Count ? b.Offsets[b.Offsets.Count - 1 - index] : -1;
            int comparison = aOffset.CompareTo(bOffset);
            return comparison;
        }

        return 0;
    }

    private IEnumerable<Pointer> GetPointerScanResultItemsByPage(int page, int pageSize)
    {
        return _pointerScanResults
            .Skip(page * pageSize)
            .Take(pageSize);
    }

    public void AddToTrackedItems(int pageIndex, int pageSize, int rowIndex)
    {
        var itemIndex = pageIndex * pageSize + rowIndex;

        if (itemIndex < 0 || itemIndex >= _pointerScanResults.Count)
            return;

        var selectedItem = _pointerScanResults[itemIndex];

        if (selectedItem != null)
        {
            _trackedItemNotifier.RaiseItemAdded(selectedItem);
        }
    }

    public void Dispose()
    {
        _pointerScanResultReader?.Dispose();

        if (_scanCancellationTokenSource != null)
        {
            _scanCancellationTokenSource.Cancel();
            _scanCancellationTokenSource.Dispose();
        }
    }
}
