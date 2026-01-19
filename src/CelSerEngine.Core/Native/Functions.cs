using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using static CelSerEngine.Core.Native.Enums;
using static CelSerEngine.Core.Native.Structs;

namespace CelSerEngine.Core.Native;

internal static class Functions
{
    [DllImport("kernel32.dll")]
    internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr CreateToolhelp32Snapshot(CreateToolhelp32SnapshotFlags dwFlags, int th32ProcessID);

    [DllImport("kernel32.dll")]
    internal static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll")]
    internal static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern NTSTATUS NtOpenProcess(out SafeProcessHandle ProcessHandle, uint AccessMask, out OBJECT_ATTRIBUTES ObjectAttributes, ref CLIENT_ID ClientId);

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern NTSTATUS NtReadVirtualMemory(SafeProcessHandle ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint NumberOfBytesToRead, out uint NumberOfBytesRead);

    [DllImport("ntdll.dll", SetLastError = true)]
    internal static extern NTSTATUS NtWriteVirtualMemory(SafeProcessHandle ProcessHandle, IntPtr BaseAddress, byte[] Buffer, uint NumberOfBytesToWrite, out uint NumberOfBytesWritten);

    [DllImport("ntdll.dll")]
    internal static extern NTSTATUS NtQueryVirtualMemory(
        SafeProcessHandle ProcessHandle,
        IntPtr BaseAddress,
        int MemoryInformationClass,
        ref MEMORY_BASIC_INFORMATION64 MemoryInformation,
        int MemoryInformationLength,
        out uint ReturnLength
    );

    [DllImport("ntdll.dll")]
    internal static extern NTSTATUS NtQueryVirtualMemory(
        SafeProcessHandle ProcessHandle,
        IntPtr BaseAddress,
        int MemoryInformationClass,
        IntPtr MemoryInformation,
        int MemoryInformationLength,
        out uint ReturnLength
    );

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool QueryWorkingSet(
        SafeProcessHandle ProcessHandle,
        IntPtr pv,
        int cb
    );

    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "K32EnumProcessModules")]
    public static extern bool EnumProcessModules(SafeProcessHandle hProcess, [Out] IntPtr[]? lphModule, uint cb, [MarshalAs(UnmanagedType.U4)] out uint lpcbNeeded);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "K32GetModuleFileNameExW")]
    public static extern int GetModuleFileNameEx(SafeProcessHandle hProcess, IntPtr hModule, [Out] char[] lpFilename, [MarshalAs(UnmanagedType.U4)] int nSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "K32GetModuleInformation")]
    public static extern bool GetModuleInformation(SafeProcessHandle hProcess, IntPtr hModule, out MODULEINFO lpmodinfo, int cb);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern int GetProcessId(SafeProcessHandle hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Thread32Next(IntPtr hSnapshot, ref THREADENTRY32 lpte);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

    [DllImport("ntdll.dll", SetLastError = true)]
    public static extern NTSTATUS NtQueryInformationThread(IntPtr threadHandle, ThreadInfoClass threadInformationClass, out THREAD_BASIC_INFORMATION threadInformation, int threadInformationLength, out uint returnLength);
}
