using System;
using System.Collections.Generic;
using System.Linq;

namespace CelSerEngine.Models;

public class Pointer : ProcessMemory
{
    public List<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public IntPtr PointingTo { get; set; }

    public Pointer Clone()
    {
        var clone = (Pointer)MemberwiseClone();
        clone.Offsets = clone.Offsets.ToList();

        return clone;
    }
}
