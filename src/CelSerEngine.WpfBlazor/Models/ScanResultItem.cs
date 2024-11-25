using CelSerEngine.Core.Models;
using System.Text.Json.Serialization;

namespace CelSerEngine.WpfBlazor.Models;

public class ScanResultItem
{
    public string Address { get; init; }
    public string Value { get; init; }
    public string PreviousValue { get; init; }

    [JsonConstructor]
    public ScanResultItem()
    {
        Address = string.Empty;
        Value = string.Empty;
        PreviousValue = string.Empty;
    }

    public ScanResultItem(MemorySegment memorySegment)
    {
        Address = memorySegment.Address.ToString("X");
        Value = memorySegment.Value;
        PreviousValue = memorySegment.InitialValue;
    }
}