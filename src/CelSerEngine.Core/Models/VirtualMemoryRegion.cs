namespace CelSerEngine.Core.Models;

public class VirtualMemoryRegion
{
    public IntPtr BaseAddress { get; set; }
    public ulong RegionSize { get; set; }
    public byte[] Bytes { get; set; }
    
    public VirtualMemoryRegion(IntPtr baseAddress, ulong regionSize, byte[] bytes)
    {
        BaseAddress = baseAddress;
        RegionSize = regionSize;
        Bytes = bytes;
    }
}
