using System;

namespace CelSerEngine.Models;

public class ProcessMemory : IProcessMemory
{
    public IntPtr BaseAddress { get; set; }
    public int BaseOffset { get; set; }
    public byte[] Memory { get; set; } = Array.Empty<byte>();
    public IntPtr Address => BaseAddress + BaseOffset;
}
