using System.Runtime.InteropServices;
using static CelSerEngine.Core.Native.Enums;
using static CelSerEngine.Core.Native.Structs;

namespace CelSerEngine.Core.Native;

internal static class Functions
{
    [DllImport("kernel32.dll")]
    internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr CreateToolhelp32Snapshot( CreateToolhelp32SnapshotFlags dwFlags, int th32ProcessID);

    [DllImport("kernel32.dll")]
    internal static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll")]
    internal static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern NTSTATUS NtOpenProcess(out IntPtr ProcessHandle, uint AccessMask, out OBJECT_ATTRIBUTES ObjectAttributes, ref CLIENT_ID ClientId);

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern NTSTATUS NtReadVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint NumberOfBytesToRead, out uint NumberOfBytesRead);

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern NTSTATUS NtWriteVirtualMemory(IntPtr ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint NumberOfBytesToWrite, out uint NumberOfBytesWritten);

    [DllImport("ntdll.dll")]
    internal static extern NTSTATUS NtQueryVirtualMemory(
        IntPtr ProcessHandle,
        IntPtr BaseAddress,
        int MemoryInformationClass,
        ref MEMORY_BASIC_INFORMATION64 MemoryInformation,
        int MemoryInformationLength,
        out uint ReturnLength
    );

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "K32EnumProcessModules")]
    public static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[]? lphModule, uint cb, [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "K32GetModuleFileNameExW")]
    public static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] char[] lpFilename, [MarshalAs(UnmanagedType.U4)] int nSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "K32GetModuleInformation")]
    public static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, int cb);
}
