using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CelSerEngine.WpfBlazor.Components;
public partial class TrackedItemsGrid : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public List<TrackedItem> TrackedItems { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private IJSObjectReference? _module;

    protected override bool ShouldRender() => false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/TrackedItemsGrid.razor.js");
            await _module!.InvokeVoidAsync("initTackedItems");
        }
    }

    public async Task RefreshDataAsync()
    {
        var jsonData = JsonSerializer.Serialize(TrackedItems.Select(x => new { x.IsFrozen, x.Description, Address = x.Item.Address.ToString("X"), x.Item.Value }));
        await _module!.InvokeVoidAsync("applyTrackedItems", jsonData);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
