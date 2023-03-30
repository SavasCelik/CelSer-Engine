using System;

namespace CelSerEngine.Models;

public class ProcessMemory
{
    public IntPtr BaseAddress { get; set; }
    public int Offset { get; set; }
    public IntPtr Address => BaseAddress + Offset;
    public byte[] Memory { get; set; }
}
