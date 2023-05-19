using System;

namespace CelSerEngine.Models;

// TODO is this class even needed?
public class ProcessMemory : IProcessMemory
{
    public IntPtr BaseAddress { get; set; }
    public int BaseOffset { get; set; }
    public IntPtr Address => BaseAddress + BaseOffset;
    public dynamic Value { get; set; } = 0L;
    public ScanDataType ScanDataType { get; set; } = ScanDataType.Integer;
}
