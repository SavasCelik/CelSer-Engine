using CelSerEngine.Core.Models;
using System.Text.Json.Serialization;

namespace CelSerEngine.WpfReact.ComponentControllers.App;

public class MemoryScanSettings
{
    [JsonPropertyName("scanValue")]
    public string ScanValue { get; set; }

    [JsonPropertyName("fromValue")]
    public string FromValue { get; set; }

    [JsonPropertyName("toValue")]
    public string ToValue { get; set; }

    [JsonPropertyName("scanCompareType")]
    public ScanCompareType ScanCompareType { get; set; }

    [JsonPropertyName("scanValueType")]
    public ScanDataType ScanValueType { get; set; }

    [JsonPropertyName("startAddress")]
    public string StartAddress { get; set; }

    [JsonPropertyName("stopAddress")]
    public string StopAddress { get; set; }

    [JsonPropertyName("writable")]
    public MemoryScanFilterOptions Writable { get; set; }

    [JsonPropertyName("executable")]
    public MemoryScanFilterOptions Executable { get; set; }

    [JsonPropertyName("copyOnWrite")]
    public MemoryScanFilterOptions CopyOnWrite { get; set; }

    [JsonPropertyName("memoryTypes")]
    public MemoryType[] MemoryTypes { get; set; }
}
