using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;

namespace CelSerEngine.WpfReact.ComponentControllers.TrackedItems;

public class TrackedItemsController : ReactControllerBase
{
    public List<MemorySegment> Items { get; set; }

    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly INativeApi _nativeApi;

    public TrackedItemsController(ProcessSelectionTracker processSelectionTracker, INativeApi nativeApi)
    {
        Items = [];
        _processSelectionTracker = processSelectionTracker;
        _nativeApi = nativeApi;
    }

    public object[] GetTrackedItems()
    {
        _nativeApi.UpdateAddresses(_processSelectionTracker.SelectedProcessHandle, Items);

        return Items.Select(x => new
        {
            Description = "Description",
            Address = x.Address.ToString("X8"),
            Value = x.Value
        }).ToArray();
    }
}
