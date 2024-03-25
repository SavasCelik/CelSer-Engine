using CelSerEngine.Core.Models;
using static CelSerEngine.Core.Native.Structs;

namespace CelSerEngine.Core.Native;

public interface INativeApi
{
    public IntPtr OpenProcess(string processName);
    public IntPtr OpenProcess(int processId);
    public ProcessModuleInfo GetProcessMainModule(int processId);
    public bool TryReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, out byte[] buffer);
    public bool TryReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer);
    public void WriteMemory(IntPtr hProcess, IMemorySegment trackedScanItem, string newValue);
    public void WriteMemory<T>(IntPtr hProcess, IntPtr memoryAddress, T newValue) where T : struct;
    public IList<VirtualMemoryRegion> GatherVirtualMemoryRegions(IntPtr hProcess);
    public void UpdateAddresses(IntPtr hProcess, IEnumerable<IMemorySegment> virtualAddresses, CancellationToken token = default);
    public IEnumerable<MEMORY_BASIC_INFORMATION64> EnumerateMemoryRegions(IntPtr hProcess);
    public IList<ModuleInfo> GetProcessModules(IntPtr hProcess);
    public IntPtr GetStackStart(IntPtr hProcess, int threadNr, ModuleInfo? kernel32Module = null);
}
