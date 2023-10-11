using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using CelSerEngine.Core.Models;
using CelSerEngine.Wpf.Services;

namespace CelSerEngine.Wpf.ViewModels;

//[INotifyPropertyChanged]
public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _windowTitle;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FirstScanDone))]
    private Visibility _firstScanVisibility;
    [ObservableProperty]
    private Visibility _newScanVisibility;
    [ObservableProperty]
    private string _foundItemsDisplayString;
    [ObservableProperty]
    private ScanDataType _selectedScanDataType;
    [ObservableProperty]
    private ScanCompareType _selectedScanCompareType;
    [ObservableProperty]
    private float _progressBarValue;

    private const string WindowTitleBase = "CelSer Engine";
    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly ScanResultsViewModel _scanResultsViewModel;
    private readonly IMemoryScanService _memoryScanService;
    private readonly ScriptOverviewViewModel _scriptOverviewViewModel;
    private readonly IProgress<float> _progressBarUpdater;
    public bool FirstScanDone => FirstScanVisibility == Visibility.Hidden;
    public bool Scanning { get; set; }

    public MainViewModel(SelectProcessViewModel selectProcessViewModel, ScanResultsViewModel scanResultsViewModel, IMemoryScanService memoryScanService, ScriptOverviewViewModel scriptOverviewViewModel)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _scanResultsViewModel = scanResultsViewModel;
        _memoryScanService = memoryScanService;
        _scriptOverviewViewModel = scriptOverviewViewModel;
        _windowTitle = WindowTitleBase;
        _firstScanVisibility = Visibility.Visible;
        _newScanVisibility = Visibility.Hidden;
        _foundItemsDisplayString = $"Found: 0";
        _selectedScanDataType = ScanDataType.Integer;
        _selectedScanCompareType = ScanCompareType.ExactValue;
        _progressBarValue = 0;
        _progressBarUpdater = new Progress<float>(newValue =>
        {
            ProgressBarValue = newValue;
        });
        _selectProcessViewModel.AttachToDebugGame();
    }

    private void HideFirstScanBtn()
    {
        FirstScanVisibility = Visibility.Hidden;
        NewScanVisibility = Visibility.Visible;
    }

    private void ShowFirstScanBtn()
    {
        FirstScanVisibility = Visibility.Visible;
        NewScanVisibility = Visibility.Hidden;
    }

    [RelayCommand]
    public async Task FirstScan(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        HideFirstScanBtn();
        Scanning = true;
        var scanConstraint = new ScanConstraint(SelectedScanCompareType, SelectedScanDataType, userInput);
        var processHandle = _selectProcessViewModel.GetSelectedProcessHandle();
        var foundItems = await _memoryScanService.ScanProcessMemoryAsync(scanConstraint, processHandle, _progressBarUpdater);
        AddFoundItems(foundItems);
        _progressBarUpdater.Report(0);
        Scanning = false;
    }

    [RelayCommand]
    public async Task NextScan(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        Scanning = true;
        var processHandle = _selectProcessViewModel.GetSelectedProcessHandle();
        var scanConstraint = new ScanConstraint(SelectedScanCompareType, SelectedScanDataType, userInput);
        var allItems = _scanResultsViewModel.AllScanItems;
        var foundItems = await _memoryScanService.FilterMemorySegmentsByScanConstraintAsync(allItems, scanConstraint, processHandle, _progressBarUpdater);
        AddFoundItems(foundItems);
        Scanning = false;
    }

    private void AddFoundItems(IList<IMemorySegment> foundItems)
    {
        _scanResultsViewModel.SetScanItems(foundItems);
        FoundItemsDisplayString = $"Found: {foundItems.Count.ToString("n0", new CultureInfo("en-US"))}" +
                    (foundItems.Count > ScanResultsViewModel.MaxListedScanItems ? $" (Showing: {ScanResultsViewModel.MaxListedScanItems.ToString("n0", new CultureInfo("en-US"))})" : "");
    }

    [RelayCommand]
    public void NewScan()
    {
        ShowFirstScanBtn();
        var emptyList = new List<IMemorySegment>();
        _scanResultsViewModel.SetScanItems(emptyList);
        AddFoundItems(emptyList);
        GC.Collect();
    }

    [RelayCommand]
    public void OpenSelectProcessWindow()
    {
        if (_selectProcessViewModel.ShowSelectProcessDialog())
        {
            var processHandle = _selectProcessViewModel.GetSelectedProcessHandle();
            Debug.WriteLine($"Opening Process {processHandle:X} was successful");
        }
    }

    /// <summary>
    /// Opens the Script overview.
    /// </summary>
    [RelayCommand]
    public async Task OpenScriptOverview()
    {
        await _scriptOverviewViewModel.OpenScriptOverviewAsync();
    }
}