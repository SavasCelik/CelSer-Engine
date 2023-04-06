using CelSerEngine.Extensions;
using CelSerEngine.Models.ObservableModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CelSerEngine.Models;

public partial class ValueAddress : ObservableProcessMemory
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValueString))]
    private dynamic value;
    public dynamic? PrevoiusValue { get; set; }
    public ScanDataType ScanDataType { get; private set; }
    public string ValueString => ((object)Value).ValueToString(ScanDataType);

    public ValueAddress(ulong baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType)
    {
        BaseAddress = (IntPtr)baseAddress;
        BaseOffset = baseOffset;
        this.value = value;
        ScanDataType = scanDataType;
        AddressDisplayString = Address.ToString("X");
    }

    public ValueAddress(ValueAddress valueAddress)
    {
        BaseAddress = valueAddress.BaseAddress;
        BaseOffset = valueAddress.BaseOffset;
        value = valueAddress.Value;
        ScanDataType = valueAddress.ScanDataType;
        AddressDisplayString = Address.ToString("X");
    }

    #region CommunityToolkit bug fix
    // ******************* this fixes the Bug from CommunityToolkit with dynamic datatype, where it asks for the implementation from these generated methods *************/
    partial void OnValueChanging(dynamic value) {}
    partial void OnValueChanged(dynamic value) {}
    #endregion

}