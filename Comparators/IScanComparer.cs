using System.Collections.Generic;
using CelSerEngine.Models;

namespace CelSerEngine.Comparators
{
    public interface IScanComparer
    {
        public IEnumerable<ValueAddress> GetMatchingValueAddresses(ICollection<VirtualMemoryPage> virtualMemoryPages);
    }
}
