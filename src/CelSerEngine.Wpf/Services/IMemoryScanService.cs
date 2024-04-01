using CelSerEngine.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace CelSerEngine.Wpf.Services;
public interface IMemoryScanService
{
    public Task<IList<IMemorySegment>> ScanProcessMemoryAsync(
        ScanConstraint scanConstraint,
        SafeProcessHandle processHandle,
        IProgress<float> progressUpdater,
        CancellationToken token = default);

    public Task<IList<IMemorySegment>> FilterMemorySegmentsByScanConstraintAsync(
        IList<IMemorySegment> memorySegments,
        ScanConstraint scanConstraint,
        SafeProcessHandle processHandle,
        IProgress<float> progressUpdater,
        CancellationToken token = default);
}
