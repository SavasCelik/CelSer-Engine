using CelSerEngine.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CelSerEngine.Wpf.Models;

public partial class ObservableMemorySegment : ObservableObject, IMemorySegment
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
    private string _value;
    public IntPtr Address => BaseAddress + BaseOffset;
    public ScanDataType ScanDataType { get; set; }
    public virtual string AddressDisplayString { get; set; }

    public ObservableMemorySegment(IntPtr baseAddress, int baseOffset, string value, ScanDataType scanDataType)
    {
        _baseAddress = baseAddress;
        _baseOffset = baseOffset;
        _value = value;
        _memory = Array.Empty<byte>();
        ScanDataType = scanDataType;
        AddressDisplayString = Address.ToString("X");
    }
}
