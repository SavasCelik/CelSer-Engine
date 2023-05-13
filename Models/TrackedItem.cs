using CelSerEngine.Models.ObservableModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CelSerEngine.Models;

public partial class TrackedItem : ObservableObject
{
    public ObservableProcessMemory Item { get; set; }
    [ObservableProperty]
    private bool isFreezed;

    [ObservableProperty]
    private string description;

    public dynamic? SetValue { get; set; }

    partial void OnIsFreezedChanged(bool value)
    {
        SetValue = value ? Item.Value : null;
    }

    public TrackedItem(ObservableProcessMemory item)
    {
        Item = item;
        description = "Description";
    }
}
