using CelSerEngine.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CelSerEngine.Models.ObservableModels;

public partial class ObservableProcessMemory : ObservableObject, IProcessMemory
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressDisplayString))]
    private IntPtr baseAddress;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddressDisplayString))]
    public int baseOffset;
    [ObservableProperty]
    private byte[] memory;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValueString))]
    private dynamic value;
    public IntPtr Address => BaseAddress + BaseOffset;
    public ScanDataType ScanDataType { get; set; }
    public string ValueString => ((object)Value).ValueToString(ScanDataType);
    public string AddressDisplayString { get; set; }

    public ObservableProcessMemory(ulong baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType)
    {
        this.baseAddress = (IntPtr)baseAddress;
        this.baseOffset = baseOffset;
        this.value = value;
        memory = Array.Empty<byte>();
        ScanDataType = scanDataType;
        AddressDisplayString = Address.ToString("X");
    }

    #region CommunityToolkit bug fix
    // ******************* this fixes the Bug from CommunityToolkit with dynamic datatype, where it asks for the implementation from these generated methods *************/
    partial void OnValueChanging(dynamic value) { }
    partial void OnValueChanged(dynamic value) { }
    #endregion
}
