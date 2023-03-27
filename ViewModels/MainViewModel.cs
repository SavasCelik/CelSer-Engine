using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CelSerEngine.Comparators;
using CelSerEngine.Extensions;
using CelSerEngine.Models;
using CelSerEngine.Native;
using CelSerEngine.Views;

namespace CelSerEngine.ViewModels
{
    //[INotifyPropertyChanged]
    public partial class MainViewModel : ObservableRecipient
    {
        private const string WindowTitleBase = "CelSer Engine";
        [ObservableProperty]
        private string windowTitle;
        [ObservableProperty]
        private ObservableCollection<TrackedScanItem> trackedItems;

        [ObservableProperty]
        private List<ValueAddress> scanItems;
        public List<ValueAddress> FullScanItems { get; set; }

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

        private IntPtr _pHandle;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _timer2;
        private readonly IProgress<float> _progressBarUpdater;
        public bool Scanning { get; set; }
        public MainViewModel(SelectProcessViewModel selectProcessViewModel, TrackedScanItemsViewModel trackedScanItemsViewModel)
        {
            windowTitle = WindowTitleBase;
            progressBarValue = 0;
            _pHandle = IntPtr.Zero;
            scanItems = new List<ValueAddress>();
            FullScanItems = new List<ValueAddress>();
            trackedItems = new ObservableCollection<TrackedScanItem>();
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

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += UpdateAddresses;
            _timer.Start();

            _timer2 = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            _timer2.Tick += UpdateTrackedItems;
            _timer2.Start();
            _selectProcessViewModel = selectProcessViewModel;
            _trackedScanItemsViewModel = trackedScanItemsViewModel;
        }

        private int _shownItemsStartIndex;
        private int _shownItemsLength;
        private static readonly object _locker = new();
        private readonly SelectProcessViewModel _selectProcessViewModel;
        private readonly TrackedScanItemsViewModel _trackedScanItemsViewModel;

        [RelayCommand]
        public void ScrollingFoundItems(ScrollChangedEventArgs? scrollChangedEventArgs)
        {
            if (scrollChangedEventArgs == null) 
                return;

            lock (_locker)
            {
                _shownItemsStartIndex = Convert.ToInt32(scrollChangedEventArgs.VerticalOffset);
                _shownItemsLength = Convert.ToInt32(scrollChangedEventArgs.ViewportHeight);
            }
        }

        public async void UpdateAddresses(object? sender, EventArgs? args)
        {
            if (_pHandle == IntPtr.Zero || ScanItems.Count <= 0) 
                return;

            await Task.Run(() =>
            {
                ValueAddress[]? shownItems = null;
                lock (_locker)
                {
                    if (_shownItemsStartIndex + _shownItemsLength == 0)
                    {
                        shownItems = ScanItems.Take(100).ToArray();
                    }
                    shownItems ??= ScanItems.ToArray().AsSpan().Slice(_shownItemsStartIndex, _shownItemsLength).ToArray();
                }

                NativeApi.UpdateAddresses(_pHandle, shownItems);
                Debug.WriteLine("Visible Item First:\t" + shownItems?.FirstOrDefault()?.AddressString);
                Debug.WriteLine("Visible Item Last:\t" + shownItems?.LastOrDefault()?.AddressString);
            });
        }

        public async void UpdateTrackedItems(object? sender, EventArgs args)
        {
            if (_pHandle != IntPtr.Zero && TrackedItems.Count > 0)
            {
                await Task.Run(() =>
                {
                    var trackedItemsCopy = TrackedItems.ToArray();
                    NativeApi.UpdateAddresses(_pHandle, trackedItemsCopy);
                    foreach (var item in trackedItemsCopy.Where(x => x.IsFreezed))
                    {
                        NativeApi.WriteMemory(_pHandle, item);
                    }
                });
            }
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
            NativeApi.UpdateAddresses(_pHandle, FullScanItems);
            var scanConstraint = new ScanConstraint(SelectedScanCompareType, SelectedScanDataType)
            {
                UserInput = userInput.ToPrimitiveDataType(SelectedScanDataType)
            };
            var foundItems = FullScanItems.Where(valueAddress => ValueComparer.CompareDataByScanConstraintType(valueAddress.Value, scanConstraint.UserInput, scanConstraint.ScanCompareType)).ToList();
            AddFoundItems(foundItems);
            Scanning = false;
        }

        private void AddFoundItems(List<ValueAddress> foundItems)
        {
            FullScanItems = foundItems;
            ScanItems = FullScanItems.Take(2_000_000).ToList();
            FoundItemsDisplayString = $"Found: {FullScanItems.Count.ToString("n0", new CultureInfo("en-US"))}" +
                        (foundItems.Count > 2_000_000 ? $" (Showing: {ScanItems.Count.ToString("n0", new CultureInfo("en-US"))})" : "");
        }

        [RelayCommand]
        public void NewScan()
        {
            ShowFirstScanBtn();
            ScanItems = new List<ValueAddress>();
            GC.Collect();
        }

        [RelayCommand]
        public void AddItemToTrackedItem(ValueAddress? selectedItem)
        {
            if (selectedItem == null)
                return;

            _trackedScanItemsViewModel.TrackedScanItems.Add(new TrackedScanItem(selectedItem));
        }

        [RelayCommand]
        public void OpenSelectProcessWindow()
        {
            if (_selectProcessViewModel.ShowSelectProcessDialog())
                _pHandle = _selectProcessViewModel.GetSelectedProcessHandle();
        }
    }
}