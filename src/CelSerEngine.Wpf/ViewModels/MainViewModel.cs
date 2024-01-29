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
using System.Threading;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    private Visibility _cancelScanVisibility;
    [ObservableProperty]
    private string _foundItemsDisplayString;
    [ObservableProperty]
    private ScanDataType _selectedScanDataType;
    [ObservableProperty]
    private ScanCompareType _selectedScanCompareType;
    [ObservableProperty]
    private float _progressBarValue;

    private const string WindowTitleBase = "CelSer Engine";
    public SelectProcessViewModel SelectProcessViewModel { get; }
    private readonly ScanResultsViewModel _scanResultsViewModel;
    private readonly IMemoryScanService _memoryScanService;
    private readonly ScriptOverviewViewModel _scriptOverviewViewModel;
    private readonly IProgress<float> _progressBarUpdater;
    private CancellationTokenSource? _scanCts;
    public bool FirstScanDone => FirstScanVisibility == Visibility.Hidden;
    public bool Scanning { get; set; }

    private const string DllFilePath = @"D:\C#_Projekte\CelSerEngine\x64\Debug\CelSerEngine.NativeDLL.dll";

    [DllImport(DllFilePath, CallingConvention = CallingConvention.Cdecl)]
    private extern static int test(CPointer[] pointerList, int number);

    public MainViewModel(SelectProcessViewModel selectProcessViewModel, ScanResultsViewModel scanResultsViewModel, IMemoryScanService memoryScanService, ScriptOverviewViewModel scriptOverviewViewModel)
    {
        SelectProcessViewModel = selectProcessViewModel;
        _scanResultsViewModel = scanResultsViewModel;
        _memoryScanService = memoryScanService;
        _scriptOverviewViewModel = scriptOverviewViewModel;
        _windowTitle = WindowTitleBase;
        _firstScanVisibility = Visibility.Visible;
        _newScanVisibility = Visibility.Hidden;
        _cancelScanVisibility = Visibility.Hidden;
        _foundItemsDisplayString = $"Found: 0";
        _selectedScanDataType = ScanDataType.Integer;
        _selectedScanCompareType = ScanCompareType.ExactValue;
        _progressBarValue = 0;
        _progressBarUpdater = new Progress<float>(newValue =>
        {
            ProgressBarValue = newValue;
        });
        SelectProcessViewModel.AttachToDebugGame();
        var list2 = new List<CPointer>()
        {
            new() { Offsets = 99 }
        };
        var aa = test(list2.ToArray(), 10);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CPointer
    {
        public int Offsets;
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
    private async Task FirstScan(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        HideFirstScanBtn();
        Scanning = true;
        _scanCts = new CancellationTokenSource();
        CancelScanVisibility = Visibility.Visible;
        var scanConstraint = new ScanConstraint(SelectedScanCompareType, SelectedScanDataType, userInput);
        var processHandle = SelectProcessViewModel.GetSelectedProcessHandle();
        var foundItems = await _memoryScanService.ScanProcessMemoryAsync(scanConstraint, processHandle, _progressBarUpdater, _scanCts.Token);
        AddFoundItems(foundItems);
        _progressBarUpdater.Report(0);
        CancelScanVisibility = Visibility.Hidden;
        _scanCts = null;
        Scanning = false;
    }

    [RelayCommand]
    private async Task NextScan(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        Scanning = true;
        var processHandle = SelectProcessViewModel.GetSelectedProcessHandle();
        var scanConstraint = new ScanConstraint(SelectedScanCompareType, SelectedScanDataType, userInput);
        var allItems = _scanResultsViewModel.AllScanItems;
        _scanCts = new CancellationTokenSource();
        CancelScanVisibility = Visibility.Visible;
        var foundItems = await _memoryScanService.FilterMemorySegmentsByScanConstraintAsync(allItems, scanConstraint, processHandle, _progressBarUpdater, _scanCts.Token);
        AddFoundItems(foundItems);
        CancelScanVisibility = Visibility.Hidden;
        _scanCts = null;
        Scanning = false;
    }

    private void AddFoundItems(IList<IMemorySegment> foundItems)
    {
        _scanResultsViewModel.SetScanItems(foundItems);
        FoundItemsDisplayString = $"Found: {foundItems.Count.ToString("n0", new CultureInfo("en-US"))}" +
                    (foundItems.Count > ScanResultsViewModel.MaxListedScanItems ? $" (Showing: {ScanResultsViewModel.MaxListedScanItems.ToString("n0", new CultureInfo("en-US"))})" : "");
    }

    [RelayCommand]
    private void NewScan()
    {
        ShowFirstScanBtn();
        var emptyList = new List<IMemorySegment>();
        _scanResultsViewModel.SetScanItems(emptyList);
        AddFoundItems(emptyList);
        GC.Collect();
    }

    [RelayCommand]
    private void OpenSelectProcessWindow()
    {
        if (SelectProcessViewModel.ShowSelectProcessDialog())
        {
            var processHandle = SelectProcessViewModel.GetSelectedProcessHandle();
            Debug.WriteLine($"Opening Process {processHandle:X} was successful");
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _scanCts?.Cancel();
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