namespace CelSerEngine.Core.Models;

public class Pointer : MemorySegment, IPointer
{
    public string ModuleName { get; set; }
    public string ModuleNameWithBaseOffset => $"{ModuleName} + {BaseOffset:X}";
    public IList<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public IntPtr PointingTo { get; set; }
    public string OffsetsDisplayString => string.Join(", ", Offsets.Select(x => x.ToString("X")).Reverse());

    public Pointer(IntPtr baseAddress, int baseOffset, string value, ScanDataType scanDataType) : base(baseAddress, baseOffset, value, scanDataType)
    {
        ModuleName = "No ModuleName";
    }

    public Pointer() : this(IntPtr.Zero, 0, "0", ScanDataType.Integer)
    {
    }

    public Pointer Clone()
    {
        var clone = (Pointer)MemberwiseClone();
        clone.Offsets = clone.Offsets.ToList();

        return clone;
    }
}
