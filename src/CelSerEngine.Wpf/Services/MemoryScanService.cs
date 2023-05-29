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
            var pages = NativeApi.GatherVirtualPages(processHandle).ToArray();
            var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
            return comparer.GetMatchingValueAddresses(pages, progressUpdater);
        }).ConfigureAwait(false);

        return matchingMemories;
    }
}
