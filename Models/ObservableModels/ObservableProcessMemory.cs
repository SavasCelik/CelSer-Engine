using CelSerEngine.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CelSerEngine.Models.ObservableModels;

public partial class ObservableProcessMemory : ObservableObject, IProcessMemory
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressDisplayString))]
    private IntPtr _baseAddress;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressDisplayString))]
    public int _baseOffset;
    [ObservableProperty]
    private byte[] _memory;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValueString))]
    private dynamic _value;
    public IntPtr Address => BaseAddress + BaseOffset;
    public ScanDataType ScanDataType { get; set; }
    public string ValueString => ((object)Value).ValueToString(ScanDataType);
    public string AddressDisplayString { get; set; }

    public ObservableProcessMemory(ulong baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType)
    {
        _baseAddress = (IntPtr)baseAddress;
        _baseOffset = baseOffset;
        _value = value;
        _memory = Array.Empty<byte>();
        ScanDataType = scanDataType;
        AddressDisplayString = Address.ToString("X");
    }

    #region CommunityToolkit bug fix
    // ******************* this fixes the Bug from CommunityToolkit with dynamic datatype, where it asks for the implementation from these generated methods *************/
    partial void OnValueChanging(dynamic value) { }
    partial void OnValueChanged(dynamic value) { }
    #endregion
}
