using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.WpfBlazor.Models;
using Microsoft.AspNetCore.Components;

namespace CelSerEngine.WpfBlazor.Components;

public partial class ScanResultItemsGrid : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public TrackedItemsGrid TrackedItemsGridRef { get; set; } = default!;

    [Inject]
    private EngineSession EngineSession { get; set; } = default!;

    [Inject]
    private INativeApi NativeApi { get; set; } = default!;

    private VirtualizedAgGrid<MemorySegment> VirtualizedAgGridRef { get; set; } = default!;
    private List<MemorySegment> ScanResultItems { get; set; }
    private ICollection<ContextMenuItem> ContextMenuItems { get; set; }

    private bool _updaterStarted;
    private readonly Timer _scanResultsUpdater;

    public ScanResultItemsGrid()
    {
        _scanResultsUpdater = new Timer((e) => UpdateVisibleScanResults(), null, Timeout.Infinite, 0);
        ScanResultItems = [];
        ContextMenuItems =
        [
            new ContextMenuItem
            {
                Text = "Add selected addresses to the tracked addresses",
                OnClick = EventCallback.Factory.Create(this, ContextMenuItemClickedAsync)
            },
        ];
    }

    public async Task AddScanResultItemsAsync(IEnumerable<MemorySegment> items)
    {
        ScanResultItems.AddRange(items);
        await VirtualizedAgGridRef.ApplyDataAsync();

        if (!_updaterStarted && ScanResultItems.Count > 0)
            StartScanResultValueUpdater();
    }

    public async Task ClearScanResultItemsAsync(bool updateTable = false)
    {
        StopScanResultValueUpdater();
        ScanResultItems.Clear();

        if (updateTable)
        {
            await VirtualizedAgGridRef.ApplyDataAsync();
            await VirtualizedAgGridRef.ShowScanningOverlayAsync();
        }
    }

    public async Task ResetScanResultItemsAsync()
    {
        StopScanResultValueUpdater();
        ScanResultItems.Clear();
        await VirtualizedAgGridRef.ResetGridAsync();
    }

    public async Task ShowScanningOverlayAsync()
    {
        StopScanResultValueUpdater();
        await VirtualizedAgGridRef.ShowScanningOverlayAsync();
    }

    public IEnumerable<MemorySegment> GetScanResultItems()
    {
        return ScanResultItems;
    }

    private async Task OnScanResultItemDoubleClicked(MemorySegment scanResultItem)
    {
        await TrackedItemsGridRef.AddTrackedItem(new TrackedItem(scanResultItem));
    }

    private async Task ContextMenuItemClickedAsync()
    {
        var selectedScanResultItems = ScanResultItems.Where(x => VirtualizedAgGridRef.SelectedItems.Contains(x.Address.ToString("X")));
        await TrackedItemsGridRef.AddTrackedItems(selectedScanResultItems.Select(x => new TrackedItem(x)));
    }

    private void StartScanResultValueUpdater()
    {
        _scanResultsUpdater.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
        _updaterStarted = true;
    }

    private void StopScanResultValueUpdater()
    {
        _scanResultsUpdater.Change(Timeout.Infinite, 0);
        _updaterStarted = false;
    }

    private async void UpdateVisibleScanResults()
    {
        var visibleItems = VirtualizedAgGridRef.GetVisibleItems().ToList();

        if (visibleItems.Count == 0)
            return;

        NativeApi.UpdateAddresses(EngineSession.SelectedProcessHandle, visibleItems);
        await VirtualizedAgGridRef.ApplyDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        StopScanResultValueUpdater();
        await _scanResultsUpdater.DisposeAsync();
    }
}
