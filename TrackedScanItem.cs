using CommunityToolkit.Mvvm.ComponentModel;
using CelSerEngine.NativeCore;

namespace CelSerEngine;

public partial class TrackedScanItem : ValueAddress
{
    [ObservableProperty]
    private bool isFreezed;
    private byte[]? setValue;
    public byte[]? SetValue
    {
        get
        {
            setValue ??= Value;
            return setValue;
        }
        set
        {
            if (value != null)
            {
                Value = value;
            }
            setValue = value;
        }
    }

    partial void OnIsFreezedChanged(bool value)
    {
        if (!value)
        {
            SetValue = null;
        }
    }

    public TrackedScanItem(ValueAddress valueAddress) : base(valueAddress)
    {
    }
}
