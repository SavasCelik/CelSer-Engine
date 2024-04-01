using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CelSerEngine.Wpf.ViewModels;

public partial class TrackedScanItemsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private ObservableCollection<TrackedItem> _trackedScanItems;

    private readonly DispatcherTimer _timer;
    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly PointerScanOptionsViewModel _pointerScanOptionsViewModel;
    private readonly INativeApi _nativeApi;

    public TrackedScanItemsViewModel(SelectProcessViewModel selectProcessViewModel, PointerScanOptionsViewModel pointerScanOptionsViewModel, INativeApi nativeApi)
    {
        _trackedScanItems = new ObservableCollection<TrackedItem>();
        _selectProcessViewModel = selectProcessViewModel;
        _pointerScanOptionsViewModel = pointerScanOptionsViewModel;
        _nativeApi = nativeApi;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(0.1)
        };
        _timer.Tick += UpdateTrackedScanItems;
        _timer.Start();
    }

    private async void UpdateTrackedScanItems(object? sender, EventArgs args)
    {
        if (TrackedScanItems.Count == 0)
            return;

        var pHandle = _selectProcessViewModel.GetSelectedProcessHandle();

        if (pHandle.IsInvalid)
            return;

        // TODO Maybe IMemorySegment should have an Update class? instead of calling NativeApi.UpdateAddresses.
        // Then e could check in the class its self whether it a pointer or not
        await Task.Run(() =>
        {
            var trackedScanItemsCopy = TrackedScanItems.ToArray();
            _nativeApi.UpdateAddresses(pHandle, trackedScanItemsCopy.Select(x => x.Item));
            foreach (var trackedItem in trackedScanItemsCopy.Where(x => x.IsFreezed))
            {
                _nativeApi.WriteMemory(pHandle, trackedItem.Item, trackedItem.SetValue ?? trackedItem.Item.Value);
            }
        });
    }

    [RelayCommand]
    public void DblClickedItem(DataGrid dataGrid)
    {
        if (dataGrid.CurrentColumn?.Header is not string colHeaderName)
            return;

        if (colHeaderName == nameof(TrackedItem.Item.Value))
        {
            ShowChangeValueDialog(dataGrid.SelectedItems);
        }
        else if (colHeaderName == nameof(TrackedItem.Description))
        {
            ShowChangeDescriptionDialog(dataGrid.SelectedItems);
        }
    }

    [RelayCommand]
    public void ShowChangeValueDialog(IList selectedItems)
    {
        var selectedTrackedItems = selectedItems.Cast<TrackedItem>().ToArray();

        if (ShowChangePropertyDialog(selectedTrackedItems.First().Item.Value, nameof(IMemorySegment.Value), out string newValue))
        {
            foreach (var trackedItem in selectedTrackedItems)
            {
                if (trackedItem.IsFreezed)
                {
                    trackedItem.SetValue = newValue;
                }
                trackedItem.Item.Value = newValue;
                _nativeApi.WriteMemory(_selectProcessViewModel.GetSelectedProcessHandle(), trackedItem.Item, trackedItem.SetValue ?? trackedItem.Item.Value);
            }
        }
    }

    [RelayCommand]
    public void ShowChangeDescriptionDialog(IList selectedItems)
    {
        var selectedTrackedItems = selectedItems.Cast<TrackedItem>().ToArray();

        if (ShowChangePropertyDialog(selectedTrackedItems.First().Description, nameof(TrackedItem.Description), out string newValue))
        {
            foreach (var item in selectedTrackedItems)
            {
                item.Description = newValue;
            }
        }
    }

    [RelayCommand]
    public void ShowPointerScanDialog(TrackedItem selectedItem)
    {
        _pointerScanOptionsViewModel.ShowPointerScanDialog(selectedItem.Item.AddressDisplayString);
    }

    private bool ShowChangePropertyDialog(string propertyValue, string propertyName, out string newValue)
    {
        var valueEditor = new ValueEditor(propertyName)
        {
            Owner = App.Current.MainWindow
        };
        valueEditor.SetValueTextBox(propertyValue);
        valueEditor.SetFocusTextBox();
        var dialogResult = valueEditor.ShowDialog();
        newValue = valueEditor.Value;

        return dialogResult ?? false;
    }
}
