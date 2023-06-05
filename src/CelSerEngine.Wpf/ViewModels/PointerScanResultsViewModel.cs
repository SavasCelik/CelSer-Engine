using CelSerEngine.Core.Models;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Services;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.ViewModels;

public partial class PointerScanResultsViewModel : ObservableRecipient
{
    [ObservableProperty]
    public IList<Pointer> _foundPointers;

    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly IMemoryScanService _memoryScanService;

    public PointerScanResultsViewModel(SelectProcessViewModel selectProcessViewModel, IMemoryScanService memoryScanService)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _memoryScanService = memoryScanService;
        _foundPointers = new List<Pointer>();
    }

    public async Task StartPointerScanAsync(PointerScanOptions pointerScanOptions)
    {
        var selectedProcess = _selectProcessViewModel.SelectedProcess!;
        pointerScanOptions.ProcessAdapter = selectedProcess;
        var foundPointers = await _memoryScanService.ScanForPointersAsync(pointerScanOptions);
        FoundPointers = foundPointers;
    }

    [RelayCommand]
    public async Task RescanScan(string nextAddress)
    {
        if (FoundPointers == null || FoundPointers.Count == 0)
            return;

        var selectedProcess = _selectProcessViewModel.SelectedProcess!;
        var searchedAddress = new IntPtr(long.Parse(nextAddress, NumberStyles.HexNumber));
        var foundPointers = await _memoryScanService.RescanPointers(FoundPointers, selectedProcess, searchedAddress);
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

    public void ShowPointerScanResultsDialog()
    {
        var selectProcessWidnwow = new PointerScanResults();
        selectProcessWidnwow.Show();
    }
}
