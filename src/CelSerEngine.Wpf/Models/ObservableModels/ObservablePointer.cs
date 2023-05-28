﻿using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System;
using CelSerEngine.Core.Models;
using System.Diagnostics.CodeAnalysis;

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

    public ObservablePointer(IntPtr baseAddress, int baseOffset, string value, ScanDataType scanDataType) : base(baseAddress, baseOffset, "leer", scanDataType)
    {
        _moduleName = "No ModuleName";
    }

    public ObservablePointer(Pointer pointer) : base(pointer.BaseAddress, pointer.BaseOffset, "leer", pointer.ScanDataType)
    {
        _moduleName = pointer.ModuleName;
        _pointingTo = pointer.PointingTo;
        Offsets = pointer.Offsets;
    }
}
