
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using static DInvoke.Data.Native;
using static DInvoke.Data.Win32.Kernel32;

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
            Process[] processList = Process.GetProcessesByName(processName);

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
                (uint)ProcessAccessFlags.PROCESS_ALL_ACCESS,
                ref oa,
                ref ci);

            return hProcess;
        }

        public static IEnumerable<MEMORY_BASIC_INFORMATION64> GatherVirtualPages(IntPtr hProcess)
        {
            if (hProcess == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(hProcess));
            }

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
                if (returnLength > 0 && mem_basic_info.State != 0x10000 /*&& mem_basic_info.Protect == PAGE_READWRITE && mem_basic_info.State == MEM_COMMIT*/)
                {
                    //VirtualProtectEx(pHandle, new IntPtr((long)mem_basic_info.BaseAddress), new UIntPtr(mem_basic_info.RegionSize), 0x40, out var prt);
                    memoryBasicInfos.Add(mem_basic_info);
                }

                // move to the next memory chunk
                proc_min_address_l += mem_basic_info.RegionSize;
                proc_min_address = new IntPtr((long)proc_min_address_l);
            }

            return memoryBasicInfos;
        }

        public static IEnumerable<ValueAddress> ReadPMV(IntPtr hProcess, IEnumerable<MEMORY_BASIC_INFORMATION64> virtualPages, ScanConstraint scanConstraint)
        {
            var sizeOfType = scanConstraint.GetSize();
            var concurrentPointingThere = new ConcurrentBag<ValueAddress>();
            var parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;

            //Parallel.ForEach(virtualPages, parallelOptions, page => {
            //    var buffer = new byte[(uint)page.RegionSize];

            //    uint bytesWritten = 0;

            //    var result = NtReadVirtualMemory(
            //        hProcess,
            //        (IntPtr)page.BaseAddress,
            //        buffer,
            //        (uint)page.RegionSize,
            //        ref bytesWritten);

            //    for (int i = 0; i < (int)page.RegionSize; i += sizeOfType)
            //    {
            //        if (i + sizeOfType > (int)page.RegionSize)
            //        {
            //            continue;
            //        }
            //        var bufferValue = buffer.Skip(i).Take(sizeOfType).ToArray();

            //        if (scanConstraint.Comapare(bufferValue))
            //        {
            //            concurrentPointingThere.Add(new ValueAddress(page.BaseAddress, i, bufferValue, scanConstraint.DataType.EnumType));
            //        }
            //    }
            //});

            //return concurrentPointingThere;

            foreach (var page in virtualPages)
            {
                var buffer = new byte[(uint)page.RegionSize];

                uint bytesWritten = 0;

                var result = NtReadVirtualMemory(
                    hProcess,
                    (IntPtr)page.BaseAddress,
                    buffer,
                    (uint)page.RegionSize,
                    ref bytesWritten);

                for (var i = 0; i < (int)page.RegionSize; i += sizeOfType)
                {
                    if (i + sizeOfType > (int)page.RegionSize)
                    {
                        continue;
                    }
                    var bufferValue = buffer.Skip(i).Take(sizeOfType).ToArray();

                    if (scanConstraint.Comapare(bufferValue))
                    {
                        yield return new ValueAddress(page.BaseAddress, i, bufferValue.ByteArrayToObject(scanConstraint.DataType.EnumType), scanConstraint.DataType.EnumType);
                    }
                }
            }
        }


        public static IEnumerable<ValueAddress> ReadPMV2(IntPtr hProcess, MEMORY_BASIC_INFORMATION64[] virtualPages, ScanConstraint scanConstraint, IVectorComparer comparer)
        {
            var sizeOfType = scanConstraint.GetSize();
            var concurrentPointingThere = new ConcurrentBag<ValueAddress>();
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };
            var valueAddressList = new List<ValueAddress>();

            //foreach (var page in virtualPages)
            //{
            //    var buffer = new byte[(uint)page.RegionSize];

            //    uint bytesWritten = 0;

            //    var result = NtReadVirtualMemory(
            //        hProcess,
            //        (IntPtr)page.BaseAddress,
            //        buffer,
            //        (uint)page.RegionSize,
            //        ref bytesWritten);

            //    var results = comparer.GetMatchingValueAddresses(page, buffer);
            //    Debug.WriteLine("");
            //    valueAddressList.AddRange(results);
            //}

            //return valueAddressList;

            Parallel.ForEach(virtualPages, parallelOptions, page =>
            {
                var buffer = new byte[(uint)page.RegionSize];
                uint bytesWritten = 0;

                var result = NtReadVirtualMemory(
                    hProcess,
                    (IntPtr)page.BaseAddress,
                    buffer,
                    (uint)page.RegionSize,
                    ref bytesWritten);

                var results = comparer.GetMatchingValueAddresses(page, buffer).ToArray();
                foreach (var item in results)
                {
                    concurrentPointingThere.Add(item);
                }
            });

            return concurrentPointingThere;
        }

        public static IEnumerable<ValueAddress> ChangedValue(IntPtr hProcess, IEnumerable<ValueAddress> virtualAddresses, ScanConstraint scanConstraint)
        {
            var sizeOfType = scanConstraint.GetSize();
            foreach (var address in virtualAddresses)
            {
                var buffer = new byte[sizeof(int)];

                uint bytesWritten = 0;

                var result = NtReadVirtualMemory(
                    hProcess,
                    address.BaseAddress + address.Offset,
                    buffer,
                    (uint)sizeOfType,
                    ref bytesWritten);

                if (scanConstraint.Comapare(buffer))
                {
                    address.Value = buffer;
                    yield return address;
                }
            }
        }

        public static IEnumerable<ValueAddress> ChangedValue2(IntPtr hProcess, IEnumerable<ValueAddress> virtualAddresses, ScanConstraint scanConstraint, IVectorComparer comparer)
        {
            var sizeOfType = scanConstraint.GetSize();
            foreach (var address in virtualAddresses)
            {
                var buffer = new byte[sizeof(int)];

                uint bytesWritten = 0;

                var result = NtReadVirtualMemory(
                    hProcess,
                    address.BaseAddress + address.Offset,
                    buffer,
                    (uint)sizeOfType,
                ref bytesWritten);
                
                if (scanConstraint.Comapare(buffer))
                {
                    address.Value = buffer;
                    yield return address;
                }
            }
        }

        public static void WriteMemory(IntPtr hProcess, TrackedScanItem trackedScanItem)
        {
            var typeSize = trackedScanItem.GetDataTypeSize();
            uint bytesWritten = 0;

            //var result = NtWriteVirtualMemory(
            //                            hProcess,
            //                            trackedScanItem.Address,
            //                            trackedScanItem.SetValue!,
            //                            (uint)typeSize,
            //                            ref bytesWritten);
        }

        public static void UpdateAddresses(IntPtr hProcess, IEnumerable<ValueAddress> virtualAddresses)
        {
            foreach (var address in virtualAddresses)
            {
                if (address == null)
                    continue;
                var typeSize = address.GetDataTypeSize();
                var buffer = new byte[typeSize];
                uint bytesWritten = 0;

                var result = NtReadVirtualMemory(
                    hProcess,
                    address.BaseAddress + address.Offset,
                    buffer,
                    (uint)typeSize,
                    ref bytesWritten);

                address.Value = buffer.ByteArrayToObject(address.EnumDataType);
            }
        }

    }
}
