namespace CelSerEngine.WpfReact.ComponentControllers.TrackedItems;

public class TrackedItemDto
{
    public required string Address { get; set; }
    public required string Description { get; set; }
    public string? Value { get; set; }
    public bool IsPointer { get; set; }
    public string? ModuleNameWithBaseOffset { get; set; }
    public string[]? Offsets { get; set; }
    public string? PointingTo { get; set; }
}
