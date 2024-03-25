using CelSerEngine.Core.Extensions;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
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
    public const int MaxLevel = 4;
    public const int StructSize = 4095;
    public const bool NoLoop = true;
    public const bool LimitToMaxOffsetsPerNode = true;
    public const int MaxOffsetsPerNode = 3;
    private bool _findValueInsteadOfAddress = false;

    public NativeApi NativeApi { get; }
    public IntPtr ProcessHandle { get; }
    internal PathQueueElement[] PathQueue { get; set; } = new PathQueueElement[MaxQueueSize];
    public int PathQueueLength { get; set; } = 0;
    public nint AutomaticAddress => new IntPtr(0x001A0AC8);

    public PointerScanner2(NativeApi nativeApi, IntPtr hProcess)
    {
        NativeApi = nativeApi;
        ProcessHandle = hProcess;
        _useStacks = true;
        _modules = NativeApi.GetProcessModules(ProcessHandle);
    }

    public void StartPointerScan()
    {
        if (IntPtr.Size == 4)
            throw new NotImplementedException("32-bit is not supported yet.");

        FillTheStackList();
        var memoryRegions = NativeApi
            .GatherVirtualMemoryRegions2(ProcessHandle)
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
        FindPointersInMemoryRegions(memoryRegions);
        FillLinkedList();
        InitializeEmptyPathQueue();

        var scanWorker = new PointerScanWorker(this);
        scanWorker.Start();
    }

    private void FillTheStackList()
    {
        for (int i = 0; i < _threadStacks; i++)
        {
            var threadStackStart = NativeApi.GetStackStart(ProcessHandle, i);

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

    protected bool IsPointer(IntPtr address, List<VirtualMemoryRegion2> memoryRegions)
    {
        if (address == IntPtr.Zero)
            return false;

        var index = BinSearchMemoryRegions(address, memoryRegions);

        return index != -1 && memoryRegions[index].ValidPointerRange;
    }

    private int BinSearchMemoryRegions(IntPtr address, List<VirtualMemoryRegion2> memoryRegions)
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

    protected abstract void FindPointersInMemoryRegions(List<VirtualMemoryRegion2> memoryRegions);
    protected abstract void FillLinkedList();
    internal abstract PointerList? FindPointerValue(IntPtr startValue, ref IntPtr stopValue);

    public void InitializeEmptyPathQueue()
    {
        for (var i = 0; i <= MaxQueueSize - 1; i++)
        {
            for (var j = 0; j <= MaxLevel + 1; j++)
            {
                if (PathQueue[i] == null)
                {
                    PathQueue[i] = new PathQueueElement(MaxLevel);
                }

                PathQueue[i].TempResults[j] = new IntPtr(0xcececece);
            }

            if (NoLoop)
            {
                for (var j = 0; j < MaxLevel + 1; j++)
                {
                    if (PathQueue[i] == null)
                    {
                        PathQueue[i] = new PathQueueElement(MaxLevel);
                    }
                    PathQueue[i].ValueList[j] = new UIntPtr(0xcececececececece);
                }
            }
        }

        if (MaxLevel > 0)
        {
            if (true) // if (initializer) then //don't start the scan if it's a worker system
            {
                if (!_findValueInsteadOfAddress)
                {
                    PathQueue[PathQueueLength].StartLevel = 0;
                    PathQueue[PathQueueLength].ValueToFind = AutomaticAddress;
                    PathQueueLength++;
                }
            }
        }
    }
}
