using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.Win32.SafeHandles;
using static CelSerEngine.Core.Native.Enums;

namespace CelSerEngine.Shared.Services.MemoryScan;
public class MemoryScanService : IMemoryScanService
{
    private readonly INativeApi _nativeApi;

    public MemoryScanService(INativeApi nativeApi)
    {
        _nativeApi = nativeApi;
    }

    public async Task<IList<IMemorySegment>> ScanProcessMemoryAsync(
        ScanConstraint scanConstraint,
        SafeProcessHandle processHandle,
        IProgress<float> progressUpdater,
        CancellationToken token = default)
    {
        var matchingMemories = await Task.Run(() =>
        {
            var virtualMemoryRegions = new List<VirtualMemoryRegion>();

            foreach (var memoryRegion in _nativeApi.EnumerateMemoryRegions(processHandle, scanConstraint.StartAddress, scanConstraint.StopAddress))
            {
                if (memoryRegion.State != MEMORY_STATE.MEM_COMMIT
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_GUARD)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_NOACCESS)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_WRITECOMBINE) // TODO: allow disabling this filter
                || !scanConstraint.AllowedMemoryTypes.Contains(MEMORY_TYPE.MEM_PRIVATE) && memoryRegion.Type == (uint)MEMORY_TYPE.MEM_PRIVATE
                || !scanConstraint.AllowedMemoryTypes.Contains(MEMORY_TYPE.MEM_MAPPED) && memoryRegion.Type == (uint)MEMORY_TYPE.MEM_MAPPED
                || !scanConstraint.AllowedMemoryTypes.Contains(MEMORY_TYPE.MEM_IMAGE) && memoryRegion.Type == (uint)MEMORY_TYPE.MEM_IMAGE
                )
                {
                    continue;
                }

                var isWritable = memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_READWRITE)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_WRITECOPY)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE_READWRITE)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE_WRITECOPY);

                var isExecutable = memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE_READ)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE_READWRITE)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE_WRITECOPY);

                var isCopyOnWrite = memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_WRITECOPY)
                || memoryRegion.Protect.HasFlag(MEMORY_PROTECTION.PAGE_EXECUTE_WRITECOPY);

                if (scanConstraint.IncludedProtections.HasFlag(MemoryProtections.Writable) && !isWritable
                || scanConstraint.ExcludedProtections.HasFlag(MemoryProtections.Writable) && isWritable)
                {
                    continue;
                }

                if (scanConstraint.IncludedProtections.HasFlag(MemoryProtections.Executable) && !isExecutable
                || scanConstraint.ExcludedProtections.HasFlag(MemoryProtections.Executable) && isExecutable)
                {
                    continue;
                }

                if (scanConstraint.IncludedProtections.HasFlag(MemoryProtections.CopyOnWrite) && !isCopyOnWrite
                || scanConstraint.ExcludedProtections.HasFlag(MemoryProtections.CopyOnWrite) && isCopyOnWrite)
                {
                    continue;
                }

                if (_nativeApi.TryReadVirtualMemory(processHandle, (nint)memoryRegion.BaseAddress, (uint)memoryRegion.RegionSize, out var memoryBytes))
                {
                    var virtualMemoryRegion = new VirtualMemoryRegion((nint)memoryRegion.BaseAddress, memoryRegion.RegionSize, memoryBytes);
                    virtualMemoryRegions.Add(virtualMemoryRegion);
                }
            }
            var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
            return comparer.GetMatchingMemorySegments(virtualMemoryRegions, progressUpdater, token);
        }).ConfigureAwait(false);

        return matchingMemories;
    }

    public async Task<IList<IMemorySegment>> FilterMemorySegmentsByScanConstraintAsync(
        IList<IMemorySegment> memorySegments,
        ScanConstraint scanConstraint,
        SafeProcessHandle processHandle,
        IProgress<float> progressUpdater,
        CancellationToken token = default)
    {
        // TODO: this has to be better in performance try benchmarking linkedlist and using vectorcomparer
        var filteredMemorySegments = await Task.Run(() =>
        {
            _nativeApi.UpdateAddresses(processHandle, memorySegments, token);

            var passedMemorySegments = new List<IMemorySegment>();

            for (var i = 0; i < memorySegments.Count; i++)
            {
                if (token.IsCancellationRequested)
                    break;

                if (ValueComparer.MeetsTheScanConstraint(memorySegments[i].Value, scanConstraint.UserInput, scanConstraint))
                    passedMemorySegments.Add(memorySegments[i]);
            }

            return passedMemorySegments;
        }).ConfigureAwait(false);

        return filteredMemorySegments;
    }
}
