using CelSerEngine.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace CelSerEngine.Wpf.Services;
public interface IMemoryScanService
{
    public Task<IList<IMemorySegment>> ScanProcessMemoryAsync(
        ScanConstraint scanConstraint,
        IntPtr processHandle,
        IProgress<float> progressUpdater);

    public Task<IList<IMemorySegment>> FilterMemorySegmentsByScanConstraintAsync(
        IList<IMemorySegment> memorySegments,
        ScanConstraint scanConstraint,
        IntPtr processHandle,
        IProgress<float> progressUpdater);
}
