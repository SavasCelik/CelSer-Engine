using CelSerEngine.Core.Extensions;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Transactions;
using static CelSerEngine.Core.Native.Enums;
using static CelSerEngine.Core.Native.Structs;

namespace CelSerEngine.Core.Scanners;

[StructLayout(LayoutKind.Sequential)]
internal struct ModuleInfoStruct
{
    [MarshalAs(UnmanagedType.LPStr)]
    public string Name;
    public long BaseAddress;
    public uint Size;
    public int ModuleIndex;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PointerListStruct
{
    public int MaxSize;
    public int ExpectedSize;
    public int Pos;
    public IntPtr List;

    //Linked list
    public long PointerValue;
    public IntPtr Previous;
    //public IntPtr Next;
}

internal class PointerListNative
{
    public int MaxSize { get; set; }
    public int ExpectedSize { get; set; }
    public int Pos { get; set; }
    public PointerDataStruct[]? List { get; set; }

    //Linked list
    public IntPtr PointerValue { get; set; }
    //public PointerList? Previous { get; set; }
    //public PointerList? Next { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
internal struct PointerDataStruct
{
    public long Address;
    public StaticDataStruct StaticData;
}

[StructLayout(LayoutKind.Sequential)]
internal struct StaticDataStruct
{
    [MarshalAs(UnmanagedType.I1)]
    public bool HasValue;
    public int ModuleIndex;
    public long Offset;
}

public class PsNative
{

