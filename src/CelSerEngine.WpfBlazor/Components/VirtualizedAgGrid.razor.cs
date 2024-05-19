using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;
using System.Text.Json;

namespace CelSerEngine.WpfBlazor.Components;

public partial class VirtualizedAgGrid<TItem> : ComponentBase, IAsyncDisposable
{
    public HashSet<string> SelectedItems { get; set; } = [];

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ThemeManager ThemeManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the fixed item source.
    /// </summary>
    [Parameter]
    public ICollection<TItem> Items { get; set; } = default!;

    [Parameter]
    public Func<TItem, string> GetRowId { get; set; } = default!;

    [Parameter]
    public Func<TItem, object> SerializableItem { get; set; } = default!;

    [Parameter]
    public EventCallback<TItem> OnRowDoubleClicked { get; set; }

    private CultureInfo _cultureInfo = new("en-US");
    private IJSObjectReference? _module;
    private DotNetObjectReference<VirtualizedAgGrid<TItem>>? _dotNetHelper;
    private int _lastStartIndex = 0;
    private int _lastItemCount = 0;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/VirtualizedAgGrid.razor.js");
            await _module.InvokeVoidAsync("initVirtualizedAgGrid", _dotNetHelper);
        }
    }

    public async Task ApplyDataAsync()
    {
        await _module!.InvokeVoidAsync("itemsChanged", Items.Count);
    }

    public async Task ShowScanningOverlayAsync()
    {
        await _module!.InvokeVoidAsync("showLoadingOverlay");
    }

    public async Task ResetGridAsync()
    {
        await _module!.InvokeVoidAsync("resetGrid");
    }

    public IEnumerable<TItem> GetVisibleItems()
    {
        return Items.Skip(_lastStartIndex).Take(_lastItemCount);
    }

    [JSInvokable]
    public Task OnRowDoubleClickedDispatcherAsync(string rowId)
    {
        if (OnRowDoubleClicked.HasDelegate)
        {
            var doubleClickedRow = Items.Single(x => GetRowId(x) == rowId);
            OnRowDoubleClicked.InvokeAsync(doubleClickedRow);
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task<string> GetItemsAsync(int startIndex, int amount)
    {
        _lastStartIndex = startIndex;
        _lastItemCount = amount;
        var visibleItems = Items.Skip(startIndex).Take(amount);

        return Task.FromResult(JsonSerializer.Serialize(visibleItems.Select(x => new { Item = SerializableItem(x), IsSelected = SelectedItems.Contains(GetRowId(x)) })));
    }

    [JSInvokable]
    public Task SelectTillItemAsync(string selectTillItem)
    {
        var isSlecting = false;
        var lastAddress = selectTillItem;
        var firstAddress = SelectedItems.LastOrDefault();

        if (firstAddress == null)
        {
            SelectedItems.Add(lastAddress);

            return Task.CompletedTask;
        }

        foreach (var item in Items)
        {
            var address = GetRowId(item);
            if (address == firstAddress || address == lastAddress)
            {
                isSlecting = !isSlecting;

                if (!isSlecting)
                {
                    SelectedItems.Add(address);
                    break;
                }
            }

            if (isSlecting)
            {
                SelectedItems.Add(address);
            }
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task<bool> IsItemSelectedAsync(string item)
    {
        return Task.FromResult(SelectedItems.Contains(item));
    }

    [JSInvokable]
    public Task ClearSelectedItems()
    {
        SelectedItems.Clear();

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task AddSelectedItemAsync(string item)
    {
        SelectedItems.Add(item);

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task RemoveSelectedItemAsync(string item)
    {
        SelectedItems.Remove(item);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _dotNetHelper?.Dispose();

        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
