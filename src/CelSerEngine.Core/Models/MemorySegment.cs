namespace CelSerEngine.Core.Models;

public class MemorySegment : IMemorySegment
{
    public IntPtr BaseAddress { get; set; }
    public int BaseOffset { get; set; }
    public IntPtr Address => BaseAddress + BaseOffset;
    public string Value { get; set; }
    public string InitialValue { get; set; }
    public ScanDataType ScanDataType { get; set; } = ScanDataType.Integer;

    public MemorySegment(IntPtr baseAddress, int baseOffset, string value, ScanDataType scanDataType)
    {
        BaseAddress = baseAddress;
        BaseOffset = baseOffset;
        Value = value;
        ScanDataType = scanDataType;
        InitialValue = value;
    }

    public MemorySegment(IMemorySegment memorySegment)
        : this(memorySegment.BaseAddress, memorySegment.BaseOffset, memorySegment.Value, memorySegment.ScanDataType)
    {}
}
