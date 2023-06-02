using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.Services;
public class MemoryScanService : IMemoryScanService
{
    public async Task<IReadOnlyCollection<IProcessMemorySegment>> ScanProcessMemory(ScanConstraint scanConstraint, IntPtr processHandle, IProgress<float> progressUpdater)
    {
        var matchingMemories = await Task.Run(() =>
        {
            var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(processHandle);
            var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
            return comparer.GetMatchingValueAddresses(virtualMemoryRegions, progressUpdater);
        }).ConfigureAwait(false);

        return matchingMemories;
    }

    public async Task<IReadOnlyCollection<IProcessMemorySegment>> FilterProcessMemorySegmentsByScanConstraint(IReadOnlyCollection<IProcessMemorySegment> memorySegments, ScanConstraint scanConstraint, IntPtr processHandle, IProgress<float> progressUpdater)
    {
        var matchingMemories = await Task.Run(() =>
        {
            NativeApi.UpdateAddresses(processHandle, memorySegments);
           return memorySegments.Where(valueAddress => ValueComparer.MeetsTheScanConstraint(valueAddress.Value, scanConstraint.UserInput, scanConstraint)).ToArray();
        }).ConfigureAwait(false);

        return matchingMemories;
    }
}