    [DllImport("CelSerEngineCpp.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int StartPointerScan(long[] pDictkeys, PointerListStruct[] pDictValues, int count, long valueToFind, int maxLevel, int structSize);

    private readonly INativeApi _nativeApi;
    private IList<ModuleInfo> _modules;
    private int _threadStacks = 2;
    private List<IntPtr> _stackList = new(2);
    private readonly bool _useStacks;

    public PsNative(INativeApi nativeApi)
    {
        _nativeApi = nativeApi;
        _useStacks = true;
    }

    public void Start(SafeProcessHandle processHandle, long valueToFind, int maxLevel, int structSize)
    {
        _modules = _nativeApi.GetProcessModules(processHandle);
        FillTheStackList(processHandle);
        var memoryRegions = GetMemoryRegions(processHandle);
        FindPointersInMemoryRegions(memoryRegions, processHandle);
        //FillLinkedList();


        var sumOfAllPossibiliteies = _pointerDict.Sum(x => x.Value.Pos);

        var moduleStruct = _modules.Select(m => new ModuleInfoStruct { Name = m.Name, BaseAddress = m.BaseAddress, Size = m.Size, ModuleIndex = m.ModuleIndex }).ToArray();

        var pDictkeys = new long[_pointerDict.Count];
        var pDictValues = new PointerListStruct[_pointerDict.Count];
        var pointerToClear = new List<IntPtr>();

        var i = 0;
        foreach (var kvp in _pointerDict.OrderBy(x => x.Key))
        {
            pDictkeys[i] = kvp.Key;
            var val = kvp.Value;
            var toStruct = new PointerListStruct()
            {
                ExpectedSize = val.ExpectedSize,
                MaxSize = val.MaxSize,
                PointerValue = val.PointerValue,
                Pos = val.Pos
            };

            // Allocate unmanaged memory for the array
            int size = Marshal.SizeOf<PointerDataStruct>() * val.List.Length;
            IntPtr pArr = Marshal.AllocHGlobal(size);

            if (val.List.Any(v => v.StaticData.HasValue == true))
            {

            }

            // Copy array to unmanaged memory
            for (int j = 0; j < val.List.Length; j++)
            {
                IntPtr ptr = IntPtr.Add(pArr, j * Marshal.SizeOf<PointerDataStruct>());
                Marshal.StructureToPtr(val.List[j], ptr, false);
            }

            toStruct.List = pArr;
            pointerToClear.Add(pArr);

            pDictValues[i] = toStruct;
            i++;
        }

        var sw1 = Stopwatch.StartNew();
        var asd = StartPointerScan(pDictkeys, pDictValues, _pointerDict.Count, valueToFind, maxLevel, structSize);
        sw1.Stop();

        foreach (var pArr in pointerToClear)
        {
            Marshal.FreeHGlobal(pArr);
        }

    }

    private void FillTheStackList(SafeProcessHandle processHandle)
    {
        var kernel32 = _modules.FirstOrDefault(x => x.Name.Contains("kernel32.dll", StringComparison.InvariantCultureIgnoreCase));

        for (int i = 0; i < _threadStacks; i++)
        {
            var threadStackStart = _nativeApi.GetStackStart(processHandle, i, kernel32);

            if (threadStackStart == IntPtr.Zero)
            {
                _threadStacks = i;
                break;
            }

            _stackList.Add(threadStackStart);
            _modules.Add(new ModuleInfo { ModuleIndex = _modules.Count, Name = $"THREADSTACK{i}", BaseAddress = threadStackStart });
        }
    }

    private IReadOnlyList<VirtualMemoryRegion2> GetMemoryRegions(SafeProcessHandle processHandle)
    {
        var memoryRegions = _nativeApi
            .EnumerateMemoryRegions(processHandle)
            .Where(m =>
                !IsSystemModule(m)
                && m.State == MEMORY_STATE.MEM_COMMIT
                && (m.Type & 0x40000) == 0
                && !m.Protect.HasFlag(MEMORY_PROTECTION.PAGE_GUARD)
                && !m.Protect.HasFlag(MEMORY_PROTECTION.PAGE_NOACCESS)
            )
            .Select(m => new VirtualMemoryRegion2
            {
                BaseAddress = (IntPtr)m.BaseAddress,
                MemorySize = m.RegionSize,
                InModule = TryGetModule((IntPtr)m.BaseAddress, out _),
                ValidPointerRange = !m.AllocationProtect.HasFlag(MEMORY_PROTECTION.PAGE_WRITECOMBINE)
                    && !m.Protect.HasFlag(MEMORY_PROTECTION.PAGE_READONLY)
                    && !m.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE)
                    && !m.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE_READ)
            })
            .ToList();

        if (memoryRegions.Count == 0)
            throw new Exception("No memory found in the specified region");

        ConcatMemoryRegions(memoryRegions);
        MakeMemoryRegionChunks(memoryRegions);
        memoryRegions.Sort((region1, region2) => region1.BaseAddress.CompareTo(region2.BaseAddress));

        return memoryRegions;
    }

    private bool IsSystemModule(MEMORY_BASIC_INFORMATION64 memoryRegion)
    {
        return _modules.Any(x => TryGetModule((IntPtr)memoryRegion.BaseAddress, out var moduleInfo) && moduleInfo.IsSystemModule);
    }

    private bool TryGetModule(IntPtr address, [NotNullWhen(true)] out ModuleInfo? moduleInfo)
    {
        moduleInfo = _modules.FirstOrDefault(x => address >= x.BaseAddress && address < x.BaseAddress + x.Size);

        return moduleInfo != null;
    }

    private void ConcatMemoryRegions(List<VirtualMemoryRegion2> memoryRegions)
    {
        var j = 0;
        var address = memoryRegions[0].BaseAddress;
        var size = memoryRegions[0].MemorySize;
        var InModule = memoryRegions[0].InModule;
        var validPtrRange = memoryRegions[0].ValidPointerRange;

        for (var i = 1; i < memoryRegions.Count; i++)
        {
            // Only concatenate if classpointers is false, or the same type of executable field is used
            if ((memoryRegions[i].BaseAddress == address + (IntPtr)size) &&
                (memoryRegions[i].ValidPointerRange == validPtrRange) &&
                (memoryRegions[i].InModule == InModule))
            {
                size += memoryRegions[i].MemorySize;
            }
            else
            {
                memoryRegions[j].BaseAddress = address;
                memoryRegions[j].MemorySize = size;
                memoryRegions[j].InModule = InModule;
                memoryRegions[j].ValidPointerRange = validPtrRange;

                address = memoryRegions[i].BaseAddress;
                size = memoryRegions[i].MemorySize;
                InModule = memoryRegions[i].InModule;
                validPtrRange = memoryRegions[i].ValidPointerRange;
                j++;
            }
        }

        memoryRegions[j].BaseAddress = address;
        memoryRegions[j].MemorySize = size;
        memoryRegions[j].InModule = InModule;
        memoryRegions[j].ValidPointerRange = validPtrRange;
        memoryRegions.RemoveRange(j + 1, memoryRegions.Count - j - 1);
    }

