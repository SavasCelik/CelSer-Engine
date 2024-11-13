using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using CelSerEngine.WpfBlazor.Components.Modals;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace CelSerEngine.WpfBlazor.Components;

public partial class PointerScanner : ComponentBase, IDisposable
{
    [Parameter]
    public PointerScanOptionsSubmitModel PointerScanOptionsSubmitModel { get; set; } = default!;

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
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/PointerScanner.razor.js");
            await _module!.InvokeVoidAsync("initPointerScanner", _dotNetHelper);

            var pointerScanOptions = new PointerScanOptions
            {
                MaxOffset = 0x1000,
                MaxLevel = 4,
                SearchedAddress = new IntPtr(long.Parse(PointerScanOptionsSubmitModel.ScanAddress, NumberStyles.HexNumber))
            };
            var pointerScanner = new DefaultPointerScanner((NativeApi)NativeApi, pointerScanOptions);
            var foundPointers = await pointerScanner.StartPointerScanAsync(EngineSession.SelectedProcessHandle);
            await _module!.InvokeVoidAsync("applyPointerScannerResults", foundPointers.Select(x => new { BaseAddress = x.ModuleNameWithBaseOffset }));
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
