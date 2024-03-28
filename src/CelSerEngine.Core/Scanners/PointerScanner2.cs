﻿using CelSerEngine.Core.Extensions;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static CelSerEngine.Core.Native.Enums;
using static CelSerEngine.Core.Native.Structs;

namespace CelSerEngine.Core.Scanners;

public abstract class PointerScanner2
{
    private readonly bool _useStacks;
    private IList<ModuleInfo> _modules;
    private int _threadStacks = 2;
    private List<IntPtr> _stackList = new(2);
    public const int MaxQueueSize = 64;
    public const bool NoLoop = true;
    public const bool LimitToMaxOffsetsPerNode = true;
    public const int MaxOffsetsPerNode = 3;
    private bool _findValueInsteadOfAddress = false;
    private Dictionary<IntPtr, IntPtr> _pointerByMemoryAddress = new();

    public INativeApi NativeApi { get; }
    public PointerScanOptions PointerScanOptions { get; init; }
    internal PathQueueElement[] PathQueue { get; set; } = new PathQueueElement[MaxQueueSize];
    public int PathQueueLength { get; set; } = 0;

    public PointerScanner2(INativeApi nativeApi, PointerScanOptions pointerScanOptions)
    {
        NativeApi = nativeApi;
        PointerScanOptions = pointerScanOptions;
        _useStacks = true;
        _modules = new List<ModuleInfo>();
    }

    public async Task<IList<Pointer>> StartPointerScanAsync(SafeProcessHandle processHandle , CancellationToken cancellationToken = default)
    {
        if (IntPtr.Size == 4)
            throw new NotImplementedException("32-bit is not supported yet.");

        _modules = NativeApi.GetProcessModules(processHandle);
        FillTheStackList(processHandle);
        var memoryRegions = GetMemoryRegions(processHandle);
        FindPointersInMemoryRegions(memoryRegions, processHandle);
        FillLinkedList();
        InitializeEmptyPathQueue();

        var foundPointers = await Task.Factory.StartNew(() =>
        {
            var scanWorker = new PointerScanWorker(this, cancellationToken);
            var scanResult = scanWorker.Start();
            var foundPointers = scanResult
                .Select(x => new Pointer
                {
                    ModuleName = _modules[x.ModuleIndex].Name,
                    BaseAddress = _modules[x.ModuleIndex].BaseAddress,
                    BaseOffset = _stackList.Contains(_modules[x.ModuleIndex].BaseAddress) ? (int)~x.Offset + 1 : (int)x.Offset,
                    Offsets = x.TempResults
                }).ToList();

            return foundPointers;
        }, cancellationToken);
        

        return foundPointers;
    }

    public async Task<IList<Pointer>> RescanPointersAsync(IEnumerable<Pointer> firstScanPointers, IntPtr searchedAddress, SafeProcessHandle processHandle)
    {
        var memoryRegions = GetMemoryRegions(processHandle);
        var buffer = new byte[memoryRegions.Max(x => x.MemorySize)];

        foreach (var memoryRegion in memoryRegions)
        {
            if (!NativeApi.TryReadVirtualMemory(processHandle, memoryRegion.BaseAddress, (uint)memoryRegion.MemorySize, buffer))
                continue;

            var lastAddress = (int)memoryRegion.MemorySize - IntPtr.Size;
            for (var i = 0; i <= lastAddress; i += 4)
            {
                var currentPointer = (IntPtr)BitConverter.ToUInt64(buffer, i);

                if (currentPointer % 4 == 0 && IsPointer(currentPointer, memoryRegions))
                {
                    var memoryAddress = memoryRegion.BaseAddress + i;
                    _pointerByMemoryAddress.Add(memoryAddress, currentPointer);
                }
            }
        }

        return await FilterPointersAsync(firstScanPointers, searchedAddress);
    }

    private IReadOnlyList<VirtualMemoryRegion2> GetMemoryRegions(SafeProcessHandle processHandle)
    {
        var memoryRegions = NativeApi
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

    private async Task<IList<Pointer>> FilterPointersAsync(IEnumerable<Pointer> firstScanPointers, IntPtr searchedAddress)
    {
        var foundPointer = Task.Factory.StartNew(() =>
        {
            var foundPointer = new ConcurrentBag<Pointer>();
            firstScanPointers.AsParallel().ForAll(x =>
            {
                var currentAddress = x.BaseAddress + x.BaseOffset;
                IntPtr currentPointer;

                if (!_pointerByMemoryAddress.TryGetValue(currentAddress, out currentPointer))
                    return;

                for (var i = x.Offsets.Count - 1; i >= 0; i--)
                {
                    currentAddress = currentPointer + x.Offsets[i];

                    if (!_pointerByMemoryAddress.TryGetValue(currentAddress, out currentPointer))
                        break;
                }

                if (currentAddress == searchedAddress)
                    foundPointer.Add(x);
            });

            return foundPointer.ToList();
        });

        return await foundPointer;
    }

    private void FillTheStackList(SafeProcessHandle processHandle)
    {
        var kernel32 = _modules.FirstOrDefault(x => x.Name.Contains("kernel32.dll", StringComparison.InvariantCultureIgnoreCase));

        for (int i = 0; i < _threadStacks; i++)
        {
            var threadStackStart = NativeApi.GetStackStart(processHandle, i, kernel32);

            if (threadStackStart == IntPtr.Zero)
            {
                _threadStacks = i;
                break;
            }

            _stackList.Add(threadStackStart);
            _modules.Add(new ModuleInfo { ModuleIndex = _modules.Count, Name = $"THREADSTACK{i}", BaseAddress = threadStackStart });
        }
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

    protected abstract void FindPointersInMemoryRegions(IReadOnlyList<VirtualMemoryRegion2> memoryRegions, SafeProcessHandle processHandle);
    protected abstract void FillLinkedList();
    internal abstract PointerList? FindPointerValue(IntPtr startValue, ref IntPtr stopValue);

    public void InitializeEmptyPathQueue()
    {
        for (var i = 0; i <= MaxQueueSize - 1; i++)
        {
            for (var j = 0; j <= PointerScanOptions.MaxLevel + 1; j++)
            {
                if (PathQueue[i] == null)
                {
                    PathQueue[i] = new PathQueueElement(PointerScanOptions.MaxLevel);
                }

                PathQueue[i].TempResults[j] = new IntPtr(0xcececece);
            }

            if (NoLoop)
            {
                for (var j = 0; j < PointerScanOptions.MaxLevel + 1; j++)
                {
                    if (PathQueue[i] == null)
                    {
                        PathQueue[i] = new PathQueueElement(PointerScanOptions.MaxLevel);
                    }
                    PathQueue[i].ValueList[j] = new UIntPtr(0xcececececececece);
                }
            }
        }

        if (PointerScanOptions.MaxLevel > 0)
        {
            if (true) // if (initializer) then //don't start the scan if it's a worker system
            {
                if (!_findValueInsteadOfAddress)
                {
                    PathQueue[PathQueueLength].StartLevel = 0;
                    PathQueue[PathQueueLength].ValueToFind = PointerScanOptions.SearchedAddress;
                    PathQueueLength++;
                }
            }
        }
    }
}
