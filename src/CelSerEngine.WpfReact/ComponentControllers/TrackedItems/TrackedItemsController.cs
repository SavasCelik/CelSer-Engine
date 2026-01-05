using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.Win32.SafeHandles;
using System.Data;
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
    private IList<ModuleInfo>? _processModulesCache;

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

    public void UpdateItem(int index, TrackedItemDto updatedTrackedItem)
    {
        var trackedItem = Items[index];
        trackedItem.Description = updatedTrackedItem.Description;

        if (trackedItem.MemorySegment is Pointer trackedPointerItem)
        {
            trackedPointerItem.Offsets = updatedTrackedItem.Offsets?.Select(x => IntPtr.Parse(x, NumberStyles.HexNumber)).ToArray() ?? [];
            // TODO: allow editing module and module offset
            //trackedPointerItem.ModuleNameWithBaseOffset = updatedTrackedItem.ModuleNameWithBaseOffset ?? "";
        }
        else
        {
            IntPtr.TryParse(updatedTrackedItem.Address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var newAddressAsIntPtr);
            trackedItem.MemorySegment.BaseAddress = newAddressAsIntPtr;
            trackedItem.MemorySegment.BaseOffset = 0;
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

        return Items.Select(x => {
            var pointer = x.MemorySegment as Pointer;

            return new
            {
                Description = x.Description,
                Address = x.MemorySegment.Address.ToString("X8"),
                Value = x.MemorySegment.Value,
                IsPointer = pointer != null,
                ModuleNameWithBaseOffset = pointer?.ModuleNameWithBaseOffset,
                Offsets = pointer?.Offsets.Select(x => x.ToString("X")).Reverse().ToArray() ?? [],
                PointingTo = pointer?.PointingTo.ToString("X8"),
            };
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

    public PointerOffsetPathsDto GetPointerOffsetPaths(string baseModule, string[] offsets)
    {
        var processHandle = _processSelectionTracker.SelectedProcessHandle;
        var pointingToInfo = new PointerOffsetPathsDto(offsets.Length);

        if (!TryParseBaseModule(baseModule, out var moduleName, out var signedBaseOffset))
            return pointingToInfo;

        var module = ResolveModule(processHandle, moduleName);

        if (module == null)
            return pointingToInfo;

        var pointerBaseAddress = module.BaseAddress + signedBaseOffset;
        var buffer = new byte[IntPtr.Size];

        if (!TryReadPointer(processHandle, pointerBaseAddress, buffer, out var currentPointer))
            return pointingToInfo;

        pointingToInfo.ModuleNameWithBaseOffset = currentPointer.ToString("X");

        for (var i = 0; i < offsets.Length; i++)
        {
            var offsetString = offsets[i];

            if (!IntPtr.TryParse(offsetString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var offset))
                break;

            if (i == offsets.Length - 1)
            {
                currentPointer += offset;
                pointingToInfo.Offsets[i] = currentPointer.ToString("X8");
                break;
            }

            if (!TryReadPointer(processHandle, currentPointer + offset, buffer, out currentPointer))
                break;

            pointingToInfo.Offsets[i] = currentPointer.ToString("X8");
        }

        return pointingToInfo;
    }

    private static bool TryParseBaseModule(string input, out string moduleName, out IntPtr signedBaseOffset)
    {
        moduleName = string.Empty;
        signedBaseOffset = IntPtr.Zero;

        var firstQuote = input.IndexOf('"');
        var lastQuote = input.LastIndexOf('"');

        if (firstQuote < 0 || lastQuote <= firstQuote)
            return false; 

        moduleName = input[(firstQuote + 1)..lastQuote].Trim();

        var offsetPart = input[(lastQuote + 1)..].Replace(" ", "");

        if (string.IsNullOrEmpty(offsetPart))
            return false;

        var sign = offsetPart[0] == '-' ? -1 : 1;
        var hexPart = offsetPart.TrimStart('+', '-');

        if (!IntPtr.TryParse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var offset))
            return false;

        signedBaseOffset = sign * offset;

        return true;
    }

    private ModuleInfo? ResolveModule(SafeProcessHandle processHandle, string moduleName)
    {
        _processModulesCache ??= _nativeApi.GetProcessModules(processHandle);

        var module = _processModulesCache
            .FirstOrDefault(m => m.ShortName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));

        if (module != null)
            return module;

        const string threadStackPrefix = "THREADSTACK";

        if (!moduleName.StartsWith(threadStackPrefix))
            return null;

        if (!int.TryParse(moduleName[threadStackPrefix.Length..], out var stackNumber))
            return null;

        var kernel32 = _processModulesCache
            .FirstOrDefault(m => m.ShortName.Equals("kernel32.dll", StringComparison.OrdinalIgnoreCase));

        if (kernel32 == null)
            return null;

        var stackStartPtr = _nativeApi.GetStackStart(processHandle, stackNumber, kernel32);

        var stackModule = new ModuleInfo
        {
            Name = moduleName,
            BaseAddress = stackStartPtr,
            ModuleIndex = _processModulesCache.Count
        };

        _processModulesCache.Add(stackModule);

        return stackModule;
    }

    private bool TryReadPointer(SafeProcessHandle processHandle, IntPtr address, byte[] buffer, out IntPtr value)
    {
        value = IntPtr.Zero;

        if (!_nativeApi.TryReadVirtualMemory(processHandle, address, (uint)buffer.Length, buffer))
            return false;

        value = (IntPtr)BitConverter.ToInt64(buffer);

        return true;
    }

    public void Dispose()
    {
        _trackedItemNotifier.ItemAdded -= AddItem;
    }
}
