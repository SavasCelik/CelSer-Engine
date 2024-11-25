using CelSerEngine.Core.Models;
using System.Text.Json.Serialization;

namespace CelSerEngine.WpfBlazor.Models;

public class PointerScanResultItem
{
    public string BaseAddress { get; set; }
    public string[] OffsetArray { get; set; }
    public string PointsTo { get; set; }

    [JsonConstructor]
    public PointerScanResultItem()
    {
        BaseAddress = string.Empty;
        OffsetArray = [];
        PointsTo = string.Empty;
    }

    public PointerScanResultItem(Pointer pointer)
    {
        BaseAddress = pointer.ModuleNameWithBaseOffset;
        OffsetArray = pointer.Offsets.Select(y => y.ToString("X")).Reverse().ToArray();
        PointsTo = string.Empty;
    }
}
