using CommunityToolkit.Mvvm.ComponentModel;
using CelSerEngine.NativeCore;

namespace CelSerEngine;

public partial class TrackedScanItem : ValueAddress
{
    [ObservableProperty]
    private bool isFreezed;
    public dynamic? SetValue { get; set; }

    partial void OnIsFreezedChanged(bool value)
    {
        SetValue = value ? Value : null;
    }

    public TrackedScanItem(ValueAddress valueAddress) : base(valueAddress) {}
}
