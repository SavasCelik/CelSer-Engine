using CelSerEngine.Core.Models;

namespace CelSerEngine.WpfReact;

public sealed class TrackedItemNotifier
{
    public event Action<MemorySegment>? ItemAdded;

    public void RaiseItemAdded(MemorySegment memorySegment) => ItemAdded?.Invoke(memorySegment);
}
