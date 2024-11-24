using CelSerEngine.Core.Models;

namespace CelSerEngine.WpfBlazor.Models;

public class ScanResultItem(MemorySegment memorySegment)
{
    public string Address { get; init; } = memorySegment.Address.ToString("X");
    public string Value { get; init; } = memorySegment.Value;
    public string PreviousValue { get; init; } = memorySegment.InitialValue;
}