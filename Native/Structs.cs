using System;
using System.Runtime.InteropServices;
namespace CelSerEngine.Native
{
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
    }
}
