using CelSerEngine.Extensions;
using CelSerEngine.Models;
using CelSerEngine.Native;
using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CelSerEngine.ViewModels;

public partial class TrackedScanItemsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private ObservableCollection<TrackedScanItem> trackedScanItems;
    private readonly DispatcherTimer _timer;
    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly PointerScanOptionsViewModel _pointerScanOptionsViewModel;

    public TrackedScanItemsViewModel(SelectProcessViewModel selectProcessViewModel, PointerScanOptionsViewModel pointerScanOptionsViewModel)
    {
        trackedScanItems = new ObservableCollection<TrackedScanItem>();
        _selectProcessViewModel = selectProcessViewModel;
        _pointerScanOptionsViewModel = pointerScanOptionsViewModel;
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

        if (pHandle == IntPtr.Zero)
            return;

        await Task.Run(() =>
        {
            var trackedScanItemsCopy = TrackedScanItems.ToArray();
            NativeApi.UpdateAddresses(pHandle, trackedScanItemsCopy);
            foreach (var item in trackedScanItemsCopy.Where(x => x.IsFreezed))
            {
                NativeApi.WriteMemory(pHandle, item);
            }
        });
    }

    [RelayCommand]
    public void DblClickedCell(DataGrid dataGrid)
    {
        if (dataGrid.CurrentColumn?.Header is not string colHeaderName)
            return;

        if (colHeaderName == nameof(TrackedScanItem.Value))
        {
            ShowChangeValueDialog(dataGrid.SelectedItems);
        }
        else if (colHeaderName == nameof(TrackedScanItem.Description))
        {
            ShowChangeDescriptionDialog(dataGrid.SelectedItems);
        }
    }

    [RelayCommand]
    public void ShowChangeValueDialog(IList selectedItems)
    {
        var selectedTrackedItems = selectedItems.Cast<TrackedScanItem>().ToArray();

        if (ShowChangePropertyDialog(selectedTrackedItems.First().ValueString, nameof(TrackedScanItem.Value), out string newValue))
        {
            foreach (var item in selectedTrackedItems)
            {
                if (item.IsFreezed)
                {
                    item.SetValue = newValue.ToPrimitiveDataType(item.ScanDataType);
                }
                item.Value = newValue.ToPrimitiveDataType(item.ScanDataType);
                NativeApi.WriteMemory(_selectProcessViewModel.GetSelectedProcessHandle(), item);
            }
        }
    }

    [RelayCommand]
    public void ShowChangeDescriptionDialog(IList selectedItems)
    {
        var selectedTrackedItems = selectedItems.Cast<TrackedScanItem>().ToArray();

        if (ShowChangePropertyDialog(selectedTrackedItems.First().Description, nameof(TrackedScanItem.Description), out string newValue))
        {
            foreach (var item in selectedTrackedItems)
            {
                item.Description = newValue;
            }
        }
    }

    [RelayCommand]
    public void ShowPointerScanDialog(TrackedScanItem selectedItem)
    {
        _pointerScanOptionsViewModel.ShowPointerScanDialog(selectedItem.AddressString);
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
