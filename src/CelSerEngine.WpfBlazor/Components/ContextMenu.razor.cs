using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace CelSerEngine.WpfBlazor.Components;
public partial class ContextMenu : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public ICollection<ContextMenuItem> Items { get; set; } = default!;

    [Parameter]
    public string Selector { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private Guid Id { get; set; } = Guid.NewGuid();

    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/ContextMenu.razor.js");
            await _module.InvokeVoidAsync("init", Selector, Id);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
