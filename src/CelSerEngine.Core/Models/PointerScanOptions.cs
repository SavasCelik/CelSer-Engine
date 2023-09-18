namespace CelSerEngine.Core.Models;

public class PointerScanOptions
{
    public int ProcessId { get; set; }
    public IntPtr ProcessHandle { get; set; }
    public IntPtr SearchedAddress { get; set; }
    public int MaxLevel { get; set; }
    public int MaxOffset { get; set; }

}
