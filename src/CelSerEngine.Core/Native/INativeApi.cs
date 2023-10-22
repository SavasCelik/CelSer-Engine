using CelSerEngine.Core.Models;

namespace CelSerEngine.Core.Native;

public interface INativeApi
{
    public IntPtr OpenProcess(string processName);
    public IntPtr OpenProcess(int processId);
    public ProcessModuleInfo GetProcessMainModule(int processId);
    public byte[] ReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead);
    public void ReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer);
    public void WriteMemory(IntPtr hProcess, IMemorySegment trackedScanItem, string newValue);
    public void WriteMemory<T>(IntPtr hProcess, IntPtr memoryAddress, T newValue) where T : struct;
    public IList<VirtualMemoryRegion> GatherVirtualMemoryRegions(IntPtr hProcess);
    public void UpdateAddresses(IntPtr hProcess, IEnumerable<IMemorySegment> virtualAddresses);
}
