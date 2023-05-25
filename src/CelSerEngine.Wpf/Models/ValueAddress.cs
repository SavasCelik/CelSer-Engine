using CelSerEngine.Core.Models;
using CelSerEngine.Models.ObservableModels;
using System;

namespace CelSerEngine.Models;

public partial class ValueAddress : ObservableProcessMemory
{
    public dynamic? PrevoiusValue { get; set; }

    public ValueAddress(IntPtr baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType) : base(baseAddress, baseOffset, 0, scanDataType)
    {
        AddressDisplayString = Address.ToString("X");
    }

    public ValueAddress(ProcessMemory processMemory) : this(processMemory.BaseAddress, processMemory.BaseOffset, 0, processMemory.ScanDataType)
    {
    }
}