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
    public int MaxParallelWorkers { get; set; } = Environment.ProcessorCount;
    public bool LimitToMaxOffsetsPerNode { get; set; } = true;
    public int MaxOffsetsPerNode { get; set; } = 3;
    public bool PreventLoops { get; set; } = true;
    public bool AllowThreadStacksAsStatic { get; set; } = true;
    public int ThreadStacks { get; set; } = 2;
    public int StackSize { get; set; } = 0x1000;
    public bool AllowReadOnlyPointers { get; set; } = false;
    public bool OnlyOneStaticInPath { get; set; } = false;
    public bool OnlyResidentMemory { get; set; } = false;
}
