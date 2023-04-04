﻿using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CelSerEngine.ViewModels;

public partial class PointerScanOptionsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string pointerScanAddress;
    private readonly PointerScanResultsViewModel _pointerScanResultsViewModel;

    public PointerScanOptionsViewModel(PointerScanResultsViewModel pointerScanResultsViewModel)
    {
        pointerScanAddress = "";
        _pointerScanResultsViewModel = pointerScanResultsViewModel;
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
