using System.Diagnostics;
using static CelSerEngine.Core.Native.Enums;
using static CelSerEngine.Core.Native.Structs;
using static CelSerEngine.Core.Native.Functions;
using CelSerEngine.Core.Models;
using System.Runtime.InteropServices;
using CelSerEngine.Core.Extensions;
using System.Buffers;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace CelSerEngine.Core.Native;

// TODO: Close handles when failed, return LastErrors
public sealed class NativeApi : INativeApi
{
    public readonly ArrayPool<byte> _byteArrayPool;

    public NativeApi()
    {
        _byteArrayPool = ArrayPool<byte>.Shared;
    }

    public IntPtr OpenProcess(string processName)
    {
        var processList = Process.GetProcessesByName(processName);

        if (processList.Length == 0)
            return IntPtr.Zero;

        var process = processList.First();

        return OpenProcess(process.Id);
    }

    public IntPtr OpenProcess(int processId)
    {
        Debug.WriteLine($"ProcessID: {processId}");

        var clientId = new CLIENT_ID
        {
            UniqueProcess = new IntPtr(processId)
        };

        var result = NtOpenProcess(
            out var hProcess,
            (uint)ProcessAccessFlags.PROCESS_ALL_ACCESS,
            out var objectAttributes,
            ref clientId);

        return hProcess;
    }

    public ProcessModuleInfo GetProcessMainModule(int processId)
    {
        var snapshotHandle = CreateToolhelp32Snapshot(CreateToolhelp32SnapshotFlags.TH32CS_SNAPMODULE, processId);

        if (snapshotHandle == IntPtr.Zero)
        {
            CloseHandle(snapshotHandle);
            throw new Exception("Failed to create snapshot.");
        }

        var moduleEntry = new MODULEENTRY32
        {
            dwSize = Marshal.SizeOf(typeof(MODULEENTRY32))
        };

        if (!Module32First(snapshotHandle, ref moduleEntry))
        {
            CloseHandle(snapshotHandle);
            throw new Exception("Failed to get module information.");
        }

        CloseHandle(snapshotHandle);

        return new ProcessModuleInfo(moduleEntry.szModule, moduleEntry.modBaseAddr, moduleEntry.modBaseSize);
    }

