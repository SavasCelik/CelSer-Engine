using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Services;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CelSerEngine.Wpf.ViewModels;

public partial class PointerScanResultsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private IList<Pointer> _foundPointers;
    [ObservableProperty]
    private Visibility _progressBarGridVisibility;
    private readonly PointerScanner _pointerScanner;
    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly IMemoryScanService _memoryScanService;
    private readonly INativeApi _nativeApi;
    private CancellationTokenSource? _currentCts;

    public PointerScanResultsViewModel(SelectProcessViewModel selectProcessViewModel, IMemoryScanService memoryScanService, INativeApi nativeApi)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _memoryScanService = memoryScanService;
        _nativeApi = nativeApi;
        _foundPointers = new List<Pointer>();
        _pointerScanner = new PointerScanner(_nativeApi);
    }

    public async Task StartPointerScanAsync(PointerScanOptions pointerScanOptions)
    {
        var selectedProcess = _selectProcessViewModel.SelectedProcess!;
        pointerScanOptions.ProcessId = selectedProcess.Process.Id;
        pointerScanOptions.ProcessHandle = selectedProcess.GetProcessHandle(_nativeApi);
        _currentCts = new CancellationTokenSource();
        var foundPointers = await _pointerScanner.ScanForPointersAsync(pointerScanOptions, _currentCts.Token);
        FoundPointers = foundPointers;
        ProgressBarGridVisibility = Visibility.Collapsed;
        _currentCts = null;
    }

    [RelayCommand]
    public async Task RescanScan(string nextAddress)
    {
        if (FoundPointers == null || FoundPointers.Count == 0)
            return;

        var selectedProcess = _selectProcessViewModel.SelectedProcess!;
        var processId = selectedProcess.Process.Id;
        var processHandle = selectedProcess.GetProcessHandle(_nativeApi);
        var searchedAddress = new IntPtr(long.Parse(nextAddress, NumberStyles.HexNumber));
        var foundPointers = await _pointerScanner.RescanPointersAsync(FoundPointers, processId, processHandle, searchedAddress);
        FoundPointers = foundPointers;
    }

    [RelayCommand]
    public void AddPointerToTrackedScanItem(Pointer? selectedItem)
    {
        if (selectedItem == null)
            return;

        var observablePointer = new ObservablePointer(selectedItem);
        //TODO: bad workaround for circular dependency Fix this later
        App.Current.Services.GetRequiredService<TrackedScanItemsViewModel>().TrackedScanItems.Add(new TrackedItem(observablePointer));
    }

    [RelayCommand]
    private void CancelScan()
    {
        _currentCts?.Cancel();
    }

    public void ShowPointerScanResultsDialog()
    {
        var pointerScanResultsWidnwow = new PointerScanResults();
        pointerScanResultsWidnwow.Show();
        pointerScanResultsWidnwow.Closed += delegate 
        {
            FoundPointers = new List<Pointer>();
            ProgressBarGridVisibility = Visibility.Visible;
        };
    }
}
