using CelSerEngine.Core.Models;
using CelSerEngine.Models.ObservableModels;
using System;

namespace CelSerEngine.Models;

public partial class ValueAddress : ObservableProcessMemory
{
    public string? PrevoiusValue { get; set; }

    public ValueAddress(IntPtr baseAddress, int baseOffset, string value, ScanDataType scanDataType) : base(baseAddress, baseOffset, "leer", scanDataType)
    {
        AddressDisplayString = Address.ToString("X");
    }

    public ValueAddress(ProcessMemory processMemory) : this(processMemory.BaseAddress, processMemory.BaseOffset, "leer", processMemory.ScanDataType)
    {
    }
}