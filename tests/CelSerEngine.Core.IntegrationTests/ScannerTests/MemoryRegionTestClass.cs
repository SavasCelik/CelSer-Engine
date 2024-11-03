using static CelSerEngine.Core.Native.Enums;

namespace CelSerEngine.Core.IntegrationTests.ScannerTests;

public class MemoryRegionTestClass
{
    public ulong BaseAddress { get; set; }
    public ulong AllocationBase { get; set; }
    public MEMORY_PROTECTION AllocationProtect { get; set; }
    public ulong RegionSize { get; set; }
    public MEMORY_STATE State { get; set; }
    public MEMORY_PROTECTION Protect { get; set; }
    public uint Type { get; set; }
    public required byte[] Data { get; set; }
}
