using CelSerEngine.Core.Native;
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

    [Inject]
    private INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private EngineSession EngineSession { get; set; } = default!;

    [Inject]
    private ThemeManager ThemeManager { get; set; } = default!;

    private Modal ModalRef { get; set; } = default!;

    private IJSObjectReference? _module;
    private DotNetObjectReference<TrackedItemsGrid>? _dotNetHelper;
    private bool _shouldRender = false;

    protected override bool ShouldRender() => _shouldRender;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/TrackedItemsGrid.razor.js");
            await _module!.InvokeVoidAsync("initTackedItems", _dotNetHelper);
            ThemeManager.OnThemeChanged += UpdateComponent;
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
                { nameof(ModalValueChange.Value), selectedTrackedItem.Item.Value },
                { nameof(ModalValueChange.ValueChanged), EventCallback.Factory.Create<string>(this, (desiredValue) => OnValueChangeRequested(desiredValue, selectedTrackedItem)) },
            };

            await ModalRef.ShowAsync<ModalValueChange>("Change Value", parameters);
        }
        else if (columnName == nameof(TrackedItem.Description))
        {
            var parameters = new Dictionary<string, object>
            {
                { nameof(ModalDescriptionChange.Description), selectedTrackedItem.Description },
                { nameof(ModalDescriptionChange.DescriptionChanged1), EventCallback.Factory.Create<string>(this, (desiredDescription) => OnDescriptionChangeRequested(desiredDescription, selectedTrackedItem)) },
            };

            await ModalRef.ShowAsync<ModalDescriptionChange>("Change Description", parameters);
        }
    }

    private async Task OnValueChangeRequested(string desiredValue, params TrackedItem[] trackedItems)
    {
        foreach (var trackedItem in trackedItems)
        {
            if (trackedItem.IsFrozen)
            {
                trackedItem.SetValue = desiredValue;
            }
            trackedItem.Item.Value = desiredValue;
            NativeApi.WriteMemory(EngineSession.SelectedProcessHandle, trackedItem.Item, desiredValue);
        }

        await RefreshDataAsync();
    }

    private async Task OnDescriptionChangeRequested(string desiredDescription, params TrackedItem[] trackedItems)
    {
        foreach (var trackedItem in trackedItems)
        {
            trackedItem.Description = desiredDescription;
        }

        await RefreshDataAsync();
    }

    private void UpdateComponent()
    {
        _shouldRender = true;
        StateHasChanged();
        _shouldRender = false;
    }

    public async ValueTask DisposeAsync()
    {
        ThemeManager.OnThemeChanged -= UpdateComponent;

        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
