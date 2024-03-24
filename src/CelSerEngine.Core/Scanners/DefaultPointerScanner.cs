using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;

namespace CelSerEngine.Core.Scanners;

internal class DefaultPointerScanner : PointerScanner2
{
    private SortedDictionary<IntPtr, PointerList> _pointerDict;
    private IntPtr[] _keyArray;

    public DefaultPointerScanner(NativeApi nativeApi, nint hProcess) : base(nativeApi, hProcess)
    {
        _pointerDict = new SortedDictionary<IntPtr, PointerList>();
        _keyArray = Array.Empty<IntPtr>();
    }

    protected override void FindPointersInMemoryRegions(List<VirtualMemoryRegion2> memoryRegions)
    {
        var buffer = new byte[memoryRegions.Max(x => x.MemorySize)];

        foreach (var memoryRegion in memoryRegions)
        {
            if (!NativeApi.TryReadVirtualMemory(ProcessHandle, memoryRegion.BaseAddress, (uint)memoryRegion.MemorySize, buffer))
                continue;

            var lastAddress = (int)memoryRegion.MemorySize - IntPtr.Size;
            for (var i = 0; i <= lastAddress; i += 4)
            {
                var currentPointer = (IntPtr)BitConverter.ToUInt64(buffer, i);

                if (currentPointer % 4 == 0 && IsPointer(currentPointer, memoryRegions))
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
        PointerList? current = null;

        foreach (var (key, value) in _pointerDict)
        {
            if (current == null)
            {
                current = value;
                continue;
            }

            current.Next = value;
            value.Previous = current;
            current = value;
        }

        _keyArray = new IntPtr[_pointerDict.Keys.Count];
        _pointerDict.Keys.CopyTo(_keyArray, 0);
    }

    internal override PointerList? FindPointerValue(nint startValue, ref nint stopValue)
    {
        var closestLowerKey = IntPtr.MaxValue;
        if (!_pointerDict.TryGetValue(startValue, out var result))
        {
            int closestLowerKeyIndex = BinarySearchClosestLowerKey(stopValue, startValue);
            if (closestLowerKeyIndex >= 0)
            {
                closestLowerKey = _keyArray[closestLowerKeyIndex];
                //_closest.Add(startValue, closestLowerKey);
            }

            if (closestLowerKey != IntPtr.MaxValue)
            {
                result = _pointerDict[closestLowerKey];
            }
        }

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
