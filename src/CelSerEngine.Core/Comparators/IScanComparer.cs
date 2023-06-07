using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Comparators;

public interface IScanComparer
{
    public IList<IMemorySegment> GetMatchingMemorySegments(IList<VirtualMemoryRegion> virtualMemoryRegions, IProgress<float>? progressBarUpdater = null);
}
