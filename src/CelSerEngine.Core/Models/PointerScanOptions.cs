using Microsoft.Win32.SafeHandles;

namespace CelSerEngine.Core.Models;

public class PointerScanOptions
{
    public int ProcessId { get; set; }
    public SafeProcessHandle ProcessHandle { get; set; }
    public IntPtr SearchedAddress { get; set; }
    public bool RequireAlignedPointers { get; set; } = true;
    public int MaxLevel { get; set; }
    public int MaxOffset { get; set; }
    public int MaxParallelWorker { get; set; }
}
