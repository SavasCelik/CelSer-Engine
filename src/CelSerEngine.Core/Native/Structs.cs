using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static CelSerEngine.Core.Native.Enums;

namespace CelSerEngine.Core.Native;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member")]
public static class Structs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CLIENT_ID
    {
        internal IntPtr UniqueProcess;
        internal IntPtr UniqueThread;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEM_INFO
    {
        public ushort processorArchitecture;
        ushort reserved;
        public uint pageSize;
        public IntPtr minimumApplicationAddress;
        public IntPtr maximumApplicationAddress;
        public IntPtr activeProcessorMask;
        public uint numberOfProcessors;
        public uint processorType;
        public uint allocationGranularity;
        public ushort processorLevel;
        public ushort processorRevision;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION64
    {
        public ulong BaseAddress;
        public ulong AllocationBase;
        public MEMORY_PROTECTION AllocationProtect;
        public uint __alignment1;
        public ulong RegionSize;
        public MEMORY_STATE State;
        public MEMORY_PROTECTION Protect;
        public uint Type;
        public uint __alignment2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct OBJECT_ATTRIBUTES
    {
        public int Length;
        public int RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public int SecurityDescriptor;
        public int SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MODULEENTRY32
    {
        public int dwSize;
        public uint th32ModuleID;
        public uint th32ProcessID;
        public uint GlblcntUsage;
        public uint ProccntUsage;
        public IntPtr modBaseAddr;
        public uint modBaseSize;
        public IntPtr hModule;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szModule;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExePath;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MODULEINFO
    {
        public IntPtr lpBaseOfDll;
        public uint SizeOfImage;
        public IntPtr EntryPoint;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct THREADENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ThreadID;
        public uint th32OwnerProcessID;
        public int tpBasePri;
        public int tpDeltaPri;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct THREAD_BASIC_INFORMATION
    {
        public IntPtr ExitStatus;
        public IntPtr TebBaseAddress;
        public IntPtr UniqueProcessId;
        public IntPtr UniqueThreadId;
        public IntPtr AffinityMask;
        public int Priority;
        public int BasePriority;
    }
}
