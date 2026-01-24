using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using System.Globalization;

namespace CelSerEngine.WpfReact.ComponentControllers.PointerScanner;

public class PointerScannerController : ReactControllerBase, IDisposable
{
    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly TrackedItemNotifier _trackedItemNotifier;
    private readonly INativeApi _nativeApi;
    private readonly List<Pointer> _pointerScanResults;
    private CancellationTokenSource? _scanCancellationTokenSource;

    public PointerScannerController(INativeApi nativeApi, ProcessSelectionTracker processSelectionTracker, TrackedItemNotifier trackedItemNotifier)
    {
        _nativeApi = nativeApi;
        _pointerScanResults = [];
        _processSelectionTracker = processSelectionTracker;
        _trackedItemNotifier = trackedItemNotifier;
    }

    public async Task StartPointerScan(PointerScanOptionsDto pointerScanOptionsDto)
    {
        if (!IntPtr.TryParse(pointerScanOptionsDto.ScanAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var searchedAddress))
        {
            throw new ArgumentException("Invalid scan address format. Please provide a valid hexadecimal address.");
        }

        _scanCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _scanCancellationTokenSource.Token;

        await InMemoryPointerScan(new PointerScanOptions
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
        }, cancellationToken);
    }

    private async Task InMemoryPointerScan(PointerScanOptions pointerScanOptions, CancellationToken cancellationToken)
    {
        var pointerScanner = new DefaultPointerScanner(_nativeApi, pointerScanOptions);
        //Logger.LogInformation("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
        //    pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset.ToString("X"), pointerScanOptions.SearchedAddress.ToString("X"));
        //var stopwatch = Stopwatch.StartNew();
        _pointerScanResults.AddRange(await pointerScanner.StartPointerScanAsync(_processSelectionTracker.SelectedProcessHandle, cancellationToken: cancellationToken));
        //stopwatch.Stop();
        //Logger.LogInformation("Pointer scan completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
    }

    public object GetPointerScanResults(int page, int pageSize)
    {
        var pointerScanResultsPage = GetPointerScanResultItemsByPage(page, pageSize).ToList();
        _nativeApi.UpdateAddresses(_processSelectionTracker.SelectedProcessHandle, pointerScanResultsPage);

        return new
        {
            Items = pointerScanResultsPage
            .Select(x => new PointerScanResultDto
            {
                ModuleNameWithBaseOffset = x.ModuleNameWithBaseOffset,
                Offsets = x.Offsets.Select(x => x.ToString("X")).Reverse().ToArray(),
                PointingToWithValue = $"{x.PointingTo.ToString("X8")} = {x.Value}"
            }),
            TotalCount = _pointerScanResults.Count
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
        if (_scanCancellationTokenSource != null)
        {
            _scanCancellationTokenSource.Cancel();
            _scanCancellationTokenSource.Dispose();
        }
    }
}
