namespace CelSerEngine.Core.Models;

public class ProcessMemory : IProcessMemory
{
    public IntPtr BaseAddress { get; set; }
    public int BaseOffset { get; set; }
    public IntPtr Address => BaseAddress + BaseOffset;
    public dynamic Value { get; set; } = 0L;
    public ScanDataType ScanDataType { get; set; } = ScanDataType.Integer;

    public ProcessMemory(IntPtr baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType)
    {
        BaseAddress = baseAddress;
        BaseOffset = baseOffset;
        Value = value;
        ScanDataType = scanDataType;
    }

    public ProcessMemory(IntPtr baseAddress, int baseOffset, ScanDataType scanDataType) : this(baseAddress, baseOffset, 0, scanDataType)
    {
    }
}
