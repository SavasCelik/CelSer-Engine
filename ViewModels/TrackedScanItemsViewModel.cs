using CelSerEngine.Extensions;
using CelSerEngine.Models;
using CelSerEngine.Native;
using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace CelSerEngine.ViewModels;

public partial class TrackedScanItemsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private ObservableCollection<TrackedScanItem> trackedScanItems;

    public TrackedScanItemsViewModel()
    {
        trackedScanItems = new ObservableCollection<TrackedScanItem>();
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
                NativeApi.WriteMemory(IntPtr.Zero, item);
            }
        }
    }
}
