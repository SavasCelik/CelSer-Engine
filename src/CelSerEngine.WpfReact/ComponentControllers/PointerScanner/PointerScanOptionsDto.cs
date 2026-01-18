namespace CelSerEngine.WpfReact.ComponentControllers.PointerScanner;

public class PointerScanOptionsDto
{
    public string ScanAddress { get; set; } = "";
    public bool RequireAlignedPointers { get; set; }
    public int MaxLevel { get; set; }
    public int MaxOffset { get; set; }
    public int MaxParallelWorkers { get; set; }
    public bool LimitToMaxOffsetsPerNode { get; set; }
    public int MaxOffsetsPerNode { get; set; }
    public bool PreventLoops { get; set; }
    public bool AllowThreadStacksAsStatic { get; set; }
    public int ThreadStacks { get; set; }
    public int StackSize { get; set; }
}
