using CelSerEngine.Extensions;
using CelSerEngine.Models;
using CelSerEngine.Native;
using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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
    private readonly DispatcherTimer _timer2;
    private readonly SelectProcessViewModel _selectProcessViewModel;

    public TrackedScanItemsViewModel(SelectProcessViewModel selectProcessViewModel)
    {
        trackedScanItems = new ObservableCollection<TrackedScanItem>();
        _timer2 = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(0.1)
        };
        _timer2.Tick += UpdateTrackedScanItems;
        _timer2.Start();

        _selectProcessViewModel = selectProcessViewModel;
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
            Owner = App.Current.MainWindow
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
                NativeApi.WriteMemory(_selectProcessViewModel.GetSelectedProcessHandle(), item);
            }
        }
    }
}
