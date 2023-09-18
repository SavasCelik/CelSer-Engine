﻿using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Wpf.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CelSerEngine.Wpf.ViewModels;

public partial class ScanResultsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private IList<ValueAddress> _scanItems;

    public const int MaxListedScanItems = 2_000_000;
    public IList<IMemorySegment> AllScanItems { get; private set; }

    private readonly TrackedScanItemsViewModel _trackedScanItemsViewModel;
    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly INativeApi _nativeApi;
    private int _shownItemsStartIndex;
    private int _shownItemsLength;
    private static readonly object s_locker = new();
    private readonly DispatcherTimer _timer;

    public ScanResultsViewModel(TrackedScanItemsViewModel trackedScanItemsViewModel, SelectProcessViewModel selectProcessViewModel, INativeApi nativeApi)
    {
        _trackedScanItemsViewModel = trackedScanItemsViewModel;
        _selectProcessViewModel = selectProcessViewModel;
        _nativeApi = nativeApi;
        _scanItems = new List<ValueAddress>();
        AllScanItems = new List<IMemorySegment>();
        _shownItemsStartIndex = 0;
        _shownItemsLength = 0;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += UpdateScanResultValues;
        _timer.Start();
    }

    public void SetScanItems(IList<IMemorySegment> scanItems)
    {
        AllScanItems = scanItems;
        ScanItems = AllScanItems.Take(MaxListedScanItems).Select(x => new ValueAddress(x)).ToList();
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

        lock (s_locker)
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
            lock (s_locker)
            {
                if (_shownItemsStartIndex + _shownItemsLength == 0)
                {
                    shownItems = ScanItems.Take(100).ToArray();
                }
                shownItems ??= ScanItems.ToArray().AsSpan().Slice(_shownItemsStartIndex, _shownItemsLength).ToArray();
            }

            _nativeApi.UpdateAddresses(pHandle, shownItems);
            Debug.WriteLine("Visible Item First:\t" + shownItems?.FirstOrDefault()?.AddressDisplayString);
            Debug.WriteLine("Visible Item Last:\t" + shownItems?.LastOrDefault()?.AddressDisplayString);
        });
    }
}
