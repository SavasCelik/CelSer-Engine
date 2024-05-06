using CelSerEngine.WpfBlazor.Components.Modals;
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

    private Modal ModalRef { get; set; } = default!;

    private IJSObjectReference? _module;
    private DotNetObjectReference<TrackedItemsGrid>? _dotNetHelper;

    protected override bool ShouldRender() => false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/TrackedItemsGrid.razor.js");
            await _module!.InvokeVoidAsync("initTackedItems", _dotNetHelper);
        }
    }

    public async Task RefreshDataAsync()
    {
        var jsonData = JsonSerializer.Serialize(TrackedItems.Select(x => new { x.IsFrozen, x.Description, Address = x.Item.Address.ToString("X"), x.Item.Value }));
        await _module!.InvokeVoidAsync("applyTrackedItems", jsonData);
    }

    [JSInvokable]
    public async Task OnCellDoubleClickedAsync(int rowIndex, string columnName)
    {
        var selectedTrackedItem = TrackedItems[rowIndex];

        if (columnName == nameof(TrackedItem.Item.Value))
        {
            var parameters = new Dictionary<string, object>
            {
                { nameof(ModalValueChange.CurrentValue), selectedTrackedItem.Item.Value },
                { nameof(ModalValueChange.ValueChanged), EventCallback.Factory.Create<string>(this, (desiredValue) => OnValueChangeRequested(desiredValue, selectedTrackedItem)) },
            };

            await ModalRef.ShowAsync<ModalValueChange>("Value Change", parameters);
        }
    }

    private async Task OnValueChangeRequested(string desiredValue, params TrackedItem[] trackedItems)
    {
        foreach (var trackedItem in trackedItems)
        {
            trackedItem.Item.Value = desiredValue;
        }

        await RefreshDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
