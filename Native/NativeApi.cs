﻿using System;
using System.Diagnostics;
using static CelSerEngine.Native.Enums;
using static CelSerEngine.Native.Structs;
using static CelSerEngine.Native.Functions;
using System.Linq;
using CelSerEngine.Models;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CelSerEngine.Extensions;
using CelSerEngine.Models.ObservableModels;
using System.Buffers;

namespace CelSerEngine.Native
{
    public sealed class NativeApi
    {
        public static readonly ArrayPool<byte> _byteArrayPool = ArrayPool<byte>.Shared;

        public static IntPtr OpenProcess(string processName)
        {
            var processList = Process.GetProcessesByName(processName);

            if (processList.Length == 0)
                return IntPtr.Zero;

            var process = processList.First();

            return OpenProcess(process.Id);
        }

        public static IntPtr OpenProcess(int processId)
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
        
        public static byte[] ReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead)
        {
            var buffer = new byte[numberOfBytesToRead];
            ReadVirtualMemory(hProcess, address, numberOfBytesToRead, buffer);

            return buffer;
        }
        
        public static void ReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer)
        {
            var result = NtReadVirtualMemory(
                hProcess,
                address,
                buffer,
                numberOfBytesToRead,
                out _);

            if (result != NTSTATUS.Success)
                throw new Exception("Failed reading memory");
        }

        public static void WriteMemory(IntPtr hProcess, TrackedItem trackedScanItem)
        {
            var typeSize = trackedScanItem.Item.ScanDataType.GetPrimitiveSize();
            var bytesToWrite = BitConverter.GetBytes(trackedScanItem.SetValue ?? trackedScanItem.Item.Value);

            var result = NtWriteVirtualMemory(
                hProcess,
                trackedScanItem.Item.Address,
                bytesToWrite,
                (uint)typeSize,
                out uint bytesWritten);
        }

        public static void UpdateAddresses(IntPtr hProcess, IEnumerable<IProcessMemory> virtualAddresses)
        {
            foreach (var address in virtualAddresses)
            {
                if (address == null)
                    continue;

                if (address is ObservablePointer pointerScanItem)
                {
                    UpdatePointerAddress(hProcess, pointerScanItem);
                    continue;
                }

                UpdateProcessMemory(hProcess, address);
            }
        }

        public static void UpdateProcessMemory(IntPtr hProcess, IProcessMemory processMemory)
        {
            if (processMemory == null)
                return;

            var typeSize = processMemory.ScanDataType.GetPrimitiveSize();
            var buffer = _byteArrayPool.Rent(typeSize);
            ReadVirtualMemory(hProcess, processMemory.Address, (uint)typeSize, buffer);
            processMemory.Value = buffer.ByteArrayToObject(processMemory.ScanDataType);
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

        public static void UpdatePointerAddress(IntPtr hProcess, ObservablePointer? pointerAddress)
        {
            if (pointerAddress == null)
                return;

            ResolvePointerPath(hProcess, pointerAddress);
            
            var typeSize = pointerAddress.ScanDataType.GetPrimitiveSize();
            var buffer = _byteArrayPool.Rent(typeSize);
            ReadVirtualMemory(hProcess, pointerAddress.PointingTo, (uint)typeSize, buffer);
            pointerAddress.Value = buffer.ByteArrayToObject(pointerAddress.ScanDataType);
            _byteArrayPool.Return(buffer, clearArray: true);
        }

        public static void ResolvePointerPath(IntPtr hProcess, ObservablePointer pointerAddress)
        {
            var buffer = _byteArrayPool.Rent(sizeof(long));
            ReadVirtualMemory(hProcess, pointerAddress.Address, sizeof(long), buffer);
            pointerAddress.PointingTo = (IntPtr)BitConverter.ToInt64(buffer);

            for (var i = pointerAddress.Offsets.Count - 1; i >= 0; i--)
            {
                var offset = pointerAddress.Offsets[i].ToInt32();

                if (i == 0)
                {
                    pointerAddress.PointingTo += offset;
                    break;
                }

                ReadVirtualMemory(hProcess, pointerAddress.PointingTo + offset, sizeof(long), buffer);
                pointerAddress.PointingTo = (IntPtr)BitConverter.ToInt64(buffer);
            }

            _byteArrayPool.Return(buffer, clearArray: true);
        }

        public static IList<VirtualMemoryPage> GatherVirtualPages(IntPtr hProcess)
        {
            if (hProcess == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hProcess));

            var virtualMemoryPages = new List<VirtualMemoryPage>();
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
                    var memoryBytes = ReadVirtualMemory(hProcess, (IntPtr)mem_basic_info.BaseAddress, (uint)mem_basic_info.RegionSize);
                    var virtualMemoryPage = new VirtualMemoryPage(mem_basic_info.BaseAddress, mem_basic_info.RegionSize, memoryBytes);
                    virtualMemoryPages.Add(virtualMemoryPage);
                }

                // move to the next memory chunk
                proc_min_address_l += mem_basic_info.RegionSize;
                proc_min_address = new IntPtr((long)proc_min_address_l);
            }

            return virtualMemoryPages;
        }
    }
}
