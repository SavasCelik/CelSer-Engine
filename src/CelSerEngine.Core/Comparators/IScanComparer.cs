using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Comparators;

public interface IScanComparer
{
    public IReadOnlyCollection<IProcessMemorySegment> GetMatchingValueAddresses(IList<VirtualMemoryRegion> virtualMemoryRegions, IProgress<float> progressBarUpdater);
}
