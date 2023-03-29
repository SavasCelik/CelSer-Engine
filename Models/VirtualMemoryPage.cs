
using System;
using CelSerEngine.Native;

namespace CelSerEngine.Models;

public class VirtualMemoryPage
{
    public ulong BaseAddress { get; set; }
    public ulong RegionSize { get; set; }
    public byte[] Bytes { get; set; }
    
    public VirtualMemoryPage(ulong baseAddress, ulong regionSize, byte[] bytes)
    {
        BaseAddress = baseAddress;
        RegionSize = regionSize;
        Bytes = bytes;
    }

    public void ReReadBytes(IntPtr processHandle)
    {
        Bytes = NativeApi.ReadVirtualMemory(processHandle, (IntPtr)BaseAddress, (uint)RegionSize);
    }
}
