using CelSerEngine.NativeCore;

namespace CelSerEngine
{
    public class VirtualMemoryPage
    {
        public MEMORY_BASIC_INFORMATION64 Page { get; set; }
        public byte[] Bytes { get; set; }

        public VirtualMemoryPage(MEMORY_BASIC_INFORMATION64 page, byte[] bytes)
        {
            Page = page;
            Bytes = bytes;
        }
    }
}
