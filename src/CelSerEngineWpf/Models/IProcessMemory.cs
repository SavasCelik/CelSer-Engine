using System;

namespace CelSerEngine.Models;

public interface IProcessMemory
{
    public IntPtr BaseAddress { get; set; }
    public int BaseOffset { get; set; }
    public IntPtr Address { get; }
    public dynamic Value { get; set; }
    public ScanDataType ScanDataType { get; set; }
}
