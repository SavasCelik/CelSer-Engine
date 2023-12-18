using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Scanners;

internal class PointerList
{
    public int Level { get; internal set; }
    public PointerList Previous { get; internal set; }
    public PointerList Next { get; internal set; }
    public HashSet<Pointer> Pointers { get; internal set; }
    public HashSet<Pointer> Results { get; internal set; }
}