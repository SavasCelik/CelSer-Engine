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

namespace CelSerEngine.ViewModels
{
    //[INotifyPropertyChanged]
    public partial class MainViewModel : ObservableRecipient
    {
        private const string WindowTitleBase = "CelSer Engine";
        [ObservableProperty]
        private string windowTitle;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FirstScanDone))]
        private Visibility firstScanVisibility = Visibility.Visible;

        [ObservableProperty]
        private Visibility newScanVisibility = Visibility.Hidden;

        public bool FirstScanDone => FirstScanVisibility == Visibility.Hidden;

        [ObservableProperty]
        private string foundItemsDisplayString = $"Found: 0";

        [ObservableProperty]
        private ScanDataType selectedScanDataType = ScanDataType.Integer;

        [ObservableProperty]
        private ScanCompareType selectedScanCompareType = ScanCompareType.ExactValue;
        [ObservableProperty]
        private float progressBarValue;

        private readonly SelectProcessViewModel _selectProcessViewModel;
        private readonly ScanResultsViewModel _scanResultsViewModel;
        private IntPtr _pHandle;
        private readonly IProgress<float> _progressBarUpdater;
        public bool Scanning { get; set; }

        public MainViewModel(SelectProcessViewModel selectProcessViewModel, ScanResultsViewModel scanResultsViewModel)
        {
            windowTitle = WindowTitleBase;
            progressBarValue = 0;
            _pHandle = IntPtr.Zero;
            _progressBarUpdater = new Progress<float>(newValue =>
            {
                ProgressBarValue = newValue;
            });

#if DEBUG
            Task.Run(() =>
            {
                while (true)
                {
                    var pHandle = NativeApi.OpenProcess("SmallGame");
                    if (pHandle != IntPtr.Zero)
                    {
                        _pHandle = pHandle;
                        Debug.WriteLine("Found Process");
                        break;
                    }
                }
            });
#endif

            _selectProcessViewModel = selectProcessViewModel;
            _scanResultsViewModel = scanResultsViewModel;
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
                var pages = NativeApi.GatherVirtualPages(_pHandle).ToArray();
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
            NativeApi.UpdateAddresses(_pHandle, _scanResultsViewModel.AllScanItems);
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
                _pHandle = _selectProcessViewModel.GetSelectedProcessHandle();
        }
    }
}