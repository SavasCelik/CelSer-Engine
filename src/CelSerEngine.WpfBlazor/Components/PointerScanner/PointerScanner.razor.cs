using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using CelSerEngine.WpfBlazor.Components.AgGrid;
using CelSerEngine.WpfBlazor.Components.Modals;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Diagnostics;

namespace CelSerEngine.WpfBlazor.Components.PointerScanner;

public partial class PointerScanner : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public IntPtr SearchedAddress { get; set; }

    [Inject]
    private ILogger<PointerScanner> Logger { get; set; } = default!;

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

    private async Task OnPointerScanOptionsSubmit(PointerScanOptions pointerScanOptions)
    {
        GridOptions.ColumnDefs[1].ArraySize = pointerScanOptions.MaxLevel;

        var pointerScanner = new DefaultPointerScanner((NativeApi)NativeApi, pointerScanOptions);
        Logger.LogInformation("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
            pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset, pointerScanOptions.SearchedAddress.ToString("X"));
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
        GC.SuppressFinalize(this);
    }
}
