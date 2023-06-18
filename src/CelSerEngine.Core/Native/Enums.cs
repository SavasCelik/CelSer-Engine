namespace CelSerEngine.Core.Native;

internal static class Enums
{
    /// <summary>
    /// Full value list here:
    /// https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/596a1078-e883-4972-9bbc-49e60bebca55
    /// https://www.pinvoke.net/default.aspx/Enums/NtStatus.html
    /// </summary>
    internal enum NTSTATUS : uint
    {
        Success = 0x00000000,
        Error = 0xc0000000
    }

    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms684880%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
    /// </summary>
    [Flags]
    public enum ProcessAccessFlags : uint
    {
        PROCESS_ALL_ACCESS = 0x001F0FFF,
        PROCESS_CREATE_PROCESS = 0x0080,
        PROCESS_CREATE_THREAD = 0x0002,
        PROCESS_DUP_HANDLE = 0x0040,
        PROCESS_QUERY_INFORMATION = 0x0400,
        PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
        PROCESS_SET_INFORMATION = 0x0200,
        PROCESS_SET_QUOTA = 0x0100,
        PROCESS_SUSPEND_RESUME = 0x0800,
        PROCESS_TERMINATE = 0x0001,
        PROCESS_VM_OPERATION = 0x0008,
        PROCESS_VM_READ = 0x0010,
        PROCESS_VM_WRITE = 0x0020,
        SYNCHRONIZE = 0x00100000
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ne-ntifs-_memory_information_class
    /// </summary>
    public enum MEMORY_INFORMATION_CLASS : int
    {
        MemoryBasicInformation
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/memory/memory-protection-constants
    /// </summary>
    [Flags]
    public enum MEMORY_PROTECTION : uint
    {
        PAGE_ACCESS_DENIED = 0x0,
        PAGE_EXECUTE = 0x10,
        PAGE_EXECUTE_READ = 0x20,
        PAGE_EXECUTE_READWRITE = 0x40,
        PAGE_EXECUTE_WRITECOPY = 0x80,
        PAGE_GUARD = 0x100,
        PAGE_NOCACHE = 0x200,
        PAGE_WRITECOMBINE = 0x400,
        PAGE_NOACCESS = 0x01,
        PAGE_READONLY = 0x02,
        PAGE_READWRITE = 0x04,
        PAGE_WRITECOPY = 0x08
    }

    [Flags]
    public enum MEMORY_STATE : uint
    {
        MEM_COMMIT = 0x1000,
        MEM_RESERVE = 0x2000,
        MEM_DECOMMIT = 0x4000,
        MEM_RELEASE = 0x8000,
        MEM_FREE = 0x10000,
        MEM_RESET = 0x80000,
        MEM_TOP_DOWN = 0x100000,
        MEM_PHYSICAL = 0x400000,
        MEM_LARGE_PAGES = 0x20000000
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/tlhelp32/nf-tlhelp32-createtoolhelp32snapshot
    /// </summary>
    [Flags]
    public enum CreateToolhelp32SnapshotFlags : uint
    {
        /// <summary>
        /// Indicates that the snapshot handle is to be inheritable.
        /// </summary>
        TH32CS_INHERIT = 0x80000000,

        /// <summary>
        /// Includes all heaps of the process specified in th32ProcessID in the snapshot.
        /// To enumerate the heaps, see Heap32ListFirst.
        /// </summary>
        TH32CS_SNAPHEAPLIST = 0x00000001,

        /// <summary>
        /// Includes all modules of the process specified in th32ProcessID in the snapshot.
        /// To enumerate the modules, see Module32First. If the function fails with
        /// ERROR_BAD_LENGTH, retry the function until it succeeds.
        /// <para>
        /// 64-bit Windows:  Using this flag in a 32-bit process includes the 32-bit modules
        /// of the process specified in th32ProcessID, while using it in a 64-bit process
        /// includes the 64-bit modules. To include the 32-bit modules of the process
        /// specified in th32ProcessID from a 64-bit process, use the TH32CS_SNAPMODULE32 flag.
        /// </para>
        /// </summary>
        TH32CS_SNAPMODULE = 0x00000008,

        /// <summary>
        /// Includes all 32-bit modules of the process specified in th32ProcessID in the
        /// snapshot when called from a 64-bit process. This flag can be combined with
        /// TH32CS_SNAPMODULE or TH32CS_SNAPALL. If the function fails with
        /// ERROR_BAD_LENGTH, retry the function until it succeeds.
        /// </summary>
        TH32CS_SNAPMODULE32 = 0x00000010,

        /// <summary>
        /// Includes all processes in the system in the snapshot. To enumerate the processes, see Process32First.
        /// </summary>
        TH32CS_SNAPPROCESS = 0x00000002,

        /// <summary>
        /// Includes all threads in the system in the snapshot. To enumerate the threads, see
        /// Thread32First.
        /// <para>
        /// To identify the threads that belong to a specific process, compare its process identifier to the
        /// th32OwnerProcessID member of the THREADENTRY32 structure when
        /// enumerating the threads.
        /// </para>
        /// </summary>
        TH32CS_SNAPTHREAD = 0x00000004,

        /// <summary>
        /// Includes all processes and threads in the system, plus the heaps and modules of the process specified in
        /// th32ProcessID.
        /// </summary>
        TH32CS_SNAPALL = TH32CS_SNAPHEAPLIST | TH32CS_SNAPMODULE | TH32CS_SNAPPROCESS | TH32CS_SNAPTHREAD,
    }
}
