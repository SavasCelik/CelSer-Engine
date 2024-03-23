using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static CelSerEngine.Core.Native.Enums;
using static CelSerEngine.Core.Native.Structs;

namespace CelSerEngine.Core.Scanners;
public class PointerScanner2
{
    private readonly NativeApi _nativeApi;
    private readonly nint _hProcess;
    private IList<ModuleInfo> _modules;
    private CheatEnginePointerScanStrategy _scanStrategy;

    public PointerScanner2(NativeApi nativeApi, IntPtr hProcess)
    {
        _nativeApi = nativeApi;
        _hProcess = hProcess;
        _modules = _nativeApi.GetProcessModules(_hProcess);
        _scanStrategy = new CheatEnginePointerScanStrategy();
    }

    public void StartPointerScan()
    {
        if (IntPtr.Size == 4)
            throw new NotImplementedException("32-bit is not supported yet.");

        var memoryRegions = _nativeApi
            .GatherVirtualMemoryRegions2(_hProcess)
            .Where(m =>
                !IsSystemModule(m)
                && m.State == (uint)MEMORY_STATE.MEM_COMMIT
                && (m.Type & 0x40000) == 0
                && (m.Protect & (uint)MEMORY_PROTECTION.PAGE_GUARD) == 0
                && (m.Protect & (uint)MEMORY_PROTECTION.PAGE_NOACCESS) == 0
            )
            .Select(m => new VirtualMemoryRegion2
            {
                BaseAddress = (IntPtr)m.BaseAddress,
                MemorySize = m.RegionSize,
                InModule = TryGetModule((IntPtr)m.BaseAddress, out _),
                ValidPointerRange = (m.AllocationProtect & (uint)MEMORY_PROTECTION.PAGE_WRITECOMBINE) == (uint)MEMORY_PROTECTION.PAGE_WRITECOMBINE
                    || (m.Protect & (uint)(MEMORY_PROTECTION.PAGE_READONLY | MEMORY_PROTECTION.PAGE_EXECUTE | MEMORY_PROTECTION.PAGE_EXECUTE_READ)) != 0
            })
            .ToList();

        if (memoryRegions.Count == 0)
            throw new Exception("No memory found in the specified region");

        ConcatMemoryRegions(memoryRegions);
        MakeMemoryRegionChunks(memoryRegions);
        memoryRegions.Sort((region1, region2) => region1.BaseAddress.CompareTo(region2.BaseAddress));
        var buffer = new byte[memoryRegions.Max(x => x.BaseAddress)];

        foreach (var memoryRegion in memoryRegions.Where(x => x.ValidPointerRange))
        {
            if (!_nativeApi.TryReadVirtualMemory(_hProcess, memoryRegion.BaseAddress, (uint)memoryRegion.MemorySize, buffer))
                continue;

            var lastAddress = (int)memoryRegion.MemorySize - IntPtr.Size;
            for (var i = 0; i <= lastAddress; i += 4)
            {
                var qwordPointer = (IntPtr)BitConverter.ToUInt64(buffer, i);

                if (qwordPointer % 4 == 0 && IsPointer(qwordPointer, memoryRegions))
                {
                    _scanStrategy.AddPointer(qwordPointer, memoryRegion.BaseAddress + i, false);
                }
            }
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

    private bool IsPointer(IntPtr address, List<VirtualMemoryRegion2> memoryRegions)
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
}
