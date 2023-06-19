using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.Services;
public class MemoryScanService : IMemoryScanService
{
    private readonly INativeApi _nativeApi;

    public MemoryScanService(INativeApi nativeApi)
    {
        _nativeApi = nativeApi;
    }

    public async Task<IList<IMemorySegment>> ScanProcessMemoryAsync(
        ScanConstraint scanConstraint,
        IntPtr processHandle,
        IProgress<float> progressUpdater)
    {
        var matchingMemories = await Task.Run(() =>
        {
            var virtualMemoryRegions = _nativeApi.GatherVirtualMemoryRegions(processHandle);
            var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
            return comparer.GetMatchingMemorySegments(virtualMemoryRegions, progressUpdater);
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
            _nativeApi.UpdateAddresses(processHandle, memorySegments);
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
