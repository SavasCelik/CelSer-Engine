using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CelSerEngine.WpfReact.ComponentControllers;

public class MemoryScanSetting
{
    [JsonPropertyName("scanValue")]
    public string ScanValue { get; set; }

    [JsonPropertyName("fromValue")]
    public string FromValue { get; set; }

    [JsonPropertyName("toValue")]
    public string ToValue { get; set; }

    [JsonPropertyName("scanCompareType")]
    public string ScanCompareType { get; set; }

    [JsonPropertyName("scanValueType")]
    public string ScanValueType { get; set; }

    [JsonPropertyName("startAddress")]
    public string StartAddress { get; set; }

    [JsonPropertyName("stopAddress")]
    public string StopAddress { get; set; }

    [JsonPropertyName("writeable")]
    public string Writeable { get; set; }

    [JsonPropertyName("executable")]
    public string Executable { get; set; }

    [JsonPropertyName("copyOnWrite")]
    public string CopyOnWrite { get; set; }

    [JsonPropertyName("memoryTypes")]
    public List<string> MemoryTypes { get; set; }
}

public class AppController : ReactControllerBase
{
    public async Task OnFirstScan(MemoryScanSetting memoryScanSetting)
    {
        await Task.Delay(5000);
    }
}
