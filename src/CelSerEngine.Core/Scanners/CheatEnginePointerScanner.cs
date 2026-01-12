using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.Win32.SafeHandles;

namespace CelSerEngine.Core.Scanners;

public class CheatEnginePointerScanner : PointerScanner2
{
    private const int MaxNibble = 15; // Since a pointer is max 64Bit and 64 / 4(nibble) = 16, but since we start from 0 we need 15
    private ReversePointerTable[] _level0list = new ReversePointerTable[16];
    private PointerList? _firstPointerList = null;
    private PointerList? _lastPointerList = null;

    public CheatEnginePointerScanner(INativeApi nativeApi, PointerScanOptions pointerScanOptions) : base(nativeApi, pointerScanOptions)
    {
    }

    protected override void FindPointersInMemoryRegions(IReadOnlyList<VirtualMemoryRegion2> memoryRegions, SafeProcessHandle processHandle)
    {
        var buffer = new byte[memoryRegions.Max(x => x.MemorySize)];
        var requireAlignedPointers = PointerScanOptions.RequireAlignedPointers;
        var increaseValue = requireAlignedPointers ? 4 : 1;

        //initial scan to fetch the counts of memory
        foreach (var memoryRegion in memoryRegions.Where(x => x.ValidPointerRange))
        {
            if (!NativeApi.TryReadVirtualMemory(processHandle, memoryRegion.BaseAddress, (uint)memoryRegion.MemorySize, buffer))
                continue;

            var lastAddress = (int)memoryRegion.MemorySize - IntPtr.Size;

            for (var i = 0; i <= lastAddress; i += increaseValue)
            {
                var currentPointer = (IntPtr)BitConverter.ToUInt64(buffer, i);

                if ((!requireAlignedPointers || currentPointer % 4 == 0) && IsPointer(currentPointer, memoryRegions))
                {
                    AddPointer(currentPointer, memoryRegion.BaseAddress + i, false);
                }
            }
        }

        //actual add
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
                    AddPointer(currentPointer, memoryRegion.BaseAddress + i, true);
                }
            }
        }
    }

    private void AddPointer(IntPtr pointerValue, IntPtr pointerWithThisValue, bool add)
    {
        var plist = FindOrAddPointerValue(pointerValue, _level0list);

        if (!add)
        {
            plist.ExpectedSize += 1;
            return;
        }
        else
        {
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
    }

    private PointerList FindOrAddPointerValue(IntPtr pointerValue, ReversePointerTable[] level0list)
    {
        var currentArray = level0list;
        var nibbleIndex = 0;
        int entryNr;

        while (nibbleIndex < MaxNibble)
        {
            entryNr = GetNibble(pointerValue, nibbleIndex);

            if (currentArray[entryNr] == null)
            {
                currentArray[entryNr] = new ReversePointerTable();
            }

            if (currentArray[entryNr].ReversePointerListArray == null)
            {
                currentArray[entryNr].ReversePointerListArray = new ReversePointerTable[MaxNibble + 1];
            }

            currentArray = currentArray[entryNr].ReversePointerListArray!;
            nibbleIndex++;
        }

        entryNr = GetNibble(pointerValue, nibbleIndex);
        if (currentArray[entryNr] == null)
        {
            currentArray[entryNr] = new ReversePointerTable();
        }

        PointerList? plist = currentArray[entryNr].PointerList;
        if (plist == null)
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

            currentArray[entryNr].PointerList = plist;
            currentArray[entryNr].ReversePointerListArray = new ReversePointerTable[MaxNibble + 1];
        }

        return plist;
    }

    private int GetNibble(IntPtr address, int nibbleIndex)
    {
        return (int)((address >> ((MaxNibble - nibbleIndex) * 4)) & 0xF);
    }

    protected override void FillLinkedList()
    {
        PointerList? current = null;
        FillList(_level0list, 0, ref current);
        _lastPointerList = current;

        if (_lastPointerList == null)
        {
            throw new MissingMemberException("No memory found in the specified region");
        }
    }

    private void FillList(ReversePointerTable[] addressList, int nibbleIndex, ref PointerList? prev)
    {
        if (nibbleIndex == MaxNibble)
        {
            for (var i = 0; i <= 0xF; i++)
            {
                if (addressList[i] == null)
                {
                    continue;
                }

                var currentPointerList = addressList[i].PointerList;
                if (currentPointerList != null)
                {
                    if (prev != null)
                    {
                        prev.Next = currentPointerList;
                    }
                    else
                    {
                        _firstPointerList = currentPointerList;
                    }

                    currentPointerList.Previous = prev;
                    prev = currentPointerList;
                }
            }
        }
        else
        {
            for (var i = 0; i <= 0xF; i++)
            {
                if (addressList[i] == null)
                {
                    continue;
                }

                var reversePointerList = addressList[i].ReversePointerListArray;
                if (reversePointerList != null)
                {
                    FillList(reversePointerList, nibbleIndex + 1, ref prev);
                }
            }
        }
    }    

    internal override PointerList? FindPointerValue(nint startValue, ref nint stopValue)
    {
        var maxNibble = MaxNibble;
        var currentStopValue = stopValue;
        var currentArray = _level0list;
        PointerList? result = null;

        for (var nibbleIndex = 0; nibbleIndex <= maxNibble; nibbleIndex++)
        {
            var entryNr = GetNibble(currentStopValue, nibbleIndex);

            if (currentArray[entryNr] == null || currentArray[entryNr].ReversePointerListArray == null)
            {
                result = FindClosestPointer(currentArray, entryNr, nibbleIndex, currentStopValue);
                break;
            }
            else
            {
                if (nibbleIndex == maxNibble)
                {
                    result = currentArray[entryNr].PointerList;
                    break;
                }
            }

            currentArray = currentArray[entryNr].ReversePointerListArray;
        }

        stopValue = result?.PointerValue ?? IntPtr.Zero;

        if (stopValue < startValue)
        {
            result = null;
        }

        return result;
    }

    private PointerList? FindClosestPointer(ReversePointerTable[] addressList, int entryNr, int nibbleIndex, IntPtr maxValue)
    {
        //first try the top   
        PointerList? result = null;

        for (var i = entryNr + 1; i <= 0xF; i++) // _maxLevel
        {
            if (addressList[i] == null || addressList[i].ReversePointerListArray == null)
            {
                continue;
            }

            if (nibbleIndex == MaxNibble)
            {
                result = addressList[i].PointerList;

                while (result != null && result.PointerValue > maxValue) //should only run one time
                {
                    result = result.Previous;
                }

                if (result == null)
                {
                    result = _firstPointerList;
                }

                return result!;
            }
            else //dig deeper
            {
                result = FindClosestPointer(addressList[i].ReversePointerListArray!, -1, nibbleIndex + 1, maxValue); //so it will be found by the next top scan

                if (result != null)
                {
                    return result;
                }
            }
        }

        //nothing at the top, try the bottom
        for (var i = entryNr - 1; i >= 0; i--)
        {
            if (addressList[i] == null || addressList[i].ReversePointerListArray == null)
            {
                continue;
            }

            if (nibbleIndex == MaxNibble)
            {
                result = addressList[i].PointerList;

                while (result != null && result.PointerValue > maxValue) //should never happen 
                {
                    result = result.Previous;
                }

                if (result == null)
                {
                    result = _firstPointerList;
                }

                return result!;
            }
            else //dig deeper
            {
                result = FindClosestPointer(addressList[i].ReversePointerListArray!, 0x10, nibbleIndex + 1, maxValue); //F downto 0

                if (result != null)
                {
                    return result;
                }
            }
        }

        return result;
    }
}
