using System;
using System.Collections.Generic;
using System.Linq;

namespace CelSerEngine.Models;

public class PointerScanResult : ProcessMemory
{
    public List<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public IntPtr PointingTo { get; set; }

    public PointerScanResult Clone()
    {
        var clone = (PointerScanResult)MemberwiseClone();
        clone.Offsets = clone.Offsets.ToList();

        return clone;
    }
}
