using CelSerEngine.Models.ObservableModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CelSerEngine.Models;

public partial class TrackedItem : ObservableObject
{
    [ObservableProperty]
    private bool _isFreezed;
    [ObservableProperty]
    private string _description;

    public ObservableProcessMemory Item { get; set; }
    public dynamic? SetValue { get; set; }

    partial void OnIsFreezedChanged(bool value)
    {
        SetValue = value ? Item.Value : null;
    }

    public TrackedItem(ObservableProcessMemory item)
    {
        Item = item;
        _description = "Description";
    }
}
