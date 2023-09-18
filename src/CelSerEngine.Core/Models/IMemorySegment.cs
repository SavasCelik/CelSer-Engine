namespace CelSerEngine.Core.Models;

public interface IMemorySegment
{
    public IntPtr BaseAddress { get; set; }
    public int BaseOffset { get; set; }
    public IntPtr Address { get; }
    public string Value { get; set; }
    public ScanDataType ScanDataType { get; set; }
}
