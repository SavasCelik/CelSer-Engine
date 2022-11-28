using System;
using System.Runtime.InteropServices;
using static CelSerEngine.Native.Enums;
using static CelSerEngine.Native.Structs;

namespace CelSerEngine.Native
{
    internal static class Functions
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
    }
}
