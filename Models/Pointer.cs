using System;
using System.Collections.Generic;
using System.Linq;

namespace CelSerEngine.Models;

public partial class Pointer : ProcessMemory
{
    public string? ModuleName { get; set; }
    public List<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public IntPtr PointingTo { get; set; }
    public string OffsetsDisplayString => string.Join(", ", Offsets);

    public Pointer Clone()
    {
        var clone = (Pointer)MemberwiseClone();
        clone.Offsets = clone.Offsets.ToList();

        return clone;
    }
}
