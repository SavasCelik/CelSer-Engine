using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CelSerEngine.Wpf.ViewModels;

public partial class PointerScanOptionsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _pointerScanAddress;
    [ObservableProperty]
    private int _maxOffset;
    [ObservableProperty]
    private int _maxLevel;

    private readonly PointerScanResultsViewModel _pointerScanResultsViewModel;

    public PointerScanOptionsViewModel(PointerScanResultsViewModel pointerScanResultsViewModel)
    {
        _pointerScanResultsViewModel = pointerScanResultsViewModel;
        _pointerScanAddress = "";
        _maxOffset = 0x1000;
        _maxLevel = 4;
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
