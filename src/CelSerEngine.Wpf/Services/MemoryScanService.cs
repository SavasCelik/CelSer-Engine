using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.Services;
public class MemoryScanService : IMemoryScanService
{
    public async Task<IList<IMemorySegment>> ScanProcessMemoryAsync(
        ScanConstraint scanConstraint,
        IntPtr processHandle,
        IProgress<float> progressUpdater)
    {
        var matchingMemories = await Task.Run(() =>
        {
            var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(processHandle);
            var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
            return comparer.GetMatchingValueAddresses(virtualMemoryRegions, progressUpdater);
        }).ConfigureAwait(false);

        return matchingMemories;
    }

    public async Task<IList<IMemorySegment>> FilterMemorySegmentsByScanConstraintAsync(
        IList<IMemorySegment> memorySegments,
        ScanConstraint scanConstraint,
        IntPtr processHandle,
        IProgress<float> progressUpdater)
    {
        // TODO: this has to be better in performance try benchmarking linkedlist and using vectorcomparer
        var filteredMemorySegments = await Task.Run(() =>
        {
            NativeApi.UpdateAddresses(processHandle, memorySegments);
            var passedMemorySegments = new List<IMemorySegment>();

            for (var i = 0; i < memorySegments.Count; i++)
            {
                if (ValueComparer.MeetsTheScanConstraint(memorySegments[i].Value, scanConstraint.UserInput, scanConstraint))
                    passedMemorySegments.Add(memorySegments[i]);
            }

           return passedMemorySegments;
        }).ConfigureAwait(false);

        return filteredMemorySegments;
    }
}
