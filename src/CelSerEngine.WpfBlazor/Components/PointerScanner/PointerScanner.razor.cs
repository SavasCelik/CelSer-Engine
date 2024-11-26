using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using CelSerEngine.WpfBlazor.Components.AgGrid;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Globalization;

namespace CelSerEngine.WpfBlazor.Components.PointerScanner;

public partial class PointerScanner : ComponentBase, IDisposable
{
    [Parameter]
    public PointerScanOptionsSubmitModel PointerScanOptionsSubmitModel { get; set; } = default!;

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
    private GridOptions GridOptions { get; }
    private List<Pointer> ScanResultItems { get; }
    private DotNetObjectReference<PointerScanner>? _dotNetHelper;

    public PointerScanner()
    {
        GridOptions = new GridOptions
        {
            ColumnDefs =
            [
                new ColumnDef { Field = "BaseAddress", HeaderName = "Address" },
                new ColumnDef { Field = "OffsetArray", HeaderName = "Offset", IsArray = true, ArraySize = 0 },
                new ColumnDef { Field = "PointsTo", HeaderName = "Previous Value" }
            ]
        };

        ScanResultItems = [];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            var pointerScanOptions = new PointerScanOptions
            {
                MaxOffset = PointerScanOptionsSubmitModel.MaxOffset,
                MaxLevel = PointerScanOptionsSubmitModel.MaxLevel,
                SearchedAddress = new IntPtr(long.Parse(PointerScanOptionsSubmitModel.ScanAddress, NumberStyles.HexNumber))
            };

            GridOptions.ColumnDefs[1].ArraySize = pointerScanOptions.MaxLevel;

            var pointerScanner = new DefaultPointerScanner((NativeApi)NativeApi, pointerScanOptions);
            Logger.LogInformation("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
                pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset, pointerScanOptions.SearchedAddress.ToString("X"));
            var stopwatch = Stopwatch.StartNew();
            ScanResultItems.AddRange(await pointerScanner.StartPointerScanAsync(EngineSession.SelectedProcessHandle));
            stopwatch.Stop();
            Logger.LogInformation("Pointer scan completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
            await VirtualizedAgGridRef.ApplyDataAsync();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // using IAsyncDisposable causes the closing method in BlazorWebViewWindow.xaml.cs to throw an exception
        _dotNetHelper?.Dispose();
        VirtualizedAgGridRef.DisposeAsync();
    }
}
