namespace CelSerEngine.Core.Models;

public class VirtualMemoryRegion2
{
    public IntPtr BaseAddress { get; set; }
    public ulong MemorySize { get; set; }
    public bool InModule { get; set; }
    public bool ValidPointerRange { get; set; }
}
