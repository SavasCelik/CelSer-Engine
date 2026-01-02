using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using System.Globalization;

namespace CelSerEngine.WpfReact.ComponentControllers.TrackedItems;

public class TrackedItemsController : ReactControllerBase, IDisposable
{
    public List<TrackedItem> Items { get; set; }

    private readonly ProcessSelectionTracker _processSelectionTracker;
    private readonly TrackedItemNotifier _trackedItemNotifier;
    private readonly ReactJsRuntime _reactJsRuntime;
    private readonly INativeApi _nativeApi;
    private readonly MainWindow _mainWindow;

    public TrackedItemsController(ProcessSelectionTracker processSelectionTracker, TrackedItemNotifier trackedItemNotifier, ReactJsRuntime reactJsRuntime, INativeApi nativeApi, MainWindow mainWindow)
    {
        Items = [];
        _processSelectionTracker = processSelectionTracker;
        _trackedItemNotifier = trackedItemNotifier;
        _reactJsRuntime = reactJsRuntime;
        _nativeApi = nativeApi;
        _mainWindow = mainWindow;

        _trackedItemNotifier.ItemAdded += AddItem;
    }

    public void UpdateItems(int[] indices, string propertyKey, string newValue)
    {
        if (indices == null || indices.Length == 0)
        {
            return;
        }

        if (string.Equals(propertyKey, nameof(MemorySegment.Value), StringComparison.InvariantCultureIgnoreCase))
        {
            foreach (var index in indices)
            {
                _nativeApi.WriteMemory(_processSelectionTracker.SelectedProcessHandle, Items[index].MemorySegment, newValue);
            }
        }
        else if (string.Equals(propertyKey, nameof(TrackedItem.Description), StringComparison.InvariantCultureIgnoreCase))
        {
            foreach (var index in indices)
            {
                Items[index].Description = newValue;
            }
        }
        else if (string.Equals(propertyKey, nameof(TrackedItem.MemorySegment.Address), StringComparison.InvariantCultureIgnoreCase))
        {
            if (!IntPtr.TryParse(newValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var newAddress))
            {
                throw new ArgumentException($"Address must be a valid hexadecimal number. {newValue}");
            }

            foreach (var index in indices)
            {
                Items[index].MemorySegment.BaseAddress = newAddress;
                Items[index].MemorySegment.BaseOffset = 0;
            }
        }
    }

    public void RemoveItems(int[] indices)
    {
        // Remove from highest index to lowest to avoid shifting issues
        foreach (var index in indices.OrderByDescending(i => i))
        {
            Items.RemoveAt(index);
        }
    }

    public void OpenPointerScanner(int selectedItemIndex)
    {
        var firstItem = Items[selectedItemIndex];
        _mainWindow.OpenPointerScanner(firstItem.MemorySegment.Address);
    }

    public object[] GetTrackedItems()
    {
        _nativeApi.UpdateAddresses(_processSelectionTracker.SelectedProcessHandle, Items.Select(x => x.MemorySegment));

        return Items.Select(x => new
        {
            Description = x.Description,
            Address = x.MemorySegment.Address.ToString("X8"),
            Value = x.MemorySegment.Value
        }).ToArray();
    }

    private async void AddItem(MemorySegment memorySegment)
    {
        Items.Add(new TrackedItem(memorySegment));
        await ItemsStateChanged();
    }

    private async Task ItemsStateChanged()
    {
        // Notify front-end about state change
        await _reactJsRuntime.InvokeVoidAsync(ComponentId, "onItemsChanged");
    }

    public void Dispose()
    {
        _trackedItemNotifier.ItemAdded -= AddItem;
    }
}
