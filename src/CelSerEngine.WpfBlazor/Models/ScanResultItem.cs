using CelSerEngine.Core.Models;

namespace CelSerEngine.WpfBlazor.Models;

public class ScanResultItem : MemorySegment
{
    public string PreviousValue { get; set; }

    public ScanResultItem(IMemorySegment memorySegment) : base(memorySegment)
    {
        PreviousValue = memorySegment.Value;
    }
}
