using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;

namespace CelSerEngine.WpfReact.ComponentControllers.TrackedItems;

public class TrackedItemsController : ReactControllerBase
{
    public List<TrackedItem> Items { get; set; }

    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly INativeApi _nativeApi;

    public TrackedItemsController(ProcessSelectionTracker processSelectionTracker, INativeApi nativeApi)
    {
        Items = [];
        _processSelectionTracker = processSelectionTracker;
        _nativeApi = nativeApi;
    }

    public void UpdateItems(int[] indices, string propertyKey, string newValue)
    {
        if (string.Equals(propertyKey, nameof(MemorySegment.Value), StringComparison.InvariantCultureIgnoreCase))
        {
            foreach (var index in indices)
            {
                _nativeApi.WriteMemory(_processSelectionTracker.SelectedProcessHandle, Items[index].MemorySegment, newValue);
            }
        }
        else if (string.Equals(propertyKey, nameof(TrackedItem.Description), StringComparison.InvariantCultureIgnoreCase))
        {
            foreach (var index in indices)
            {
                Items[index].Description = newValue;
            }
        }
    }

    public object[] GetTrackedItems()
    {
        _nativeApi.UpdateAddresses(_processSelectionTracker.SelectedProcessHandle, Items.Select(x => x.MemorySegment));

        return Items.Select(x => new
        {
            Description = x.Description,
            Address = x.MemorySegment.Address.ToString("X8"),
            Value = x.MemorySegment.Value
        }).ToArray();
    }
}
