using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;

namespace CelSerEngine.Models.ObservableModels;

public partial class ObservablePointer : ObservableProcessMemory
{
    [ObservableProperty]
    private string _moduleName;
    public string ModuleNameWithBaseOffset => $"{ModuleName} + {BaseOffset:X}";
    public List<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public IntPtr PointingTo { get; set; }

    public ObservablePointer(ulong baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType) : base(baseAddress, baseOffset, 0, scanDataType)
    {
        _moduleName = "No ModuleName";
    }
}
