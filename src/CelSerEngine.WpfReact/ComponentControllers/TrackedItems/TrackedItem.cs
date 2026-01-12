using CelSerEngine.Core.Models;

namespace CelSerEngine.WpfReact.ComponentControllers.TrackedItems;

public class TrackedItem
{
    public string Description { get; set; }
    public IMemorySegment MemorySegment { get; set; }

    public TrackedItem(IMemorySegment item)
    {
        Description = "Description";

        if (item is Pointer pointerItem)
        {
            MemorySegment = pointerItem.Clone();
        }
        else
        {
            MemorySegment = new MemorySegment(item);
        }
    }
}
