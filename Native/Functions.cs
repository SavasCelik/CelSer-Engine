using System;
using System.Runtime.InteropServices;
using static CelSerEngine.Native.Enums;
using static CelSerEngine.Native.Structs;

namespace CelSerEngine.Native
{
    internal static class Functions
    {
        [DllImport("kernel32.dll")]
        internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern NTSTATUS NtOpenProcess(out IntPtr ProcessHandle, uint AccessMask, out OBJECT_ATTRIBUTES ObjectAttributes, ref CLIENT_ID ClientId);

        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern NTSTATUS NtReadVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint NumberOfBytesToRead, ref uint NumberOfBytesRead);

        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern NTSTATUS NtWriteVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint NumberOfBytesToWrite, ref uint NumberOfBytesWritten);

        [DllImport("ntdll.dll")]
        internal static extern NTSTATUS NtQueryVirtualMemory(
            IntPtr ProcessHandle,
            IntPtr BaseAddress,
            int MemoryInformationClass,
            ref MEMORY_BASIC_INFORMATION64 MemoryInformation,
            int MemoryInformationLength,
            out uint ReturnLength
        );
    }
}
