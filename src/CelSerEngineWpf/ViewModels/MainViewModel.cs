using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CelSerEngine.Comparators;
using CelSerEngine.Extensions;
using CelSerEngine.Models;
using CelSerEngine.Native;

namespace CelSerEngine.ViewModels;

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
    private readonly IProgress<float> _progressBarUpdater;
    public bool FirstScanDone => FirstScanVisibility == Visibility.Hidden;
    public bool Scanning { get; set; }

    public MainViewModel(SelectProcessViewModel selectProcessViewModel, ScanResultsViewModel scanResultsViewModel)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _scanResultsViewModel = scanResultsViewModel;
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
        await Task.Run(() =>
        {
            var scanConstraint = new ScanConstraint(SelectedScanCompareType, SelectedScanDataType)
            {
                UserInput = userInput.ToPrimitiveDataType(SelectedScanDataType)
            };
            var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
            //var comparer = new ValueComparer(SelectedScanConstraint);
            var processHandle = _selectProcessViewModel.GetSelectedProcessHandle();
            var pages = NativeApi.GatherVirtualPages(processHandle).ToArray();
            var sw = new Stopwatch();
            sw.Start();
            var foundItems2 = comparer.GetMatchingValueAddresses(pages, _progressBarUpdater).ToList();
            sw.Stop();
            Debug.WriteLine(sw.Elapsed);
            // Slower but has visiual effect
            //foreach (var page in foundItems2)
            //{
            //    Application.Current.Dispatcher.Invoke(new Action(() =>
            //    {
            //        ScanItems.Add(page);
            //        FoundItems = "Found: " + ScanItems.Count;
            //    }));

            //}

            AddFoundItems(foundItems2);
            _progressBarUpdater.Report(0);
        });
        Scanning = false;
    }

    [RelayCommand]
    public void NextScan(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        Scanning = true;
        var processHandle = _selectProcessViewModel.GetSelectedProcessHandle();
        NativeApi.UpdateAddresses(processHandle, _scanResultsViewModel.AllScanItems);
        var scanConstraint = new ScanConstraint(SelectedScanCompareType, SelectedScanDataType)
        {
            UserInput = userInput.ToPrimitiveDataType(SelectedScanDataType)
        };
        var foundItems = _scanResultsViewModel.AllScanItems.Where(valueAddress => ValueComparer.CompareDataByScanConstraintType(valueAddress.Value, scanConstraint.UserInput, scanConstraint.ScanCompareType)).ToList();
        AddFoundItems(foundItems);
        Scanning = false;
    }

    private void AddFoundItems(List<ValueAddress> foundItems)
    {
        _scanResultsViewModel.SetScanItems(foundItems);
        FoundItemsDisplayString = $"Found: {foundItems.Count.ToString("n0", new CultureInfo("en-US"))}" +
                    (foundItems.Count > ScanResultsViewModel.MaxListedScanItems ? $" (Showing: {ScanResultsViewModel.MaxListedScanItems.ToString("n0", new CultureInfo("en-US"))})" : "");
    }

    [RelayCommand]
    public void NewScan()
    {
        ShowFirstScanBtn();
        var emptyList = new List<ValueAddress>();
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
            Debug.WriteLine("Opening Process was successfull");
        }
    }
}