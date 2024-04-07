using CelSerEngine.Core.Models;
using System;

namespace CelSerEngine.Wpf.Models;

public partial class ValueAddress : ObservableMemorySegment
{
    public string? PreviousValue { get; set; }

    public ValueAddress(IntPtr baseAddress, int baseOffset, string value, ScanDataType scanDataType) : base(baseAddress, baseOffset, value, scanDataType)
    {
        AddressDisplayString = Address.ToString("X");
    }

    public ValueAddress(IMemorySegment memorySegment) : this(memorySegment.BaseAddress, memorySegment.BaseOffset, memorySegment.Value, memorySegment.ScanDataType)
    {
    }
}