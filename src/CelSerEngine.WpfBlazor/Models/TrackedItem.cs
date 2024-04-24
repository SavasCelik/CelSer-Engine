using CelSerEngine.Core.Models;

namespace CelSerEngine.WpfBlazor.Models;
public class TrackedItem
{
    public bool IsFrozen { get; set; }
    public string Description { get; set; }
    public IMemorySegment Item { get; set; }
    public string SetValue { get; set; }

    public TrackedItem(IMemorySegment item)
    {
        Description = "Description";
        Item = item;
        SetValue = item.Value;
    }
}
