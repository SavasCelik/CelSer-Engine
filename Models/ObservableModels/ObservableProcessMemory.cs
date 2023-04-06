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
    public IntPtr Address => BaseAddress + BaseOffset;
    public string AddressDisplayString { get; set; }

    public ObservableProcessMemory()
    {
        AddressDisplayString = Address.ToString("X");
        memory = Array.Empty<byte>();
    }
}
