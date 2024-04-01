using CelSerEngine.Core.Models;
using Microsoft.Win32.SafeHandles;
using static CelSerEngine.Core.Native.Structs;

namespace CelSerEngine.Core.Native;

public interface INativeApi
{
    public SafeProcessHandle OpenProcess(string processName);
    public SafeProcessHandle OpenProcess(int processId);
    public ProcessModuleInfo GetProcessMainModule(int processId);
    public bool TryReadVirtualMemory(SafeProcessHandle hProcess, IntPtr address, uint numberOfBytesToRead, out byte[] buffer);
    public bool TryReadVirtualMemory(SafeProcessHandle hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer);
    public void WriteMemory(SafeProcessHandle hProcess, IMemorySegment trackedScanItem, string newValue);
    public void WriteMemory<T>(SafeProcessHandle hProcess, IntPtr memoryAddress, T newValue) where T : struct;
    public IList<VirtualMemoryRegion> GatherVirtualMemoryRegions(SafeProcessHandle hProcess);
    public void UpdateAddresses(SafeProcessHandle hProcess, IEnumerable<IMemorySegment> virtualAddresses, CancellationToken token = default);
    public IEnumerable<MEMORY_BASIC_INFORMATION64> EnumerateMemoryRegions(SafeProcessHandle hProcess);
    public IList<ModuleInfo> GetProcessModules(SafeProcessHandle hProcess);
    public IntPtr GetStackStart(SafeProcessHandle hProcess, int threadNr, ModuleInfo? kernel32Module = null);
}
