using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;

namespace CelSerEngine.WpfReact.ComponentControllers.ScanResultItems;

public class ScanResultItemsController : ReactControllerBase
{
    public List<MemorySegment> ScanResultItems { get; set; }

    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly TrackedItemNotifier _trackedItemNotifier;
    private readonly INativeApi _nativeApi;

    public ScanResultItemsController(ProcessSelectionTracker processSelectionTracker, TrackedItemNotifier trackedItemNotifier, INativeApi nativeApi)
    {
        ScanResultItems = [];
        _processSelectionTracker = processSelectionTracker;
        _trackedItemNotifier = trackedItemNotifier;
        _nativeApi = nativeApi;
    }

    public object GetScanResultItems(int page, int pageSize)
    {
        var scanResultItems = GetScanResultItemsByPage(page, pageSize);
        _nativeApi.UpdateAddresses(_processSelectionTracker.SelectedProcessHandle, scanResultItems);

        return new
        {
            Items = scanResultItems
            .Select(x => new ScanResultItemReact
            {
                Address = x.Address.ToString("X8"),
                Value = x.Value,
                PreviousValue = x.InitialValue
            }),
            TotalCount = ScanResultItems.Count
        };
    }

    public IEnumerable<MemorySegment> GetScanResultItemsByPage(int page, int pageSize)
    {
        return ScanResultItems
            .Skip(page * pageSize)
            .Take(pageSize);
    }

    public void AddToTrackedItems(int pageIndex, int pageSize, int rowIndex)
    {
        var itemIndex = pageIndex * pageSize + rowIndex;

        if (itemIndex < 0 || itemIndex >= ScanResultItems.Count)
            return;

        var selectedItem = ScanResultItems[itemIndex];

        if (selectedItem != null)
        {
            _trackedItemNotifier.RaiseItemAdded(selectedItem);
        }
    }
}
