using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Comparators;

public interface IScanComparer
{
    public IList<IMemorySegment> GetMatchingValueAddresses(IList<VirtualMemoryRegion> virtualMemoryRegions, IProgress<float> progressBarUpdater);
}
