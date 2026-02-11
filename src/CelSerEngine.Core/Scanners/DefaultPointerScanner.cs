using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.Win32.SafeHandles;
using System.Collections.Frozen;

namespace CelSerEngine.Core.Scanners;

public class DefaultPointerScanner : PointerScanner2
{
    private Dictionary<IntPtr, PointerList> _pointerDict;
    private FrozenDictionary<IntPtr, PointerList> _frozenPointerDict;
    private IntPtr[] _keyArray;

    public DefaultPointerScanner(INativeApi nativeApi, PointerScanOptions pointerScanOptions) : base(nativeApi, pointerScanOptions)
    {
        _pointerDict = [];
        _frozenPointerDict = FrozenDictionary<IntPtr, PointerList>.Empty;
        _keyArray = [];
    }

    protected override void FindPointersInMemoryRegions(IReadOnlyList<VirtualMemoryRegion2> memoryRegions, SafeProcessHandle processHandle)
    {
        var buffer = new byte[memoryRegions.Max(x => x.MemorySize)];
        var requireAlignedPointers = PointerScanOptions.RequireAlignedPointers;
        var increaseValue = requireAlignedPointers ? 4 : 1;

        foreach (var memoryRegion in memoryRegions)
        {
            if (!NativeApi.TryReadVirtualMemory(processHandle, memoryRegion.BaseAddress, (uint)memoryRegion.MemorySize, buffer))
                continue;

            var lastAddress = (int)memoryRegion.MemorySize - IntPtr.Size;
            for (var i = 0; i <= lastAddress; i += increaseValue)
            {
                var currentPointer = (IntPtr)BitConverter.ToUInt64(buffer, i);

                if ((!requireAlignedPointers || currentPointer % 4 == 0) && IsPointer(currentPointer, memoryRegions))
                {
                    AddPointer(currentPointer, memoryRegion.BaseAddress + i);
                }
            }
        }
    }

    private void AddPointer(IntPtr pointerValue, IntPtr pointerWithThisValue)
    {
        var plist = FindOrAddPointerValue(pointerValue);

        if (plist.List == null)
        {
            plist.List = new PointerData[plist.ExpectedSize];
            plist.MaxSize = plist.ExpectedSize;
        }

        if (plist.Pos >= plist.MaxSize) //the new entry will be over the maximum. Reallocate   
        {
            //quadrupple the storage
            var newList = new PointerData[plist.MaxSize * 4];
            Array.Copy(plist.List, newList, plist.List.Length);
            plist.List = newList;
            plist.MaxSize = plist.List.Length;
        }

        if (plist.List[plist.Pos] == null)
        {
            plist.List[plist.Pos] = new PointerData();
        }

        plist.List[plist.Pos].Address = pointerWithThisValue;

        if (isStatic(pointerWithThisValue, out var mi))
        {
            var staticData = new StaticData()
            {
                ModuleIndex = mi.ModuleIndex,
                Offset = pointerWithThisValue - mi.BaseAddress
            };
            plist.List[plist.Pos].StaticData = staticData;
        }

        plist.Pos++;
    }

    private PointerList FindOrAddPointerValue(IntPtr pointerValue)
    {
        if (!_pointerDict.TryGetValue(pointerValue, out var plist))
        {
            plist = new PointerList
            {
                PointerValue = pointerValue,
                ExpectedSize = 1
            };

            if (pointerValue % 0x10 == 0)
            {
                plist.ExpectedSize = 5;

                if (pointerValue % 0x100 == 0)
                {
                    plist.ExpectedSize = 10;

                    if (pointerValue % 0x1000 == 0)
                    {
                        plist.ExpectedSize = 20;

                        if (pointerValue % 0x10000 == 0)
                        {
                            plist.ExpectedSize = 50;
                        }
                    }
                }
            }

            _pointerDict.Add(pointerValue, plist);
        }

        return plist;
    }

    protected override void FillLinkedList()
    {
        _keyArray = _pointerDict.Keys.Order().ToArray();
        PointerList? current = null;

        foreach (var key in _keyArray)
        {
            var value = _pointerDict[key]; 

            if (current == null)
            {
                current = value;
                continue;
            }

            current.Next = value;
            value.Previous = current;
            current = value;
        }

        // use FrozenDictionary for faster read-only lookups
        _frozenPointerDict = _pointerDict.ToFrozenDictionary();
        // avoid holding duplicate references
        _pointerDict = [];
    }

    internal override PointerList? FindPointerValue(nint startValue, ref nint stopValue)
    {
        var closestLowerKey = IntPtr.MaxValue;
        if (!_frozenPointerDict.TryGetValue(stopValue, out var result))
        {
            int closestLowerKeyIndex = BinarySearchClosestLowerKey(stopValue, startValue);
            if (closestLowerKeyIndex >= 0)
            {
                closestLowerKey = _keyArray[closestLowerKeyIndex];
                //_closest.Add(startValue, closestLowerKey);
            }

            if (closestLowerKey != IntPtr.MaxValue)
            {
                result = _frozenPointerDict[closestLowerKey];
            }
        }

        if (result != null)
            stopValue = result.PointerValue;

        return result;
    }

    private int BinarySearchClosestLowerKey(IntPtr searchedKey, IntPtr minValue)
    {
        int low = 0;
        int high = _keyArray.Length - 1;
        int closestLowerIndex = -1;

        while (low <= high)
        {
            int mid = low + (high - low) / 2;

            if (_keyArray[mid] <= searchedKey)
            {
                if (_keyArray[mid] >= minValue)
                {
                    closestLowerIndex = mid;
                }
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return closestLowerIndex; // Return index of the closest lower key
    }
}
