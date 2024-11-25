using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
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

    private IJSObjectReference? _module;
    private DotNetObjectReference<PointerScanner>? _dotNetHelper;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/PointerScanner/PointerScanner.razor.js");
            await _module!.InvokeVoidAsync("initPointerScanner", _dotNetHelper);

            var pointerScanOptions = new PointerScanOptions
            {
                MaxOffset = PointerScanOptionsSubmitModel.MaxOffset,
                MaxLevel = PointerScanOptionsSubmitModel.MaxLevel,
                SearchedAddress = new IntPtr(long.Parse(PointerScanOptionsSubmitModel.ScanAddress, NumberStyles.HexNumber))
            };
            var pointerScanner = new DefaultPointerScanner((NativeApi)NativeApi, pointerScanOptions);
            Logger.LogInformation("Starting pointer scan with options: MaxLevel = {MaxLevel}, MaxOffset = {MaxOffset}, SearchedAddress = {SearchedAddress}",
                pointerScanOptions.MaxLevel, pointerScanOptions.MaxOffset, pointerScanOptions.SearchedAddress.ToString("X"));
            var stopwatch = Stopwatch.StartNew();
            var foundPointers = await pointerScanner.StartPointerScanAsync(EngineSession.SelectedProcessHandle);
            stopwatch.Stop();
            Logger.LogInformation("Pointer scan completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            await _module!.InvokeVoidAsync("applyPointerScannerResults",
                new
                {
                    Pointers = foundPointers.Select(x => new PointerScanResultItem(x)),
                    MaxLevel = pointerScanOptions.MaxLevel
                });
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // using IAsyncDisposable causes the closing method in BlazorWebViewWindow.xaml.cs to throw an exception
        _dotNetHelper?.Dispose();

        if (_module != null)
        {
            JSRuntime.InvokeVoidAsync("console.log", $"Disposing {nameof(PointerScanner)} js");
            _module.DisposeAsync();
        }
    }
}
