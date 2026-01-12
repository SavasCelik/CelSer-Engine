namespace CelSerEngine.WpfReact.ComponentControllers.TrackedItems;

public class PointerOffsetPathsDto
{
    public string ModuleNameWithBaseOffset { get; set; }
    public string[] Offsets { get; set; }

    public PointerOffsetPathsDto(int offsetLength)
    {
        ModuleNameWithBaseOffset = "????????";
        Offsets = new string[offsetLength];
        Array.Fill(Offsets, "????????");
    }
}
