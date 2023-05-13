using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CelSerEngine.ViewModels;

public partial class PointerScanOptionsViewModel : ObservableRecipient
{
    private readonly PointerScanResultsViewModel _pointerScanResultsViewModel;

    [ObservableProperty]
    private string pointerScanAddress;
    [ObservableProperty]
    private int maxOffset;
    [ObservableProperty]
    private int maxLevel;

    public PointerScanOptionsViewModel(PointerScanResultsViewModel pointerScanResultsViewModel)
    {
        _pointerScanResultsViewModel = pointerScanResultsViewModel;
        pointerScanAddress = "";
        maxOffset = 0x1000;
        maxLevel = 4;
    }

    [RelayCommand]
    public void StartPointerScan()
    {
        _pointerScanResultsViewModel.StartPointerScan(this);
        _pointerScanResultsViewModel.ShowPointerScanResultsDialog();
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
