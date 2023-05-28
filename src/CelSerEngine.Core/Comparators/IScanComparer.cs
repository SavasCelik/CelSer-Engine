using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Comparators;

public interface IScanComparer
{
    public IReadOnlyCollection<ProcessMemory> GetMatchingValueAddresses(IList<VirtualMemoryPage> virtualMemoryPages, IProgress<float> progressBarUpdater);
}
