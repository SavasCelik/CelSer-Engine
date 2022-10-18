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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using CelSerEngine.NativeCore;
using CelSerEngine.Comparators;
using CelSerEngine.Extensions;

namespace CelSerEngine
{
    //[INotifyPropertyChanged]
    public partial class MainViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private ObservableCollection<TrackedScanItem> trackedItems;

        [ObservableProperty]
        private List<ValueAddress> scanItems;
        public List<ValueAddress> FullScanItems { get; set; }

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(FirstScanDone))]
        private Visibility firstScanVisibility = Visibility.Visible;

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(FirstScanDone))]
        private Visibility newScanVisibility = Visibility.Hidden;

        public bool FirstScanDone => FirstScanVisibility == Visibility.Hidden;

        [ObservableProperty]
        private string foundItemsDisplayString = $"Found: 0";

        [ObservableProperty]
        private ScanDataType selectedScanDataType = ScanDataType.Integer;

        [ObservableProperty]
        private ScanCompareType selectedScanCompareType = ScanCompareType.ExactValue;

        private IntPtr _pHandle;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _timer2;
        public bool Scanning { get; set; }
        public MainViewModel()
        {
            _pHandle = IntPtr.Zero;
            scanItems = new List<ValueAddress>();
            FullScanItems = new List<ValueAddress>();
            trackedItems = new ObservableCollection<TrackedScanItem>();

#if TESTUI
            for (int i = 0; i < 1000; i++)
            {
                scanItems.Add(new ValueAddress
                {
                    BaseAddress = 0x100000,
                    Offset = i,
                    Value = "123"
                });

                trackedItems.Add(new ValueAddress
                {
                    BaseAddress = 0x100000,
                    Offset = i,
                    Value = "123"
                });
            }
#endif

            Task.Run(() => 
            {
                while (true)
                {
                    //var pHandle = MemManagerDInvoke2.OpenProcess("TClient");
                    var pHandle = MemManagerDInvoke2.OpenProcess("SmallGame");
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

        [ICommand]
        public void ResultScanLoaded(ListView listView)
        {
            var scrollViewer = listView.GetVisualChild<ScrollViewer>(); //Extension method
            if (scrollViewer != null)
            {
                ScrollBar? scrollBar = scrollViewer.Template.FindName("PART_VerticalScrollBar", scrollViewer) as ScrollBar;
                if (scrollBar != null)
                {
                    scrollBar.ValueChanged += Scrolling;
                }
            }
        }

        public int ShownItemsStartIndex { get; set; } = 0;
        public int ShownItemsLength { get; set; } = 0;
        private static readonly object _locker = new();

        public void Scrolling(object sender, EventArgs e)
        {
            if (ScanItems.Count > 0)
            {
                var scrollBar = (ScrollBar)sender;
                var scrollViewer = (ScrollViewer)scrollBar.TemplatedParent;
                lock (_locker)
                {
                    ShownItemsStartIndex = Convert.ToInt32(scrollViewer.VerticalOffset);
                    ShownItemsLength = Convert.ToInt32(scrollViewer.ViewportHeight);
                }
            }
        }

        public async void UpdateAddresses(object? sender, EventArgs args)
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

                    MemManagerDInvoke2.UpdateAddresses(_pHandle, shownItems);
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
                    MemManagerDInvoke2.UpdateAddresses(_pHandle, trackedItems);
                    foreach (var item in trackedItems.Where(x => x.IsFreezed))
                    {
                        MemManagerDInvoke2.WriteMemory(_pHandle, item);
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

        [ICommand]
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
                var pages = MemManagerDInvoke2.GatherVirtualPages(_pHandle).ToArray();
                var sw = new Stopwatch();
                sw.Start();
                var foundItems2 = comparer.GetMatchingValueAddresses(pages).ToList();
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
            });
            Scanning = false;
        }

        [ICommand]
        public void NextScan(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return;

            Scanning = true;
            MemManagerDInvoke2.UpdateAddresses(_pHandle, FullScanItems);
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

        [ICommand]
        public void NewScan()
        {
            ShowFirstScanBtn();
            ScanItems = new List<ValueAddress>();
            GC.Collect();
        }

        [ICommand]
        public void AddItemToTrackedItem(ValueAddress? selectedItem)
        {
            if (selectedItem == null)
                return;

            TrackedItems.Add(new TrackedScanItem(selectedItem));
        }

        [ICommand]
        public void DblClickedCell(DataGrid dataGrid)
        {
            var colName = dataGrid.CurrentColumn?.SortMemberPath;

            if (colName == null)
                return;

            var selectedItems = dataGrid.SelectedItems.Cast<TrackedScanItem>().ToArray();
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
                    MemManagerDInvoke2.WriteMemory(_pHandle, item);
                }
            }
        }
    }
}