    public bool TryReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, out byte[] buffer)
    {
        buffer = new byte[numberOfBytesToRead];
        return TryReadVirtualMemory(hProcess, address, numberOfBytesToRead, buffer);
    }

    public bool TryReadVirtualMemory(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer)
    {
        var result = NtReadVirtualMemory(
            hProcess,
            address,
            buffer,
            numberOfBytesToRead,
            out _);

        return result == NTSTATUS.Success;
    }

    public void WriteMemory(IntPtr hProcess, IMemorySegment trackedScanItem, string newValue)
    {
        var memoryAddress = trackedScanItem.Address;

        if (trackedScanItem is IPointer pointer)
        {
            memoryAddress = pointer.PointingTo;
        }

        switch (trackedScanItem.ScanDataType)
        {
            case ScanDataType.Short:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<short>());
                break;
            case ScanDataType.Integer:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<int>());
                break;
            case ScanDataType.Float:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<float>());
                break;
            case ScanDataType.Double:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<double>());
                break;
            case ScanDataType.Long:
                WriteMemory(hProcess, memoryAddress, newValue.ParseNumber<long>());
                break;
            default:
                throw new NotImplementedException($"{nameof(WriteMemory)} for Type: {trackedScanItem.ScanDataType} is not implemented");

        }
        
    }

    public void WriteMemory<T>(IntPtr hProcess, IntPtr memoryAddress, T newValue)
        where T : struct
    {
        var typeSize = Marshal.SizeOf(typeof(T));
        var bytesToWrite = _byteArrayPool.Rent(typeSize);
        var ptr = Marshal.AllocHGlobal(typeSize);
        Marshal.StructureToPtr(newValue, ptr, true);
        Marshal.Copy(ptr, bytesToWrite, 0, typeSize);
        Marshal.FreeHGlobal(ptr);

        var result = NtWriteVirtualMemory(
            hProcess,
            memoryAddress,
            bytesToWrite,
            (uint)typeSize,
            out uint _);

        _byteArrayPool.Return(bytesToWrite);
    }

    public void UpdateAddresses(IntPtr hProcess, IEnumerable<IMemorySegment> virtualAddresses, CancellationToken token = default)
    {
        foreach (var address in virtualAddresses)
        {
            if (token.IsCancellationRequested)
                break;

            if (address == null)
                continue;

            if (address is IPointer pointerScanItem)
            {
                UpdatePointerAddress(hProcess, pointerScanItem);
                continue;
            }

            UpdateMemorySegmennt(hProcess, address);
        }
    }

    public void UpdateMemorySegmennt(IntPtr hProcess, IMemorySegment memorySegment)
    {
        if (memorySegment == null)
            return;

        var typeSize = memorySegment.ScanDataType.GetPrimitiveSize();
        var buffer = _byteArrayPool.Rent(typeSize);
        var successful = TryReadVirtualMemory(hProcess, memorySegment.Address, (uint)typeSize, buffer);
        memorySegment.Value = successful ? buffer.ConvertToString(memorySegment.ScanDataType) : "???";
        _byteArrayPool.Return(buffer, clearArray: true);
    }

    //public static void UpdatePointerAddress(IntPtr hProcess, TrackedPointerScanItem trackedPointerScanItem)
    //{
    //    // TODO: This method should be refactored, calling DetermineAddressDisplayString just feels weird
    //    var buffer = new byte[sizeof(long)];

    //    NtReadVirtualMemory(
    //        hProcess,
    //        trackedPointerScanItem.Pointer.Address,
    //        buffer,
    //        (uint)buffer.Length,
    //        out _);

    //    var pointingAddress = (IntPtr)BitConverter.ToInt64(buffer);

    //    for (var i = trackedPointerScanItem.Pointer.Offsets.Count - 1; i >= 0; i--)
    //    {
    //        var offset = trackedPointerScanItem.Pointer.Offsets[i].ToInt32();

    //        if (i == 0)
    //        {
    //            pointingAddress = pointingAddress + offset;
    //            break;
    //        }

    //        NtReadVirtualMemory(
    //        hProcess,
    //        pointingAddress + offset,
    //        buffer,
    //        (uint)buffer.Length,
    //        out _);

    //        pointingAddress = (IntPtr)BitConverter.ToInt64(buffer);
    //    }

    //    trackedPointerScanItem.ScanItem.BaseAddress = pointingAddress;
    //    trackedPointerScanItem.DetermineAddressDisplayString();
    //}

    public void UpdatePointerAddress(IntPtr hProcess, IPointer? pointerAddress)
    {
        if (pointerAddress == null)
            return;

        ResolvePointerPath(hProcess, pointerAddress);
        
        var typeSize = pointerAddress.ScanDataType.GetPrimitiveSize();
        var buffer = _byteArrayPool.Rent(typeSize);
        var successful = TryReadVirtualMemory(hProcess, pointerAddress.PointingTo, (uint)typeSize, buffer);
        pointerAddress.Value = successful ? buffer.ConvertToString(pointerAddress.ScanDataType) : "???";
        _byteArrayPool.Return(buffer, clearArray: true);
    }

    public void ResolvePointerPath(IntPtr hProcess, IPointer pointerAddress)
    {
        var buffer = _byteArrayPool.Rent(sizeof(long));
        TryReadVirtualMemory(hProcess, pointerAddress.Address, sizeof(long), buffer);
        pointerAddress.PointingTo = (IntPtr)BitConverter.ToInt64(buffer);

        for (var i = pointerAddress.Offsets.Count - 1; i >= 0; i--)
        {
            var offset = pointerAddress.Offsets[i].ToInt32();

            if (i == 0)
            {
                pointerAddress.PointingTo += offset;
                break;
            }

            TryReadVirtualMemory(hProcess, pointerAddress.PointingTo + offset, sizeof(long), buffer);
            pointerAddress.PointingTo = (IntPtr)BitConverter.ToInt64(buffer);
        }

        _byteArrayPool.Return(buffer, clearArray: true);
    }

    public IList<VirtualMemoryRegion> GatherVirtualMemoryRegions(IntPtr hProcess)
    {
        if (hProcess == IntPtr.Zero)
            throw new ArgumentNullException(nameof(hProcess));

        QueryingMemoryregions(hProcess);

        var virtualMemoryRegions = new List<VirtualMemoryRegion>();
        GetSystemInfo(out var systemInfo);

        // IntPtr proc_min_address = (IntPtr)0x00007ff78cb10000;
        IntPtr proc_min_address = systemInfo.minimumApplicationAddress;
        IntPtr proc_max_address = systemInfo.maximumApplicationAddress;

        // saving the values as long ints so I won't have to do a lot of casts later
        ulong proc_min_address_l = (ulong)proc_min_address;
        ulong proc_max_address_l = (ulong)proc_max_address;

        // this will store any information we get from VirtualQueryEx()
        var memoryBasicInfos = new List<MEMORY_BASIC_INFORMATION64>();
        MEMORY_BASIC_INFORMATION64 mem_basic_info = new MEMORY_BASIC_INFORMATION64();
        MEMORY_BASIC_INFORMATION64 mem_basic_info2 = new MEMORY_BASIC_INFORMATION64();
        while (proc_min_address_l < proc_max_address_l)
        {
            // 28 = sizeof(MEMORY_BASIC_INFORMATION)
            //var result = VirtualQueryEx(pHandle, proc_min_address, out mem_basic_info, (uint)Marshal.SizeOf(mem_basic_info));
            var result = NtQueryVirtualMemory(
                hProcess,
                proc_min_address,
                (int)MEMORY_INFORMATION_CLASS.MemoryBasicInformation,
                ref mem_basic_info,
                Marshal.SizeOf(mem_basic_info),
                out var returnLength
            );

            if (returnLength > 0)
            {

                if (IsWithinModule(hProcess, (IntPtr)mem_basic_info.BaseAddress, out string moduleName) && !IsSystemModule(hProcess, (IntPtr)mem_basic_info.BaseAddress, out var aaa))
                {

                }
            }

            // if this memory chunk is accessible
            if (returnLength > 0 && mem_basic_info.Protect == (uint)MEMORY_PROTECTION.PAGE_READWRITE && mem_basic_info.State == (uint)MEMORY_STATE.MEM_COMMIT)
            {
                

                //VirtualProtectEx(pHandle, new IntPtr((long)mem_basic_info.BaseAddress), new UIntPtr(mem_basic_info.RegionSize), 0x40, out var prt);
                memoryBasicInfos.Add(mem_basic_info);
                if (TryReadVirtualMemory(hProcess, (IntPtr)mem_basic_info.BaseAddress, (uint)mem_basic_info.RegionSize, out var memoryBytes))
                {
                    var virtualMemoryRegion = new VirtualMemoryRegion((IntPtr)mem_basic_info.BaseAddress, mem_basic_info.RegionSize, memoryBytes);
                    virtualMemoryRegions.Add(virtualMemoryRegion);
                }
            }

            // move to the next memory chunk
            proc_min_address_l += mem_basic_info.RegionSize;
            proc_min_address = new IntPtr((long)proc_min_address_l);
        }

        return virtualMemoryRegions;
    }

    private bool _useStacks = true;
    private int _threadStacks = 2;
    private List<IntPtr> _stackList = new(2);
    private IntPtr _hProcess = IntPtr.Zero;

    private void FillTheStackList()
    {
        for (int i = 0; i < _threadStacks; i++)
        {
            _stackList.Add(GetStackStart(i));

            if (_stackList[i] == IntPtr.Zero)
            {
                _threadStacks = i;
                break;
            }
        }
    }

    private IntPtr GetStackStart(int threadNr)
    {
        MODULEINFO mi = new MODULEINFO();

        // this is actually capsulated perfectly, still have it here for now
        {
            var kernel32Handle = GetModuleHandle("kernel32.dll");

            if (kernel32Handle != IntPtr.Zero)
            {
                GetModuleInformation(_hProcess, kernel32Handle, out mi, (uint)Marshal.SizeOf(typeof(MODULEINFO)));
            }
        }

        var stackStart = IntPtr.Zero;
        var pId = GetProcessId(_hProcess);
        IntPtr hSnapshot = CreateToolhelp32Snapshot(CreateToolhelp32SnapshotFlags.TH32CS_SNAPTHREAD, 0);

        if (hSnapshot == IntPtr.Zero)
        {
            throw new Exception("Error taking snapshot");
        }

        THREADENTRY32 te32 = new THREADENTRY32();
        te32.dwSize = (uint)Marshal.SizeOf(typeof(THREADENTRY32));

        if (Thread32First(hSnapshot, ref te32))
        {
            // Print information about each thread in the snapshot
            do
            {
                if (te32.th32OwnerProcessID == pId)
                {
                    if (threadNr != 0)
                    {
                        threadNr--;
                        continue;
                    }

                    IntPtr hThread = OpenThread(ThreadAccess.QUERY_INFORMATION | ThreadAccess.GET_CONTEXT, false, te32.th32ThreadID);

                    if (hThread != IntPtr.Zero)
                    {
                        var stackTop = new byte[8];
                        var tbi = new THREAD_BASIC_INFORMATION();
                        var result = NtQueryInformationThread(hThread, ThreadInfoClass.ThreadBasicInformation, ref tbi, Marshal.SizeOf(tbi), out _);

                        if (result == NTSTATUS.Success)
                        {
                            NtReadVirtualMemory(_hProcess, tbi.TebBaseAddress + 8, stackTop, 8, out _);
                        }

                        CloseHandle(hThread);
                        var stackTopPointer = (IntPtr)BitConverter.ToInt64(stackTop);

                        if (stackTopPointer != IntPtr.Zero)
                        {
                            //find the stack entry pointing to the function that calls "ExitXXXXXThread"
                            //Fun thing to note: It's the first entry that points to a address in kernel32  

                            var buffer = _byteArrayPool.Rent(4096);

                            if (NtReadVirtualMemory(_hProcess, stackTopPointer - 4096, buffer, 4096, out _) == NTSTATUS.Success)
                            {
                                for (int i = (4096 / 8) - 1; i >= 0; i--)
                                {
                                    var buffAddress = (IntPtr)BitConverter.ToUInt64(buffer, i * 8);

                                    if (InRange(buffAddress, mi.lpBaseOfDll, mi.lpBaseOfDll + (int)mi.SizeOfImage))
                                    {
                                        stackStart = stackTopPointer - 4096 + i * 8;
                                    }
                                }
                            }

                            _byteArrayPool.Return(buffer);
                        }
                    }

                    break;
                }
            } while (Thread32Next(hSnapshot, ref te32));

            CloseHandle(hSnapshot);
        }

        return stackStart;
    }

    private bool InRange(IntPtr value, IntPtr min, IntPtr max)
    {
        return value >= min && value <= max;
    }

    public void QueryingMemoryregions(IntPtr hProcess)
    {
        _hProcess = hProcess;
        FillTheStackList();
        ulong currentAddress = 0x0;
        ulong stopAddress = 0x7FFFFFFFFFFFFFFF;
        MEMORY_BASIC_INFORMATION64 mbi = new MEMORY_BASIC_INFORMATION64();
        var memoryRegions = new List<MemoryRegion>();

        while (VirtualQueryEx(hProcess, (IntPtr)currentAddress, out mbi, (uint)Marshal.SizeOf(mbi)) != 0 && currentAddress < stopAddress && (currentAddress + mbi.RegionSize) > currentAddress) 
        {
            if (!IsSystemModule(hProcess, (IntPtr)mbi.BaseAddress, out _) 
                && mbi.State == (uint)MEMORY_STATE.MEM_COMMIT
                && (mbi.Type & 0x40000) == 0
                && (mbi.Protect & (uint)MEMORY_PROTECTION.PAGE_GUARD) == 0
                && (mbi.Protect & (uint)MEMORY_PROTECTION.PAGE_NOACCESS) == 0
                )
            {
                var valid = false;

                if ((mbi.AllocationProtect & (uint)MEMORY_PROTECTION.PAGE_WRITECOMBINE) == (uint)MEMORY_PROTECTION.PAGE_WRITECOMBINE
                    || (mbi.Protect & (uint)(MEMORY_PROTECTION.PAGE_READONLY | MEMORY_PROTECTION.PAGE_EXECUTE | MEMORY_PROTECTION.PAGE_EXECUTE_READ)) != 0)
                {
                    valid = false;
                }
                else
                {
                    valid = true;
                }

                var memoryRegion = new MemoryRegion();
                memoryRegion.BaseAddress = (IntPtr)mbi.BaseAddress;
                memoryRegion.MemorySize = mbi.RegionSize;
                memoryRegion.InModule = IsWithinModule(hProcess, (IntPtr)mbi.BaseAddress, out _);
                memoryRegion.ValidPointerRange = valid;
                memoryRegions.Add(memoryRegion);
            }

            currentAddress = mbi.BaseAddress + mbi.RegionSize;
        }


        if (memoryRegions.Count == 0)
        {
            Console.WriteLine("No memory found in the specified region");
            throw new Exception("No memory found in the specified region");
        }

        ConcatMemoryRegions(memoryRegions);
        MakeMemoryRegionChunks(memoryRegions);
        memoryRegions.Sort((region1, region2) => region1.BaseAddress.CompareTo(region2.BaseAddress));

        // init scan
        var buffer = new byte[memoryRegions.Max(x => x.MemorySize)];
        var buffer2 = new byte[memoryRegions.Max(x => x.MemorySize)];
        const int pointerSize = 8;

        var test = new byte[8] { 16, 147, 11, 0, 0, 0, 0, 0 };
        var level0list = new ReversePointerTable[16];

        foreach (var validMemRegion in memoryRegions.Where(x => x.ValidPointerRange))
        {
            if (NtReadVirtualMemory(hProcess, validMemRegion.BaseAddress, buffer, (uint)validMemRegion.MemorySize, out var actualRead) == NTSTATUS.Success)
            {
                var lastAddress = (int)validMemRegion.MemorySize - pointerSize;

                for (var i = 0; i <= lastAddress; i += 4)
                {
                    var qwordPointer = (IntPtr)BitConverter.ToUInt64(buffer, i);

                    if (qwordPointer % 4 == 0 && IsPointer(qwordPointer, memoryRegions))
                    {
                        var validPointer = true;

                        AddPointer(qwordPointer, validMemRegion.BaseAddress + i, false, level0list);
                    }
                }

                var aaa = "";
            }
        }

        foreach (var validMemRegion in memoryRegions.Where(x => x.ValidPointerRange))
        {
            if (NtReadVirtualMemory(hProcess, validMemRegion.BaseAddress, buffer, (uint)validMemRegion.MemorySize, out var actualRead) == NTSTATUS.Success)
            {
                var lastAddress = (int)validMemRegion.MemorySize - pointerSize;

                for (var i = 0; i <= lastAddress; i += 4)
                {
                    var qwordPointer = (IntPtr)BitConverter.ToUInt64(buffer, i);

                    if (qwordPointer % 4 == 0 && IsPointer(qwordPointer, memoryRegions))
                    {
                        var validPointer = true;

                        AddPointer(qwordPointer, validMemRegion.BaseAddress + i, true, level0list);
                    }
                }

                var aaa = "";
            }
        }
    }

    public class ReversePointerTable
    {
        public PointerList? PointerList { get; set; }
        public ReversePointerTable[]? ReversePointerlistArray { get; set; }
    }

    public class PointerList
    {
        public int MaxSize { get; set; }
        public int ExpectedSize { get; set; }
        public int Pos { get; set; }
        public List<PointerData>? List { get; set; }

        //Linked list
        public IntPtr PointerValue { get; set; }
        public PointerList? Previous { get; set; }
        public PointerList? Next { get; set; }
    }

    public class PointerData
    {
        public IntPtr Address { get; set; }
        public StaticData? StaticData { get; set; }
    }

    public class StaticData
    {
        public ulong ModuleIndex { get; set; }
        public int Index { get; set; }
    }

    private void AddPointer(IntPtr pointerValue, IntPtr pointerWithTHisValue, bool add, ReversePointerTable[] level0list)
    {
        var plist = FindOrAddPointerValue(pointerValue, level0list);

        if (!add)
        {
            plist.ExpectedSize += 1;
            return;
        }
        else
        {
            if (plist.List == null)
            {
                plist.List = new List<PointerData>(plist.ExpectedSize);
                plist.MaxSize = plist.ExpectedSize;
            }

            if (plist.List[plist.Pos] == null)
            {
                plist.List[plist.Pos] = new PointerData();
            }

            plist.List[plist.Pos].Address = pointerWithTHisValue;
        }
    }

    private void isStatic(IntPtr address, out IntPtr moduleBaseAddress, out int moduleIndex)
    {
        const int stackSize = 4096;
        var isStack = false;

        if (_useStacks)
        {
            for (var i = 0; i < _threadStacks - 1; i++)
            {
                if (InRange(address, _stackList[i] - stackSize, _stackList[i]))
                {
                    moduleBaseAddress = _stackList[i];
                    isStack = true;
                }
            }
        }
    }

    private PointerList FindOrAddPointerValue(IntPtr pointerValue, ReversePointerTable[] level0list)
    {
        var currentArray = level0list;
        var level = 0;
        var maxLevel = 15;
        var entryNr = 0;

        while (level < maxLevel)
        {
            entryNr = (int)((pointerValue >> ((maxLevel - level) * 4)) & 0xF);

            if (currentArray[entryNr] == null)
            {
                currentArray[entryNr] = new ReversePointerTable();
            }

            if (currentArray[entryNr].ReversePointerlistArray == null)
            {
                currentArray[entryNr].ReversePointerlistArray = new ReversePointerTable[maxLevel + 16];
            }

            currentArray = currentArray[entryNr].ReversePointerlistArray!;
            level++;
        }

        entryNr = (int)((pointerValue >> ((maxLevel - level) * 4)) & 0xF);
        if (currentArray[entryNr] == null)
        {
            currentArray[entryNr] = new ReversePointerTable();
        }

        PointerList? plist = currentArray[entryNr].PointerList;
        if (plist == null)
        {
            plist = new PointerList();
            plist.PointerValue = pointerValue;
            plist.ExpectedSize = 1;

            if (pointerValue % 0x10 == 0)
            {
                plist.ExpectedSize = 5;

                if (pointerValue % 0x100 == 0)
                {
                    plist.ExpectedSize = 10;

                    if (pointerValue % 0x1000 == 0)
                    {
                        plist.ExpectedSize = 20;

                        if (pointerValue % 0x10000 == 0)
                        {
                            plist.ExpectedSize = 50;
                        }
                    }
                }
            }

            currentArray[entryNr].PointerList = plist;
        }

        return plist;
    }

    public int BinSearchMemRegions(IntPtr address, List<MemoryRegion> memoryRegions) 
    {
        int first = 0; // Sets the first item of the range
        int last = memoryRegions.Count - 1; // Sets the last item of the range
        bool found = false; // Initializes the Found flag (Not found yet)
        int result = -1; // Initializes the Result

        while (first <= last && !found)
        {
            // Gets the middle of the selected range
            int pivot = (first + last) / 2;

            // Compares the address with the memory region
            if (address >= memoryRegions[pivot].BaseAddress && address < memoryRegions[pivot].BaseAddress + (long)memoryRegions[pivot].MemorySize)
            {
                found = true;
                result = pivot;
            }
            // If the item in the middle has a bigger value than
            // the searched item, then select the first half
            else if (memoryRegions[pivot].BaseAddress > address)
            {
                last = pivot - 1;
            }
            // Else select the second half
            else
            {
                first = pivot + 1;
            }
        }

        return result;
    }

    private bool IsPointer(IntPtr qwordPointer, List<MemoryRegion> memoryRegions)
    {
        if (qwordPointer == 0)
        {
            return false;
        }

        var index = BinSearchMemRegions(qwordPointer, memoryRegions);

        if (index != -1 && memoryRegions[index].ValidPointerRange)
        {
            return true;
        }

        return false;
    }

    private void ConcatMemoryRegions(List<MemoryRegion> memoryRegions)
    {
        var j = 0;
        var address = memoryRegions[0].BaseAddress;
        var size = memoryRegions[0].MemorySize;
        var InModule = memoryRegions[0].InModule;
        var validPtrRange = memoryRegions[0].ValidPointerRange;

        for (var i = 1; i < memoryRegions.Count; i++)
        {
            // Only concatenate if classpointers is false, or the same type of executable field is used
            if ((memoryRegions[i].BaseAddress == address + (IntPtr)size) &&
                (memoryRegions[i].ValidPointerRange == validPtrRange) &&
                (memoryRegions[i].InModule == InModule))
            {
                size += memoryRegions[i].MemorySize;
            }
            else
            {
                memoryRegions[j].BaseAddress = address;
                memoryRegions[j].MemorySize = size;
                memoryRegions[j].InModule = InModule;
                memoryRegions[j].ValidPointerRange = validPtrRange;

                address = memoryRegions[i].BaseAddress;
                size = memoryRegions[i].MemorySize;
                InModule = memoryRegions[i].InModule;
                validPtrRange = memoryRegions[i].ValidPointerRange;
                j++;
            }
        }

        memoryRegions[j].BaseAddress = address;
        memoryRegions[j].MemorySize = size;
        memoryRegions[j].InModule = InModule;
        memoryRegions[j].ValidPointerRange = validPtrRange;
        memoryRegions.RemoveRange(j + 1, memoryRegions.Count - j - 1);
    }

    private void MakeMemoryRegionChunks(List<MemoryRegion> memoryRegions)
    {
        const int chunkSize = 512 * 1024; // 512KB

        for (int i = 0; i < memoryRegions.Count; i++)
        {
            if (memoryRegions[i].MemorySize > chunkSize)
            {
                // Too big, so cut into pieces
                // Create new entry with 512KB less
                memoryRegions.Add(new MemoryRegion
                {
                    BaseAddress = memoryRegions[i].BaseAddress + chunkSize,
                    MemorySize = memoryRegions[i].MemorySize - chunkSize,
                    InModule = memoryRegions[i].InModule,
                    ValidPointerRange = memoryRegions[i].ValidPointerRange
                });

                memoryRegions[i].MemorySize = chunkSize; // Set the current region to be 512KB
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MODULEINFO
    {
        public IntPtr lpBaseOfDll;
        public uint SizeOfImage;
        public IntPtr EntryPoint;
    }

    //TODO: this could be improved on, cuz isSystemModule is actually doing the same
    static bool IsWithinModule(IntPtr hProcess, IntPtr baseAddress, out string moduleName)
    {
        moduleName = null;

        // Get the module information for the specified base address
        MODULEINFO moduleInfo;
        if (GetModuleInformation(hProcess, baseAddress, out moduleInfo, (uint)Marshal.SizeOf(typeof(MODULEINFO))))
        {
            // Get the module file name
            StringBuilder moduleNameBuilder = new StringBuilder(260); // MAX_PATH
            bool moduleNameLength = GetModuleFileNameEx(hProcess, moduleInfo.lpBaseOfDll, moduleNameBuilder, 260);

            if (moduleNameLength)
            {
                moduleName = moduleNameBuilder.ToString();
                return true;
            }
        }

        return false;
    }

    static bool IsSystemModule(IntPtr hProcess, IntPtr baseAddress, out string moduleName)
    {
        const int MAX_PATH = 260;
        moduleName = "";
        // Attempt to get the module file name for the specified base address
        StringBuilder moduleNameBuilder = new StringBuilder(MAX_PATH);
        if (!GetModuleFileNameEx(hProcess, baseAddress, moduleNameBuilder, MAX_PATH))
        {
            return false;
        }
        moduleName = moduleNameBuilder.ToString();
        // Check if the module file name contains a system directory (e.g., C:\Windows)
        return moduleNameBuilder.ToString().ToLower().Contains("system32");
    }
}
