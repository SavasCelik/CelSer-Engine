﻿using System.Diagnostics;
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
            if (returnLength > 0 && mem_basic_info.Protect == (uint)MEMORY_PROTECTION.PAGE_READWRITE && mem_basic_info.State == (uint)MEMORY_STATE.MEM_COMMIT)
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
}