    private void MakeMemoryRegionChunks(List<VirtualMemoryRegion2> memoryRegions)
    {
        const int chunkSize = 512 * 1024; // 512KB

        for (int i = 0; i < memoryRegions.Count; i++)
        {
            if (memoryRegions[i].MemorySize > chunkSize)
            {
                // Too big, so cut into pieces
                // Create new entry with 512KB less
                memoryRegions.Add(new VirtualMemoryRegion2
                {
                    BaseAddress = memoryRegions[i].BaseAddress + chunkSize,
                    MemorySize = memoryRegions[i].MemorySize - chunkSize,
                    InModule = memoryRegions[i].InModule,
                    ValidPointerRange = memoryRegions[i].ValidPointerRange
                });

                memoryRegions[i].MemorySize = chunkSize; // Set the current region to be 512KB
            }
        }
    }

    protected bool IsPointer(IntPtr address, IReadOnlyList<VirtualMemoryRegion2> memoryRegions)
    {
        if (address == IntPtr.Zero)
            return false;

        var index = BinSearchMemoryRegions(address, memoryRegions);

        return index != -1 && memoryRegions[index].ValidPointerRange;
    }

    private int BinSearchMemoryRegions(IntPtr address, IReadOnlyList<VirtualMemoryRegion2> memoryRegions)
    {
        int first = 0; // Sets the first item of the range
        int last = memoryRegions.Count - 1; // Sets the last item of the range
        bool found = false; // Initializes the Found flag (Not found yet)
        int result = -1; // Initializes the Result

        while (first <= last && !found)
        {
            // Gets the middle of the selected range
            int pivot = (first + last) / 2;

            // Compares the address with the memory region
            if ((UIntPtr)address >= (UIntPtr)memoryRegions[pivot].BaseAddress && (UIntPtr)address < (UIntPtr)memoryRegions[pivot].BaseAddress + memoryRegions[pivot].MemorySize)
            {
                found = true;
                result = pivot;
            }
            // If the item in the middle has a bigger value than
            // the searched item, then select the first half
            else if ((UIntPtr)memoryRegions[pivot].BaseAddress > (UIntPtr)address)
            {
                last = pivot - 1;
            }
            // Else select the second half
            else
            {
                first = pivot + 1;
            }
        }

        return result;
    }

    protected bool isStatic(IntPtr address, [NotNullWhen(true)] out ModuleInfo? moduleInfo)
    {
        moduleInfo = null;
        const int stackSize = 4096;
        var isStack = false;
        var moduleBaseAddress = IntPtr.Zero;

        if (_useStacks)
        {
            for (var i = 0; i <= _threadStacks - 1; i++)
            {
                if (address.InRange(_stackList[i] - stackSize, _stackList[i]))
                {
                    moduleBaseAddress = _stackList[i];
                    isStack = true;
                }
            }
        }

        if (!isStack)
        {
            //TODO this probably could just check for BaseAddress == address
            moduleBaseAddress = _modules.Where(x => address >= x.BaseAddress && address < x.BaseAddress + x.Size).Select(x => x.BaseAddress).FirstOrDefault();
        }

        if (moduleBaseAddress != IntPtr.Zero)
        {
            moduleInfo = _modules.SingleOrDefault(x => x.BaseAddress == moduleBaseAddress);
        }

        return moduleInfo != null;
    }


