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
        [NotifyPropertyChangedFor(nameof(FirstScanDone))]
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
        public MainViewModel()
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
        }

        public int ShownItemsStartIndex { get; set; } = 0;
        public int ShownItemsLength { get; set; } = 0;
        private static readonly object _locker = new();

        [RelayCommand]
        public void ScrollingFoundItems(ScrollChangedEventArgs scrollChangedEventArgs)
        {
            if (scrollChangedEventArgs != null)
            {
                lock (_locker)
                {
                    ShownItemsStartIndex = Convert.ToInt32(scrollChangedEventArgs.VerticalOffset);
                    ShownItemsLength = Convert.ToInt32(scrollChangedEventArgs.ViewportHeight);
                }
            }
        }

        public async void UpdateAddresses(object? sender, EventArgs? args)
        {
            if (_pHandle != IntPtr.Zero && ScanItems.Count > 0)
            {
                await Task.Run(() =>
                {
                    ValueAddress[]? shownItems = null;
                    lock (_locker)
                    {
                        if (ShownItemsStartIndex + ShownItemsLength == 0)
                        {
                            shownItems = ScanItems.Take(100).ToArray();
                        }
                        shownItems ??= ScanItems.ToArray().AsSpan().Slice(ShownItemsStartIndex, ShownItemsLength).ToArray();
                    }

                    NativeApi.UpdateAddresses(_pHandle, shownItems);
                    Debug.WriteLine("Visible Item First:\t" + shownItems?.FirstOrDefault()?.AddressString);
                    Debug.WriteLine("Visible Item Last:\t" + shownItems?.LastOrDefault()?.AddressString);
                });
            }
        }

        public async void UpdateTrackedItems(object? sender, EventArgs args)
        {
            if (_pHandle != IntPtr.Zero && TrackedItems.Count > 0)
            {
                await Task.Run(() =>
                {
                    var trackedItems = TrackedItems.ToArray();
                    NativeApi.UpdateAddresses(_pHandle, trackedItems);
                    foreach (var item in trackedItems.Where(x => x.IsFreezed))
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

            TrackedItems.Add(new TrackedScanItem(selectedItem));
        }

        [RelayCommand]
        public void OpenSelectProcessWindow()
        {
            var selectProcessWidnwow = new SelectProcess
            {
                Owner = Application.Current.MainWindow
            };
            selectProcessWidnwow.ShowDialog();
            var selectProcessViewModel = selectProcessWidnwow.DataContext as SelectProcessViewModel;
            var selectedProcess = selectProcessViewModel?.SelectedProcess?.Process;

            if (selectedProcess != null)
            {
                var pHandle = NativeApi.OpenProcess(selectedProcess.Id);
                
                if (pHandle != IntPtr.Zero)
                {
                    _pHandle = pHandle;
                    WindowTitle = $"{WindowTitleBase} - {selectedProcess.ProcessName}";
                }
            }
        }

        [RelayCommand]
        public void DblClickedCell(DataGrid dataGrid)
        {
            var colHeaderName = dataGrid.CurrentColumn?.Header as string;

            if (colHeaderName == null)
                return;

            var selectedItems = dataGrid.SelectedItems.Cast<TrackedScanItem>().ToArray();

            if (colHeaderName == nameof(TrackedScanItem.Value))
            {
                DoubleClickOnValueCell(selectedItems);
            }
        }

        private void DoubleClickOnValueCell(TrackedScanItem[] selectedItems)
        {
            var valueEditor = new ValueEditor
            {
                Owner = Application.Current.MainWindow
            };
            valueEditor.SetValueTextBox(selectedItems.First().ValueString);
            valueEditor.SetFocusTextBox();
            var dialogResult = valueEditor.ShowDialog();

            if (dialogResult ?? false)
            {
                var value = valueEditor.Value;
                foreach (var item in selectedItems)
                {
                    if (item.IsFreezed)
                    {
                        item.SetValue = value.ToPrimitiveDataType(item.ScanDataType);
                    }
                    item.Value = value.ToPrimitiveDataType(item.ScanDataType);
                    NativeApi.WriteMemory(_pHandle, item);
                }
            }
        }
    }
}