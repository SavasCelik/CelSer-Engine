using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using CelSerEngine.WpfBlazor.Components.AgGrid;
using CelSerEngine.WpfBlazor.Components.Modals;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.Win32;
using System.Diagnostics;
using Pointer = CelSerEngine.Core.Models.Pointer;

namespace CelSerEngine.WpfBlazor.Components.PointerScanner;

public partial class PointerScanner : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public IntPtr SearchedAddress { get; set; }

    [Inject]
    private ILogger<PointerScanner> Logger { get; set; } = default!;

    [Inject]
    private MainWindow MainWindow { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private EngineSession EngineSession { get; set; } = default!;

    [Inject]
    private INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private ThemeManager ThemeManager { get; set; } = default!;

    private VirtualizedAgGrid<Pointer, PointerScanResultItem> VirtualizedAgGridRef { get; set; } = default!;
    private Modal ModalRef { get; set; } = default!;
    private GridOptions GridOptions { get; }
    private List<Pointer> ScanResultItems { get; }
    private PointerScanResultReader? _pointerScanResultReader;

    private readonly Timer _scanResultsUpdater;

    public PointerScanner()
    {
        _scanResultsUpdater = new Timer(_ => UpdateVisibleScanResults(), null, Timeout.Infinite, 0);
        GridOptions = new GridOptions
        {
            ColumnDefs =
            [
                new ColumnDef { Field = "BaseAddress", HeaderName = "Address" },
                new ColumnDef { Field = "OffsetArray", HeaderName = "Offset", IsArray = true, ArraySize = 0 },
                new ColumnDef { Field = "PointsToWithValue", HeaderName = "Points To" }
            ]
        };

        ScanResultItems = [];
    }

    private void StartScanResultValueUpdater()
    {
        _scanResultsUpdater.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void StopScanResultValueUpdater()
    {
        _scanResultsUpdater.Change(Timeout.Infinite, 0);
    }

    private async void UpdateVisibleScanResults()
    {
        var visibleItems = VirtualizedAgGridRef.GetVisibleItems().ToList();

        if (visibleItems.Count == 0)
            return;

        NativeApi.UpdateAddresses(EngineSession.SelectedProcessHandle, visibleItems);
        await VirtualizedAgGridRef.ApplyDataAsync();
    }

    private async Task OnModalReadyAsync()
    {
        var parameters = new Dictionary<string, object>
        {
            { nameof(ModalPointerScanOptions.SearchedAddress), SearchedAddress.ToString("X") },
            { nameof(ModalPointerScanOptions.OnPointerScanOptionsSubmit), EventCallback.Factory.Create<PointerScanOptions>(this, OnPointerScanOptionsSubmit) },
        };

        await ModalRef.ShowAsync<ModalPointerScanOptions>("Pointer scanner options", parameters);
    }

    private Task<(ICollection<Pointer>, int)> GetPointerScanResultsAsync(int startIndex, int amount)
    {
        //var pointers = ScanResultItems.Skip(startIndex).Take(amount).ToList();
        ArgumentNullException.ThrowIfNull(_pointerScanResultReader);
        var pointers = _pointerScanResultReader.ReadPointers(startIndex, amount).ToList();
        NativeApi.UpdateAddresses(EngineSession.SelectedProcessHandle, pointers);

        return Task.FromResult<(ICollection<Pointer>, int)>((pointers, _pointerScanResultReader.TotalItemCount));
    }

    private async Task OnPointerScanOptionsSubmit(PointerScanOptions pointerScanOptions)
    {
        const StorageType storageType = StorageType.File; // currently only allowing file storage
        GridOptions.ColumnDefs[1].ArraySize = pointerScanOptions.MaxLevel;
        await VirtualizedAgGridRef.UpdateColumnDefs();
        await VirtualizedAgGridRef.ShowScanningOverlayAsync();

        if (storageType == StorageType.File)
        {
            await FileStoragePointerScan(pointerScanOptions);
        }
        else
        {
            await InMemoryPointerScan(pointerScanOptions);
        }
    }

    private async Task FileStoragePointerScan(PointerScanOptions pointerScanOptions)
    {
        var fileName = "";
        await MainWindow.Dispatcher.InvokeAsync(() =>
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

        if (string.IsNullOrEmpty(fileName))
            return;

        var pointerScanner = new DefaultPointerScanner((NativeApi)NativeApi, pointerScanOptions);
        Logger.LogInformation("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
            pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset.ToString("X"), pointerScanOptions.SearchedAddress.ToString("X"));
        var stopwatch = Stopwatch.StartNew();
        await pointerScanner.StartPointerScanAsync(EngineSession.SelectedProcessHandle, StorageType.File, fileName);
        stopwatch.Stop();
        Logger.LogInformation("Pointer scan completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        _pointerScanResultReader = new PointerScanResultReader(fileName);

        if (_pointerScanResultReader.TotalItemCount > 0)
        {
            await VirtualizedAgGridRef.ApplyDataAsync();
            StartScanResultValueUpdater();
        }
    }

    private async Task InMemoryPointerScan(PointerScanOptions pointerScanOptions)
    {
        var pointerScanner = new DefaultPointerScanner((NativeApi)NativeApi, pointerScanOptions);
        Logger.LogInformation("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
            pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset.ToString("X"), pointerScanOptions.SearchedAddress.ToString("X"));
        var stopwatch = Stopwatch.StartNew();
        ScanResultItems.AddRange(await pointerScanner.StartPointerScanAsync(EngineSession.SelectedProcessHandle));
        stopwatch.Stop();
        Logger.LogInformation("Pointer scan completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

        if (ScanResultItems.Count > 0)
        {
            await VirtualizedAgGridRef.ApplyDataAsync();
            StartScanResultValueUpdater();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        StopScanResultValueUpdater();
        await _scanResultsUpdater.DisposeAsync();
        _pointerScanResultReader?.Dispose();
        GC.SuppressFinalize(this);
    }
}
