using System;
using System.Collections.Generic;
using System.Numerics;
using CelSerEngine.NativeCore;

namespace CelSerEngine
{
    public interface IVectorComparer
    {
        public Vector<byte> ComapreTo(ReadOnlySpan<byte> bytes);
        public int GetVectorSize();
        public IEnumerable<ValueAddress> GetMatchingValueAddresses(MEMORY_BASIC_INFORMATION64 page, byte[] pageValues);
    }
}
