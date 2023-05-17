using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;

namespace CelSerEngine.Models.ObservableModels;

public partial class ObservablePointer : ObservableProcessMemory
{
    [ObservableProperty]
    private string _moduleName;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressDisplayString))]
    public IntPtr _pointingTo;
    public string ModuleNameWithBaseOffset => $"{ModuleName} + {BaseOffset:X}";
    public List<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public override string AddressDisplayString => $"P->{PointingTo:X}";

    public ObservablePointer(ulong baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType) : base(baseAddress, baseOffset, 0, scanDataType)
    {
        _moduleName = "No ModuleName";
    }
}
