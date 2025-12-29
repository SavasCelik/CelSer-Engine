namespace CelSerEngine.WpfReact.ComponentControllers.PointerScanner;

public class PointerScanResultDto
{
    public required string ModuleNameWithBaseOffset { get; init; }
    public required string[] Offsets { get; init; }
    public required string PointingToWithValue { get; init; }
}
