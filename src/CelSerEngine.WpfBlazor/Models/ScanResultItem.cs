using CelSerEngine.Core.Models;
using System.Text.Json.Serialization;

namespace CelSerEngine.WpfBlazor.Models;

public class ScanResultItem
{
    public string Address { get; init; }
    public string Value { get; init; }
    public string PreviousValue { get; init; }

    public ScanResultItem(MemorySegment memorySegment)
    {
        Address = memorySegment.Address.ToString("X");
        Value = memorySegment.Value;
        PreviousValue = memorySegment.InitialValue;
    }
}