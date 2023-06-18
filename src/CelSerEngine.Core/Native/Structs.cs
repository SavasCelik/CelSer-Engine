using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CelSerEngine.Core.Native;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>", Scope = "member")]
internal static class Structs
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
    internal struct MEMORY_BASIC_INFORMATION64
    {
        public ulong BaseAddress;
        public ulong AllocationBase;
        public uint AllocationProtect;
        public uint __alignment1;
        public ulong RegionSize;
        public uint State;
        public uint Protect;
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
}
