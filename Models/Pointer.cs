using CelSerEngine.Models.ObservableModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CelSerEngine.Models;

public partial class Pointer : ProcessMemory
{
    public string? ModuleName { get; set; }
    public string ModuleNameWithBaseOffset => $"{ModuleName} + {BaseOffset:X}";
    public List<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public IntPtr PointingTo { get; set; }
    public string OffsetsDisplayString => string.Join(", ", Offsets.Select(x => x.ToString("X")).Reverse());

    public Pointer Clone()
    {
        var clone = (Pointer)MemberwiseClone();
        clone.Offsets = clone.Offsets.ToList();

        return clone;
    }

    public ObservablePointer ToObservablePointer()
    {
        var observablePointer = new ObservablePointer(
            (ulong)BaseAddress,
            BaseOffset,
            Value,
            ScanDataType)
        {
            ModuleName = ModuleName ?? "",
            Offsets = Offsets,
            PointingTo = PointingTo,
            AddressDisplayString = $"P->{PointingTo:X}"
    };

        return observablePointer;
    }
}
