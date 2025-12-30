using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using System.Diagnostics;
using System.Globalization;

namespace CelSerEngine.WpfReact.ComponentControllers.PointerScanner;

public class PointerScannerController : ReactControllerBase
{
    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly INativeApi _nativeApi;
    private readonly List<Pointer> _pointerScanResults;

    public PointerScannerController(INativeApi nativeApi, ProcessSelectionTracker processSelectionTracker)
    {
        _nativeApi = nativeApi;
        _pointerScanResults = [];
        _processSelectionTracker = processSelectionTracker;
    }
    public async Task StartPointerScan(string scanAddress, int maxOffset, int maxLevel)
    {
        if (!IntPtr.TryParse(scanAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var searchedAddress))
        {
            throw new ArgumentException("Invalid scan address format. Please provide a valid hexadecimal address.");
        }

        await InMemoryPointerScan(new PointerScanOptions { SearchedAddress = searchedAddress, MaxOffset = maxOffset, MaxLevel = maxLevel});
    }

    private async Task InMemoryPointerScan(PointerScanOptions pointerScanOptions)
    {
        var pointerScanner = new DefaultPointerScanner(_nativeApi, pointerScanOptions);
        //Logger.LogInformation("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
        //    pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset.ToString("X"), pointerScanOptions.SearchedAddress.ToString("X"));
        //var stopwatch = Stopwatch.StartNew();
        _pointerScanResults.AddRange(await pointerScanner.StartPointerScanAsync(_processSelectionTracker.SelectedProcessHandle));
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

    private IEnumerable<Pointer> GetPointerScanResultItemsByPage(int page, int pageSize)
    {
        return _pointerScanResults
            .Skip(page * pageSize)
            .Take(pageSize);
    }
}
