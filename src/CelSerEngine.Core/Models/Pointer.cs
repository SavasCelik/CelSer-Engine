﻿namespace CelSerEngine.Core.Models;

public class Pointer : ProcessMemory
{

    public string? ModuleName { get; set; }
    public string ModuleNameWithBaseOffset => $"{ModuleName} + {BaseOffset:X}";
    public List<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public IntPtr PointingTo { get; set; }
    public string OffsetsDisplayString => string.Join(", ", Offsets.Select(x => x.ToString("X")).Reverse());

    public Pointer(IntPtr baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType) : base(baseAddress, baseOffset, scanDataType)
    {
        Value = value;
    }

    public Pointer Clone()
    {
        var clone = (Pointer)MemberwiseClone();
        clone.Offsets = clone.Offsets.ToList();

        return clone;
    }
}
