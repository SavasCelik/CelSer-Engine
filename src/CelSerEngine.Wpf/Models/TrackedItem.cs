using CommunityToolkit.Mvvm.ComponentModel;

namespace CelSerEngine.Wpf.Models;

public partial class TrackedItem : ObservableObject
{
    [ObservableProperty]
    private bool _isFreezed;
    [ObservableProperty]
    private string _description;

    public ObservableProcessMemorySegment Item { get; set; }
    public string? SetValue { get; set; }

    partial void OnIsFreezedChanged(bool value)
    {
        SetValue = value ? Item.Value : null;
    }

    public TrackedItem(ObservableProcessMemorySegment item)
    {
        Item = item;
        _description = "Description";
    }
}
