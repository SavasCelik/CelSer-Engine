using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Reflection;

namespace CelSerEngine.WpfBlazor.Components;

public partial class PointerScanner : ComponentBase, IDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

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
            await _module!.InvokeVoidAsync("applyPointerScannerResults", Enumerable.Range(1, 10).Select(x => new {BaseAddress= x.ToString("X")}));
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
