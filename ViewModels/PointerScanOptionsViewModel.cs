using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CelSerEngine.ViewModels;

public partial class PointerScanOptionsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string pointerScanAddress;

    [RelayCommand]
    public void StartPointerScan()
    {

    }
}
