using System.Diagnostics;
using static CelSerEngine.Core.Native.Enums;
using static CelSerEngine.Core.Native.Structs;
using static CelSerEngine.Core.Native.Functions;
using CelSerEngine.Core.Models;
using System.Runtime.InteropServices;
using CelSerEngine.Core.Extensions;
using System.Buffers;

namespace CelSerEngine.Core.Native;

// TODO: Close handles when failed, return LastErrors
public sealed class NativeApi : INativeApi
{
    public readonly ArrayPool<byte> _byteArrayPool;

    public NativeApi()
    {
        _byteArrayPool = ArrayPool<byte>.Shared;
    }

    public IntPtr OpenProcess(string processName)
    {
        var processList = Process.GetProcessesByName(processName);

        if (processList.Length == 0)
            return IntPtr.Zero;

        var process = processList.First();

        return OpenProcess(process.Id);
    }

    public IntPtr OpenProcess(int processId)
    {
        Debug.WriteLine($"ProcessID: {processId}");

        var clientId = new CLIENT_ID
        {
            UniqueProcess = new IntPtr(processId)
        };

        var result = NtOpenProcess(
            out var hProcess,
            (uint)ProcessAccessFlags.PROCESS_ALL_ACCESS,
            out var objectAttributes,
            ref clientId);

        return hProcess;
    }

    public ProcessModuleInfo GetProcessMainModule(int processId)
    {
        var snapshotHandle = CreateToolhelp32Snapshot(CreateToolhelp32SnapshotFlags.TH32CS_SNAPMODULE, processId);

        if (snapshotHandle == IntPtr.Zero)
        {
            CloseHandle(snapshotHandle);
            throw new Exception("Failed to create snapshot.");
        }

        var moduleEntry = new MODULEENTRY32
        {
            dwSize = Marshal.SizeOf(typeof(MODULEENTRY32))
        };

        if (!Module32First(snapshotHandle, ref moduleEntry))
        {
            CloseHandle(snapshotHandle);
            throw new Exception("Failed to get module information.");
        }

        CloseHandle(snapshotHandle);

        return new ProcessModuleInfo(moduleEntry.szModule, moduleEntry.modBaseAddr, moduleEntry.modBaseSize);
    }

