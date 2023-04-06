using CommunityToolkit.Mvvm.ComponentModel;

namespace CelSerEngine.Models;

public partial class TrackedScanItem : ValueAddress
{
    [ObservableProperty]
    private bool isFreezed;

    [ObservableProperty]
    private string description;

    public dynamic? SetValue { get; set; }
    
    partial void OnIsFreezedChanged(bool value)
    {
        SetValue = value ? Value : null;
    }

    public TrackedScanItem(ValueAddress valueAddress) : base(valueAddress)
    {
        description = "Description";
    }

    public TrackedScanItem(ulong baseAddress, int offset, dynamic value, ScanDataType scanDataType) : base(baseAddress, offset, (object)value, scanDataType)
    {
        description = "Description";
    }
}