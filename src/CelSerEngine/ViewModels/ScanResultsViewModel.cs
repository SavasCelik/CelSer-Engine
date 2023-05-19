using CelSerEngine.Models;
using CelSerEngine.Native;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CelSerEngine.ViewModels;

public partial class ScanResultsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private List<ValueAddress> _scanItems;

    public const int MaxListedScanItems = 2_000_000;
    public List<ValueAddress> AllScanItems { get; private set; }

    private readonly TrackedScanItemsViewModel _trackedScanItemsViewModel;
    private readonly SelectProcessViewModel _selectProcessViewModel;
    private int _shownItemsStartIndex;
    private int _shownItemsLength;
    private static readonly object _locker = new();
    private readonly DispatcherTimer _timer;

    public ScanResultsViewModel(TrackedScanItemsViewModel trackedScanItemsViewModel, SelectProcessViewModel selectProcessViewModel)
    {
        _trackedScanItemsViewModel = trackedScanItemsViewModel;
        _selectProcessViewModel = selectProcessViewModel;
        _scanItems = new List<ValueAddress>(0);
        AllScanItems = new List<ValueAddress>(0);
        _shownItemsStartIndex = 0;
        _shownItemsLength = 0;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += UpdateScanResultValues;
        _timer.Start();
    }

    public void SetScanItems(List<ValueAddress> scanItems)
    {
        AllScanItems = scanItems;
        ScanItems = AllScanItems.Take(MaxListedScanItems).ToList();
    }

    [RelayCommand]
    public void AddItemToTrackedScanItem(ValueAddress? selectedItem)
    {
        if (selectedItem == null)
            return;

        _trackedScanItemsViewModel.TrackedScanItems.Add(new TrackedItem(selectedItem));
    }


    [RelayCommand]
    public void ScrollingScanResults(ScrollChangedEventArgs? scrollChangedEventArgs)
    {
        if (scrollChangedEventArgs == null)
            return;

        lock (_locker)
        {
            _shownItemsStartIndex = Convert.ToInt32(scrollChangedEventArgs.VerticalOffset);
            _shownItemsLength = Convert.ToInt32(scrollChangedEventArgs.ViewportHeight);
        }
    }

    private async void UpdateScanResultValues(object? sender, EventArgs? args)
    {
        if (ScanItems.Count == 0)
            return;

        var pHandle = _selectProcessViewModel.GetSelectedProcessHandle();

        if (pHandle == IntPtr.Zero)
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

            NativeApi.UpdateAddresses(pHandle, shownItems);
            Debug.WriteLine("Visible Item First:\t" + shownItems?.FirstOrDefault()?.AddressDisplayString);
            Debug.WriteLine("Visible Item Last:\t" + shownItems?.LastOrDefault()?.AddressDisplayString);
        });
    }
}