    public bool TryReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, out byte[] buffer)
    {
        buffer = new byte[numberOfBytesToRead];
        return TryReadVirtualMemory(hProcess, address, numberOfBytesToRead, buffer);
    }

    public bool TryReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer)
    {
        var result = NtReadVirtualMemory(
            hProcess,
            address,
            buffer,
            numberOfBytesToRead,
            out _);

        return result == NTSTATUS.Success;
    }

    public void WriteMemory(IntPtr hProcess, IMemorySegment trackedScanItem, string newValue)
    {
        var memoryAddress = trackedScanItem.Address;

        if (trackedScanItem is IPointer pointer)
        {
            memoryAddress = pointer.PointingTo;
        }

        switch (trackedScanItem.ScanDataType)
        {
            case ScanDataType.Short:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<short>());
                break;
            case ScanDataType.Integer:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<int>());
                break;
            case ScanDataType.Float:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<float>());
                break;
            case ScanDataType.Double:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<double>());
                break;
            case ScanDataType.Long:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<long>());
                break;
            default:
                throw new NotImplementedException($"{nameof(WriteMemory)} for Type: {trackedScanItem.ScanDataType} is not implemented");

        }
        
    }

    public void WriteMemory<T>(IntPtr hProcess, IntPtr memoryAddress, T newValue)
        where T : struct
    {
        var typeSize = Marshal.SizeOf(typeof(T));
        var bytesToWrite = _byteArrayPool.Rent(typeSize);
        var ptr = Marshal.AllocHGlobal(typeSize);
        Marshal.StructureToPtr(newValue, ptr, true);
        Marshal.Copy(ptr, bytesToWrite, 0, typeSize);
        Marshal.FreeHGlobal(ptr);

        var result = NtWriteVirtualMemory(
            hProcess,
            memoryAddress,
            bytesToWrite,
            (uint)typeSize,
            out uint _);

        _byteArrayPool.Return(bytesToWrite);
    }

    public void UpdateAddresses(IntPtr hProcess, IEnumerable<IMemorySegment> virtualAddresses, CancellationToken token = default)
    {
        foreach (var address in virtualAddresses)
        {
            if (token.IsCancellationRequested)
                break;

            if (address == null)
                continue;

            if (address is IPointer pointerScanItem)
            {
                UpdatePointerAddress(hProcess, pointerScanItem);
                continue;
            }

            UpdateMemorySegmennt(hProcess, address);
        }
    }

    public void UpdateMemorySegmennt(IntPtr hProcess, IMemorySegment memorySegment)
    {
        if (memorySegment == null)
            return;

        var typeSize = memorySegment.ScanDataType.GetPrimitiveSize();
        var buffer = _byteArrayPool.Rent(typeSize);
        var successful = TryReadVirtualMemory(hProcess, memorySegment.Address, (uint)typeSize, buffer);
        memorySegment.Value = successful ? buffer.ConvertToString(memorySegment.ScanDataType) : "???";
        _byteArrayPool.Return(buffer, clearArray: true);
    }

    //public static void UpdatePointerAddress(IntPtr hProcess, TrackedPointerScanItem trackedPointerScanItem)
    //{
    //    // TODO: This method should be refactored, calling DetermineAddressDisplayString just feels weird
    //    var buffer = new byte[sizeof(long)];

    //    NtReadVirtualMemory(
    //        hProcess,
    //        trackedPointerScanItem.Pointer.Address,
    //        buffer,
    //        (uint)buffer.Length,
    //        out _);

    //    var pointingAddress = (IntPtr)BitConverter.ToInt64(buffer);

    //    for (var i = trackedPointerScanItem.Pointer.Offsets.Count - 1; i >= 0; i--)
    //    {
    //        var offset = trackedPointerScanItem.Pointer.Offsets[i].ToInt32();

    //        if (i == 0)
    //        {
    //            pointingAddress = pointingAddress + offset;
    //            break;
    //        }

    //        NtReadVirtualMemory(
    //        hProcess,
    //        pointingAddress + offset,
    //        buffer,
    //        (uint)buffer.Length,
    //        out _);

    //        pointingAddress = (IntPtr)BitConverter.ToInt64(buffer);
    //    }

    //    trackedPointerScanItem.ScanItem.BaseAddress = pointingAddress;
    //    trackedPointerScanItem.DetermineAddressDisplayString();
    //}

    public void UpdatePointerAddress(IntPtr hProcess, IPointer? pointerAddress)
    {
        if (pointerAddress == null)
            return;

        ResolvePointerPath(hProcess, pointerAddress);
        
        var typeSize = pointerAddress.ScanDataType.GetPrimitiveSize();
        var buffer = _byteArrayPool.Rent(typeSize);
        var successful = TryReadVirtualMemory(hProcess, pointerAddress.PointingTo, (uint)typeSize, buffer);
        pointerAddress.Value = successful ? buffer.ConvertToString(pointerAddress.ScanDataType) : "???";
        _byteArrayPool.Return(buffer, clearArray: true);
    }

    public void ResolvePointerPath(IntPtr hProcess, IPointer pointerAddress)
    {
        var buffer = _byteArrayPool.Rent(sizeof(long));
        TryReadVirtualMemory(hProcess, pointerAddress.Address, sizeof(long), buffer);
        pointerAddress.PointingTo = (IntPtr)BitConverter.ToInt64(buffer);

        for (var i = pointerAddress.Offsets.Count - 1; i >= 0; i--)
        {
            var offset = pointerAddress.Offsets[i].ToInt32();

            if (i == 0)
            {
                pointerAddress.PointingTo += offset;
                break;
            }

            TryReadVirtualMemory(hProcess, pointerAddress.PointingTo + offset, sizeof(long), buffer);
            pointerAddress.PointingTo = (IntPtr)BitConverter.ToInt64(buffer);
        }

        _byteArrayPool.Return(buffer, clearArray: true);
    }

    public IList<VirtualMemoryRegion> GatherVirtualMemoryRegions(IntPtr hProcess)
    {
        if (hProcess == IntPtr.Zero)
            throw new ArgumentNullException(nameof(hProcess));

        var virtualMemoryRegions = new List<VirtualMemoryRegion>();
        GetSystemInfo(out var systemInfo);

        // IntPtr proc_min_address = (IntPtr)0x00007ff78cb10000;
        IntPtr proc_min_address = systemInfo.minimumApplicationAddress;
        IntPtr proc_max_address = systemInfo.maximumApplicationAddress;

        // saving the values as long ints so I won't have to do a lot of casts later
        ulong proc_min_address_l = (ulong)proc_min_address;
        ulong proc_max_address_l = (ulong)proc_max_address;

        // this will store any information we get from VirtualQueryEx()
        var memoryBasicInfos = new List<MEMORY_BASIC_INFORMATION64>();
        MEMORY_BASIC_INFORMATION64 mem_basic_info = new MEMORY_BASIC_INFORMATION64();
        while (proc_min_address_l < proc_max_address_l)
        {
            // 28 = sizeof(MEMORY_BASIC_INFORMATION)
            //var result = VirtualQueryEx(pHandle, proc_min_address, out mem_basic_info, (uint)Marshal.SizeOf(mem_basic_info));
            var result = NtQueryVirtualMemory(
                hProcess,
                proc_min_address,
                (int)MEMORY_INFORMATION_CLASS.MemoryBasicInformation,
                ref mem_basic_info,
                Marshal.SizeOf(mem_basic_info),
                out var returnLength
            );

            // if this memory chunk is accessible
            if (returnLength > 0 && mem_basic_info.Protect == MEMORY_PROTECTION.PAGE_READWRITE && mem_basic_info.State == MEMORY_STATE.MEM_COMMIT)
            {
                //VirtualProtectEx(pHandle, new IntPtr((long)mem_basic_info.BaseAddress), new UIntPtr(mem_basic_info.RegionSize), 0x40, out var prt);
                memoryBasicInfos.Add(mem_basic_info);
                if (TryReadVirtualMemory(hProcess, (IntPtr)mem_basic_info.BaseAddress, (uint)mem_basic_info.RegionSize, out var memoryBytes))
                {
                    var virtualMemoryRegion = new VirtualMemoryRegion((IntPtr)mem_basic_info.BaseAddress, mem_basic_info.RegionSize, memoryBytes);
                    virtualMemoryRegions.Add(virtualMemoryRegion);
                }
            }

            // move to the next memory chunk
            proc_min_address_l += mem_basic_info.RegionSize;
            proc_min_address = new IntPtr((long)proc_min_address_l);
        }

        return virtualMemoryRegions;
    }

    public IEnumerable<MEMORY_BASIC_INFORMATION64> EnumerateMemoryRegions(IntPtr hProcess)
    {
        ulong currentAddress = 0x0;
        ulong stopAddress = 0x7FFFFFFFFFFFFFFF;
        // or
        //GetSystemInfo(out var systemInfo);
        //IntPtr proc_min_address = systemInfo.minimumApplicationAddress;
        //IntPtr proc_max_address = systemInfo.maximumApplicationAddress;
        var memInfoClass = (int)MEMORY_INFORMATION_CLASS.MemoryBasicInformation;
        var mbi = new MEMORY_BASIC_INFORMATION64();

        while (NtQueryVirtualMemory(hProcess, (IntPtr)currentAddress, memInfoClass, ref mbi, Marshal.SizeOf(mbi), out _) == NTSTATUS.Success
            && currentAddress < stopAddress && (currentAddress + mbi.RegionSize) > currentAddress)
        {
            yield return mbi;
            currentAddress = mbi.BaseAddress + mbi.RegionSize;
        }
    }

    public IList<ModuleInfo> GetProcessModules(IntPtr hProcess)
    {
        // https://github.com/microsoft/clrmd/blob/main/src/Microsoft.Diagnostics.Runtime/DataReaders/Windows/WindowsProcessDataReader.cs#L138
        EnumProcessModules(hProcess, null, 0, out uint needed);
        var moduleHandles = new IntPtr[needed / IntPtr.Size];

        if (!EnumProcessModules(hProcess, moduleHandles, needed, out _))
            throw new InvalidOperationException("Unable to get process modules. " + Marshal.GetLastWin32Error());

        var moduleInfos = new List<ModuleInfo>(moduleHandles.Length);
        const int BufferSize = 1024;
        var buffer = ArrayPool<char>.Shared.Rent(BufferSize);

        try
        {
            for (var i = 0; i < moduleHandles.Length; i++)
            {
                var stringLength = GetModuleFileNameEx(hProcess, moduleHandles[i], buffer, BufferSize);

                if (stringLength == 0)
                    throw new InvalidOperationException("Unable to get module file name. " + Marshal.GetLastWin32Error());

                var fileName = new string(buffer, 0, stringLength);

                if (!GetModuleInformation(hProcess, moduleHandles[i], out var mi, Marshal.SizeOf<MODULEINFO>()))
                    throw new InvalidOperationException("Unable to read module info. " + Marshal.GetLastWin32Error());

                moduleInfos.Add(new ModuleInfo { Name = fileName, BaseAddress = mi.lpBaseOfDll, Size = mi.SizeOfImage, ModuleIndex = i });
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

        return moduleInfos;
    }

    public IntPtr GetStackStart(IntPtr hProcess, int threadNr, ModuleInfo? kernel32Module = null)
    {
        if (kernel32Module == null)
        {
            var _kernel32ModuleHandle = GetModuleHandle("kernel32.dll");

            if (_kernel32ModuleHandle == IntPtr.Zero)
                throw new InvalidOperationException("Handle to kernel32.dll not found. " + Marshal.GetLastWin32Error());

            if (!GetModuleInformation(hProcess, _kernel32ModuleHandle, out var mi, Marshal.SizeOf<MODULEINFO>()))
                throw new InvalidOperationException("Failed fetching kernel32 module info. " + Marshal.GetLastWin32Error());

            kernel32Module = new ModuleInfo { Name = "kernel32.dll", BaseAddress = mi.lpBaseOfDll, Size = mi.SizeOfImage };
        }

        var processId = GetProcessId(hProcess);
        IntPtr hSnapshot = CreateToolhelp32Snapshot(CreateToolhelp32SnapshotFlags.TH32CS_SNAPTHREAD, processId);

        if (hSnapshot == IntPtr.Zero)
            throw new InvalidOperationException("Failed taking snapshot. " + Marshal.GetLastWin32Error());

        var stackStart = IntPtr.Zero;
        var te32 = new THREADENTRY32
        {
            dwSize = (uint)Marshal.SizeOf(typeof(THREADENTRY32))
        };

        try
        {
            if (!Thread32First(hSnapshot, ref te32))
                throw new InvalidOperationException("Failed getting first thread. " + Marshal.GetLastWin32Error());

            do
            {
                if (te32.th32OwnerProcessID == processId)
                {
                    if (threadNr != 0)
                    {
                        threadNr--;
                        continue;
                    }

                    IntPtr hThread = OpenThread(ThreadAccess.QUERY_INFORMATION | ThreadAccess.GET_CONTEXT, false, te32.th32ThreadID);

                    if (hThread == IntPtr.Zero)
                        throw new InvalidOperationException($"Failed getting thread handle. te32.th32ThreadID: {te32.th32ThreadID} " + Marshal.GetLastWin32Error());

                    var stackTopPtr = IntPtr.Zero;

                    if (NtQueryInformationThread(hThread, ThreadInfoClass.ThreadBasicInformation, out var tbi, Marshal.SizeOf<THREAD_BASIC_INFORMATION>(), out _) == NTSTATUS.Success)
                    {
                        var stackTop = new byte[8];
                        NtReadVirtualMemory(hProcess, tbi.TebBaseAddress + 8, stackTop, 8, out _);
                        stackTopPtr = (IntPtr)BitConverter.ToInt64(stackTop);

                        // This would give the same result:
                        //NtReadVirtualMemory(hProcess, tbi.TebBaseAddress, stackTop4, (uint)Marshal.SizeOf(tbi), out _);
                        //IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<THREAD_BASIC_INFORMATION>());
                        //Marshal.Copy(stackTop4, 0, ptr, stackTop4.Length);
                        //var tbi2 = Marshal.PtrToStructure<THREAD_BASIC_INFORMATION>(ptr);
                        //Marshal.FreeHGlobal(ptr);
                        //stackTopPtr = tbi2.TebBaseAddress;
                    }

                    CloseHandle(hThread);

                    if (stackTopPtr == IntPtr.Zero)
                        continue;

                    var buffer = _byteArrayPool.Rent(4096);

                    if (NtReadVirtualMemory(hProcess, stackTopPtr - 4096, buffer, 4096, out _) == NTSTATUS.Success)
                    {
                        for (int i = (4096 / 8) - 1; i >= 0; i--)
                        {
                            var buffAddress = (IntPtr)BitConverter.ToUInt64(buffer, i * 8);
                        
                            if (buffAddress.InRange(kernel32Module.BaseAddress, kernel32Module.BaseAddress + (int)kernel32Module.Size))
                            {
                                stackStart = stackTopPtr - 4096 + i * 8;
                                break;
                            }
                        }
                    }

                    _byteArrayPool.Return(buffer);
                    break;
                }
            } while (Thread32Next(hSnapshot, ref te32));

        }
        finally
        {
            CloseHandle(hSnapshot);
        }

        return stackStart;
    }

    private bool InRange(IntPtr value, IntPtr min, IntPtr max)
    {
        return value >= min && value <= max;
    }
}
