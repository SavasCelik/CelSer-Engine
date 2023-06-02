using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Comparators;

public interface IScanComparer
{
    public IList<IProcessMemorySegment> GetMatchingValueAddresses(IList<VirtualMemoryRegion> virtualMemoryRegions, IProgress<float> progressBarUpdater);
}
