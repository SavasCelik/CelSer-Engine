using CelSerEngine.Core.Models;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;
using System.Threading.Tasks;

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
    public async Task StartPointerScan()
    {
        var pointerScanAddress = long.Parse(PointerScanAddress, NumberStyles.HexNumber);
        var pointerScanOptions = new PointerScanOptions()
        {
            SearchedAddress = new IntPtr(pointerScanAddress),
            MaxLevel = MaxLevel,
            MaxOffset = MaxOffset,
        };
        _pointerScanResultsViewModel.ShowPointerScanResultsDialog();
        await _pointerScanResultsViewModel.StartPointerScanAsync(pointerScanOptions);
    }

    public bool ShowPointerScanDialog(string pointerScanAddress = "")
    {
        PointerScanAddress = pointerScanAddress;
        var pointerScanOpstionsDlg = new PointerScanOptionsDialog()
        {
            Owner = App.Current.MainWindow
        };

        return pointerScanOpstionsDlg.ShowDialog() ?? false;
    }
}
