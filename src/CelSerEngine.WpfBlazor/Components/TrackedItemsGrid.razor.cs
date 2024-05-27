using CelSerEngine.Core.Native;
using CelSerEngine.WpfBlazor.Components.Modals;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace CelSerEngine.WpfBlazor.Components;
public partial class TrackedItemsGrid : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private EngineSession EngineSession { get; set; } = default!;

    [Inject]
    private ThemeManager ThemeManager { get; set; } = default!;

    private Modal ModalRef { get; set; } = default!;
    private ICollection<ContextMenuItem> ContextMenuItems { get; set; }

    private List<TrackedItem> _trackedItems;
    private IJSObjectReference? _module;
    private DotNetObjectReference<TrackedItemsGrid>? _dotNetHelper;
    private bool _shouldRender = false;
    private bool _startedUpdater = false;
    private readonly Timer _trackedItemsUpdater;

    public TrackedItemsGrid()
    {
        _trackedItemsUpdater = new Timer((e) => UpdateTrackedItems(), null, Timeout.Infinite, 0);
        _trackedItems = [];
        ContextMenuItems =
        [
            new ContextMenuItem
            {
                Text = "Change Value",
                OnClick = EventCallback.Factory.Create(this, OnChangeValueContextMenuClicked)
            },
            new ContextMenuItem
            {
                Text = "Change Description",
                OnClick = EventCallback.Factory.Create(this, OnChangeDescriptionContextMenuClicked)
            },
            new ContextMenuItem
            {
                Text = "Remove selected items",
                OnClick = EventCallback.Factory.Create(this, OnRemoveSelectedItemsContextMenuClicked)
            },
        ];
    }

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

    private void UpdateComponent()
    {
        _shouldRender = true;
        StateHasChanged();
        _shouldRender = false;
    }

    public async Task AddTrackedItem(TrackedItem trackedItem)
    {
        _trackedItems.Add(trackedItem);
        await RefreshDataAsync();
    }

    public async Task AddTrackedItems(IEnumerable<TrackedItem> trackedItems)
    {
        _trackedItems.AddRange(trackedItems);
        await RefreshDataAsync();
    }

    private async Task RefreshDataAsync()
    {
        if (!_startedUpdater && _trackedItems.Count > 0)
        {
            StartTrackedItemsUpdater();
            _startedUpdater = true;
        }
        else if (_startedUpdater && _trackedItems.Count == 0)
        {
            StopTrackedItemValueUpdater();
            _startedUpdater = false;
        }

        var jsonData = JsonSerializer.Serialize(_trackedItems.Select(x => new { x.IsFrozen, x.Description, Address = x.Item.Address.ToString("X"), x.Item.Value }));
        await _module!.InvokeVoidAsync("applyTrackedItems", jsonData);
    }

    private async void UpdateTrackedItems()
    {
        NativeApi.UpdateAddresses(EngineSession.SelectedProcessHandle, _trackedItems.Select(x => x.Item));

        foreach (var trackedItem in _trackedItems.Where(x => x.IsFrozen))
        {
            NativeApi.WriteMemory(EngineSession.SelectedProcessHandle, trackedItem.Item, trackedItem.SetValue);
        }

        var jsonData = JsonSerializer.Serialize(_trackedItems.Select(x => new { x.IsFrozen, x.Description, Address = x.Item.Address.ToString("X"), x.Item.Value }));
        await _module!.InvokeVoidAsync("updateTrackedItemValues", jsonData);
    }

    private void StartTrackedItemsUpdater()
    {
        _trackedItemsUpdater.Change(TimeSpan.Zero, TimeSpan.FromSeconds(0.1));
    }

    private void StopTrackedItemValueUpdater()
    {
        _trackedItemsUpdater.Change(Timeout.Infinite, 0);
    }

    private async Task OnChangeValueContextMenuClicked()
    {
        var selectedTrackedItems = await GetSelectedTrackedItems();
        await ShowChangeValueModal(selectedTrackedItems);
    }

    private async Task OnChangeDescriptionContextMenuClicked()
    {
        var selectedTrackedItems = await GetSelectedTrackedItems();
        await ShowChangeDescriptionModal(selectedTrackedItems);
    }

    private async Task<TrackedItem[]> GetSelectedTrackedItems()
    {
        var selectedIndexes = await _module!.InvokeAsync<int[]>("getSelectedRowIndexes");

        return selectedIndexes.Select(x => _trackedItems[x]).ToArray();
    }

    [JSInvokable]
    public void UpdateFreezeStateByRowIndex(int rowIndex, bool isFrozen)
    {
        var trackedItem = _trackedItems[rowIndex];
        trackedItem.IsFrozen = isFrozen;

        if (isFrozen)
        {
            trackedItem.SetValue = trackedItem.Item.Value;
        }
    }

    [JSInvokable]
    public async Task OnCellDoubleClickedAsync(int rowIndex, string columnName)
    {
        var selectedTrackedItem = _trackedItems[rowIndex];

        if (columnName == nameof(TrackedItem.Item.Value))
        {
            await ShowChangeValueModal(selectedTrackedItem);
        }
        else if (columnName == nameof(TrackedItem.Description))
        {
            await ShowChangeDescriptionModal(selectedTrackedItem);
        }
    }

    private async Task ShowChangeValueModal(params TrackedItem[] selectedTrackedItems)
    {
        if (selectedTrackedItems.Length == 0)
            return;

        var parameters = new Dictionary<string, object>
        {
            { nameof(ModalValueChange.Value), selectedTrackedItems[0].Item.Value },
            { nameof(ModalValueChange.ValueChanged), EventCallback.Factory.Create<string>(this, (desiredValue) => OnValueChangeRequested(desiredValue, selectedTrackedItems)) },
        };

        await ModalRef.ShowAsync<ModalValueChange>("Change Value", parameters);
    }

    private async Task ShowChangeDescriptionModal(params TrackedItem[] selectedTrackedItems)
    {
        if (selectedTrackedItems.Length == 0)
            return;

        var parameters = new Dictionary<string, object>
        {
            { nameof(ModalDescriptionChange.Description), selectedTrackedItems[0].Description },
            { nameof(ModalDescriptionChange.DescriptionChanged), EventCallback.Factory.Create<string>(this, (desiredDescription) => OnDescriptionChangeRequested(desiredDescription, selectedTrackedItems)) },
        };

        await ModalRef.ShowAsync<ModalDescriptionChange>("Change Description", parameters);
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

    private async Task OnRemoveSelectedItemsContextMenuClicked()
    {
        var selectedIndexes = await _module!.InvokeAsync<int[]>("getSelectedRowIndexes");
        
        foreach (var index in selectedIndexes)
        {
            _trackedItems.RemoveAt(index);
        }

        await RefreshDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        StopTrackedItemValueUpdater();
        _trackedItemsUpdater.Dispose();
        ThemeManager.OnThemeChanged -= UpdateComponent;

        if (_module != null)
        {
            await JSRuntime.InvokeVoidAsync("console.log", $"Disposing {nameof(TrackedItemsGrid)} js");
            await _module.DisposeAsync();
        }
    }
}