    //*************************************************** Ds
    private Dictionary<IntPtr, PointerListNative> _pointerDict = [];
    private IntPtr[] _keyArray = [];

    protected void FindPointersInMemoryRegions(IReadOnlyList<VirtualMemoryRegion2> memoryRegions, SafeProcessHandle processHandle)
    {
        var buffer = new byte[memoryRegions.Max(x => x.MemorySize)];

        foreach (var memoryRegion in memoryRegions)
        {
            if (!_nativeApi.TryReadVirtualMemory(processHandle, memoryRegion.BaseAddress, (uint)memoryRegion.MemorySize, buffer))
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
            plist.List = new PointerDataStruct[plist.ExpectedSize];
            plist.MaxSize = plist.ExpectedSize;
        }

        if (plist.Pos >= plist.MaxSize) //the new entry will be over the maximum. Reallocate   
        {
            //quadrupple the storage
            var newList = new PointerDataStruct[plist.MaxSize * 4];
            Array.Copy(plist.List, newList, plist.List.Length);
            plist.List = newList;
            plist.MaxSize = plist.List.Length;
        }

        if (plist.List[plist.Pos].Address == IntPtr.Zero)
        {
            plist.List[plist.Pos] = new PointerDataStruct();
        }

        plist.List[plist.Pos].Address = pointerWithThisValue;

        if (isStatic(pointerWithThisValue, out var mi))
        {
            var staticData = new StaticDataStruct()
            {
                ModuleIndex = mi.ModuleIndex,
                Offset = pointerWithThisValue - mi.BaseAddress,
                HasValue = true
            };
            plist.List[plist.Pos].StaticData = staticData;
        }

        plist.Pos++;
    }

    private PointerListNative FindOrAddPointerValue(IntPtr pointerValue)
    {
        if (!_pointerDict.TryGetValue(pointerValue, out var plist))
        {
            plist = new PointerListNative
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

    //protected void FillLinkedList()
    //{
    //    PointerList? current = null;

    //    foreach (var (key, value) in _pointerDict)
    //    {
    //        if (current == null)
    //        {
    //            current = value;
    //            continue;
    //        }

    //        current.Next = value;
    //        value.Previous = current;
    //        current = value;
    //    }

    //    _keyArray = new IntPtr[_pointerDict.Keys.Count];
    //    _pointerDict.Keys.CopyTo(_keyArray, 0);
    //}

    //internal PointerList? FindPointerValue(nint startValue, ref nint stopValue)
    //{
    //    var closestLowerKey = IntPtr.MaxValue;
    //    if (!_pointerDict.TryGetValue(stopValue, out var result))
    //    {
    //        int closestLowerKeyIndex = BinarySearchClosestLowerKey(stopValue, startValue);
    //        if (closestLowerKeyIndex >= 0)
    //        {
    //            closestLowerKey = _keyArray[closestLowerKeyIndex];
    //            //_closest.Add(startValue, closestLowerKey);
    //        }

    //        if (closestLowerKey != IntPtr.MaxValue)
    //        {
    //            result = _pointerDict[closestLowerKey];
    //        }
    //    }

    //    if (result != null)
    //        stopValue = result.PointerValue;

    //    return result;
    //}

    //private int BinarySearchClosestLowerKey(IntPtr searchedKey, IntPtr minValue)
    //{
    //    int low = 0;
    //    int high = _keyArray.Length - 1;
    //    int closestLowerIndex = -1;

    //    while (low <= high)
    //    {
    //        int mid = low + (high - low) / 2;

    //        if (_keyArray[mid] <= searchedKey)
    //        {
    //            if (_keyArray[mid] >= minValue)
    //            {
    //                closestLowerIndex = mid;
    //            }
    //            low = mid + 1;
    //        }
    //        else
    //        {
    //            high = mid - 1;
    //        }
    //    }

    //    return closestLowerIndex; // Return index of the closest lower key
    //}
}
