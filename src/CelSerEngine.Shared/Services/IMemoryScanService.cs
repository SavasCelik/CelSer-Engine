using CelSerEngine.Core.Models;
using Microsoft.Win32.SafeHandles;

namespace CelSerEngine.Shared.Services;
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
