using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CelSerEngine.ViewModels;

public partial class PointerScanOptionsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string pointerScanAddress;

    public PointerScanOptionsViewModel()
    {
        pointerScanAddress = "";
    }

    [RelayCommand]
    public void StartPointerScan()
    {
    }

    public bool ShowPointerScanDialog(string pointerScanAddress = "")
    {
        PointerScanAddress = pointerScanAddress;
        var pointerScanOpstionsDlg = new PointerScanOptions()
        {
            Owner = App.Current.MainWindow
        };


        return pointerScanOpstionsDlg.ShowDialog() ?? false;
    }
}
