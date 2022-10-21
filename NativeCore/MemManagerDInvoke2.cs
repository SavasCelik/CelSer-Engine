
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CelSerEngine.Extensions;
using CelSerEngine.Models;
using static DInvoke.Data.Native;
using static DInvoke.Data.Win32;

namespace CelSerEngine.NativeCore
{
    public static class MemManagerDInvoke2
    {
        [DllImport("kernel32.dll")]
        static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern NTSTATUS NtOpenProcess(ref IntPtr ProcessHandle, uint AccessMask, ref OBJECT_ATTRIBUTES ObjectAttributes, ref CLIENT_ID ClientId);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern NTSTATUS NtReadVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint NumberOfBytesToRead, ref uint NumberOfBytesRead);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern NTSTATUS NtWriteVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint NumberOfBytesToWrite, ref uint NumberOfBytesWritten);

        [DllImport("ntdll.dll")]
        public static extern NTSTATUS NtQueryVirtualMemory(
          IntPtr ProcessHandle,
          IntPtr BaseAddress,
          int MemoryInformationClass,
          ref MEMORY_BASIC_INFORMATION64 MemoryInformation,
          int MemoryInformationLength,
          out uint ReturnLength
        );

        //[DllImport("ntdll.dll", SetLastError = true)]
        //public extern NTSTATUS NtQueryVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, DInvoke.Data.Native.MEMORYINFOCLASS MemoryInformationClass, IntPtr MemoryInformation, uint MemoryInformationLength, ref uint ReturnLength);

        public static IntPtr OpenProcess(string processName)
        {
            var processList = Process.GetProcessesByName(processName);

            if (processList.Length == 0)
                return IntPtr.Zero;

            var pcs = processList.First();

            Debug.WriteLine($"ProcessID: {pcs.Id}");

            var hProcess = IntPtr.Zero;
            var oa = new OBJECT_ATTRIBUTES();

            var ci = new CLIENT_ID
            {
                UniqueProcess = new IntPtr(pcs.Id)
            };

            var result = NtOpenProcess(
                ref hProcess,
                (uint)Kernel32.ProcessAccessFlags.PROCESS_ALL_ACCESS,
                ref oa,
                ref ci);

            return hProcess;
        }

        public static IntPtr OpenProcess(int processId)
        {
            Debug.WriteLine($"ProcessID: {processId}");

            var hProcess = IntPtr.Zero;
            var oa = new OBJECT_ATTRIBUTES();

            var ci = new CLIENT_ID
            {
                UniqueProcess = new IntPtr(processId)
            };

            var result = NtOpenProcess(
                ref hProcess,
                (uint)Kernel32.ProcessAccessFlags.PROCESS_ALL_ACCESS,
                ref oa,
                ref ci);

            return hProcess;
        }

        public static IList<VirtualMemoryPage> GatherVirtualPages(IntPtr hProcess)
        {
            if (hProcess == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hProcess));
            }

            var virtualMemoryPages = new List<VirtualMemoryPage>();

            GetSystemInfo(out var sys_info);

            // IntPtr proc_min_address = (IntPtr)0x00007ff78cb10000;
            IntPtr proc_min_address = sys_info.minimumApplicationAddress;
            IntPtr proc_max_address = sys_info.maximumApplicationAddress;

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
                (int)MEMORYINFOCLASS.MemoryBasicInformation,
                ref mem_basic_info,
                Marshal.SizeOf(mem_basic_info),
                out var returnLength
                );

                // if this memory chunk is accessible
                if (returnLength > 0 && mem_basic_info.Protect == WinNT.PAGE_READWRITE && mem_basic_info.State == Kernel32.MEM_COMMIT)
                {
                    //VirtualProtectEx(pHandle, new IntPtr((long)mem_basic_info.BaseAddress), new UIntPtr(mem_basic_info.RegionSize), 0x40, out var prt);
                    memoryBasicInfos.Add(mem_basic_info);
                    var memoryBytes = ReadVirtualMemory(hProcess, (IntPtr)mem_basic_info.BaseAddress, (uint)mem_basic_info.RegionSize);
                    var virtualMemoryPage = new VirtualMemoryPage(mem_basic_info, memoryBytes);
                    virtualMemoryPages.Add(virtualMemoryPage);
                }

                // move to the next memory chunk
                proc_min_address_l += mem_basic_info.RegionSize;
                proc_min_address = new IntPtr((long)proc_min_address_l);
            }

            return virtualMemoryPages;
        }

        public static byte[] ReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead)
        {
            var buffer = new byte[numberOfBytesToRead];
            uint bytesWritten = 0;

            var result = NtReadVirtualMemory(
                hProcess,
                address,
                buffer,
                numberOfBytesToRead,
                ref bytesWritten);

            return result == NTSTATUS.Success ? buffer : Array.Empty<byte>();
        }

        public static void WriteMemory(IntPtr hProcess, TrackedScanItem trackedScanItem)
        {
            var typeSize = trackedScanItem.ScanDataType.GetPrimitiveSize();
            uint bytesWritten = 0;
            var bytesToWrite = BitConverter.GetBytes(trackedScanItem.SetValue ?? trackedScanItem.Value);

            var result = NtWriteVirtualMemory(
                                        hProcess,
                                        trackedScanItem.Address,
                                        bytesToWrite,
                                        (uint)typeSize,
                                        ref bytesWritten);
        }

        public static void UpdateAddresses(IntPtr hProcess, IEnumerable<ValueAddress?> virtualAddresses)
        {
            foreach (var address in virtualAddresses)
            {
                if (address == null)
                    continue;
                var typeSize = address.ScanDataType.GetPrimitiveSize();
                var buffer = new byte[typeSize];
                uint bytesWritten = 0;

                var result = NtReadVirtualMemory(
                    hProcess,
                    address.BaseAddress + address.Offset,
                    buffer,
                    (uint)typeSize,
                    ref bytesWritten);

                address.Value = buffer.ByteArrayToObject(address.ScanDataType);
            }
        }

    }
}
