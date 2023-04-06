using System;

namespace CelSerEngine.Models;

public interface IProcessMemory
{
    public IntPtr BaseAddress { get; set; }
    public int BaseOffset { get; set; }
    public byte[] Memory { get; set; }
    public IntPtr Address { get; }
}
