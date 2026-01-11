namespace CelSerEngine.WpfReact.ComponentControllers.PointerScanner;

public class PointerScanOptionsDto
{
    public string ScanAddress { get; set; } = "";
    public bool RequireAlignedPointers { get; set; }
    public int MaxLevel { get; set; }
    public int MaxOffset { get; set; }
}
