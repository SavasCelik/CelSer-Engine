using System;
using System.Collections.Generic;
using System.Numerics;
using CelSerEngine.NativeCore;

namespace CelSerEngine
{
    public interface IScanComparer
    {
        public IEnumerable<ValueAddress> GetMatchingValueAddresses(ICollection<VirtualMemoryPage> virtualMemoryPages);
    }
}
