using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Collections.Concurrent;

namespace CelSerEngine.Core.Scanners;

public class PointerScanner
{
    private readonly INativeApi _nativeApi;
    private readonly int _pointerSize;
    private readonly ArrayPool<byte> _byteArrayPool;
    private List<Pointer> _pointerList;
    private IDictionary<IntPtr, Pointer> _staticPointersByAddress;
    private IDictionary<IntPtr, Pointer[]> _heapPointersByPointingTo;
    private IDictionary<IntPtr, int> _pathCounter;

    public PointerScanner(INativeApi nativeApi)
    {
        _byteArrayPool = ArrayPool<byte>.Shared;
        _nativeApi = nativeApi;
        _pointerSize = sizeof(long);
        _pointerList = new List<Pointer>();
        _staticPointersByAddress = new Dictionary<IntPtr, Pointer>();
        _heapPointersByPointingTo = new Dictionary<IntPtr, Pointer[]>();
        _pathCounter = new Dictionary<IntPtr, int>();
    }

    public async Task<IList<Pointer>> ScanForPointersAsync(PointerScanOptions pointerScanOptions, CancellationToken token = default)
    {
        var result = await Task.Run(() => ScanPointers(pointerScanOptions, token, useParallel: false)).ConfigureAwait(false);
        return result;
    }

    public async Task<IList<Pointer>> ScanForPointersParallelAsync(PointerScanOptions pointerScanOptions, CancellationToken token = default)
    {
        var result = await Task.Run(() => ScanPointers(pointerScanOptions, token, useParallel: true)).ConfigureAwait(false);
        return result;
    }

    private void SetupPointerScan(PointerScanOptions pointerScanOptions)
    {
        var staticPointers = GetStaticPointers(pointerScanOptions.ProcessId, pointerScanOptions.ProcessHandle);
        _staticPointersByAddress = staticPointers.ToDictionary(x => x.Address);
        var heapPointers = GetHeapPointers(pointerScanOptions.ProcessHandle);
        _heapPointersByPointingTo = heapPointers
            .OrderBy(x => x.PointingTo)
            .GroupBy(x => x.PointingTo)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var pointersInRelevantRange = heapPointers
            .Where(x => x.Address.ToInt64() >= pointerScanOptions.SearchedAddress - pointerScanOptions.MaxOffset && x.Address.ToInt64() <= pointerScanOptions.SearchedAddress)
            .ToArray();
        _pointerList = new List<Pointer>();

        foreach (var pointer in pointersInRelevantRange)
        {
            var firstOffset = pointerScanOptions.SearchedAddress - pointer.Address;
            pointer.Offsets.Add(firstOffset);
            pointer.PointingTo = pointerScanOptions.SearchedAddress;

            if (_heapPointersByPointingTo.TryGetValue(pointer.Address, out var pointers))
            {
                foreach (var p in pointers)
                {
                    var clonedPointer = p.Clone();
                    clonedPointer.Offsets.Add(firstOffset);
                    _pointerList.Add(clonedPointer);
                }
            }
        }

        _pointerList = _pointerList.OrderBy(x => x.Offsets.Last()).ToList();
        _pathCounter = new ConcurrentDictionary<IntPtr, int>();
    }

    private IList<Pointer> ScanPointers(PointerScanOptions pointerScanOptions, CancellationToken token, bool useParallel)
    {
        SetupPointerScan(pointerScanOptions);
        var pointerWithStaticPointerPaths = new List<Pointer>();

        for (var currentLevel = 0; currentLevel < pointerScanOptions.MaxLevel; currentLevel++)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var pointerList = _pointerList.OrderBy(x => x.Offsets.Last()).ToArray();
            _pointerList.Clear();

            if (useParallel)
            {
                Parallel.ForEach(pointerList, (pointer) => ProcessPointer(pointer, pointerWithStaticPointerPaths, pointerScanOptions, currentLevel));
            }
            else
            {
                foreach (var pointer in pointerList)
                {
                    ProcessPointer(pointer, pointerWithStaticPointerPaths, pointerScanOptions, currentLevel);
                }
            }
        }

        return pointerWithStaticPointerPaths.OrderBy(x => x.Offsets.Count).ToList();
    }

    private void ProcessPointer(Pointer pointer, List<Pointer> pointerWithStaticPointerPaths, PointerScanOptions pointerScanOptions, int currentLevel)
    {
        if (_staticPointersByAddress.TryGetValue(pointer.Address, out var startingPoint))
        {
            var newPointingThere = new Pointer
            {
                ModuleName = startingPoint.ModuleName,
                BaseAddress = startingPoint.BaseAddress,
                BaseOffset = startingPoint.BaseOffset,
                Offsets = pointer.Offsets,
                PointingTo = pointerScanOptions.SearchedAddress
            };

            lock (pointerWithStaticPointerPaths)
            {
                pointerWithStaticPointerPaths.Add(newPointingThere);
            }
        }

        if (currentLevel == pointerScanOptions.MaxLevel - 1)
        {
            return;
        }

        var foundPointersWithOffset = FindHeapPointersWithOffsetWith(pointer, pointerScanOptions.MaxOffset);

        lock (_pointerList)
        {
            _pointerList.AddRange(foundPointersWithOffset);
        }
    }

    private List<Pointer> FindHeapPointersWithOffsetWith(Pointer pointer, int maxOffset)
    {
        var foundPointers = new List<Pointer>();

        for (int currentOffset = 0; currentOffset < maxOffset; currentOffset += _pointerSize)
        {
            var newAddress = pointer.Address - currentOffset;
            if (_heapPointersByPointingTo.TryGetValue(newAddress, out var pointers))
            {
                if (pointers == null)
                    continue;

                var offsets = new List<IntPtr>(pointer.Offsets)
                {
                    currentOffset
                };

                foreach (var foundPointer in pointers)
                {
                    var clonedPointer = foundPointer.Clone();
                    clonedPointer.Offsets = offsets.ToList();
                    foundPointers.Add(clonedPointer);
                }

                var countingFound = _pathCounter.TryGetValue(newAddress, out int count);
                if (!_staticPointersByAddress.ContainsKey(newAddress) && countingFound && count >= 3)
                {
                    lock (_heapPointersByPointingTo)
                    {
                        _heapPointersByPointingTo.Remove(newAddress);
                    }
                }
                else
                {
                    lock (_pathCounter)
                    {
                        _pathCounter[newAddress] = ++count;
                    }
                }
            }
        }

        return foundPointers;
    }

    public async Task<IList<Pointer>> RescanPointersAsync(IEnumerable<Pointer> pointers, int processId, SafeProcessHandle processHandle, IntPtr searchedAddress)
    {
        var result = await Task.Run(() =>
        {
            var staticPointers = GetStaticPointers(processId, processHandle);
            var staticPointersByAddress = staticPointers.ToDictionary(x => x.Address);
            var heapPointers = GetHeapPointers(processHandle);
            var heapPointerByAddress = heapPointers.GroupBy(x => x.Address).ToDictionary(x => x.Key, x => x.First());
            var foundPointers = new List<Pointer>();

            foreach (var resultItem in pointers)
            {
                if (staticPointersByAddress.TryGetValue(resultItem.Address, out var pointsTo))
                {
                    for (int i = resultItem.Offsets.Count - 1; i >= 0; i--)
                    {
                        var offset = resultItem.Offsets[i];
                        var addressWithOffset = pointsTo.PointingTo + offset.ToInt32();
                        if (addressWithOffset == searchedAddress)
                        {
                            foundPointers.Add(resultItem);
                        }
                        else if (heapPointerByAddress.TryGetValue(addressWithOffset, out var pointsToWithOffset))
                        {
                            pointsTo = pointsToWithOffset;
                        }
                    }
                }
            }

            return foundPointers;
        }).ConfigureAwait(false);

        return result;
    }

    private IList<Pointer> GetStaticPointers(int processId, SafeProcessHandle processHandle)
    {
        var mainModule = _nativeApi.GetProcessMainModule(processId);
        var listOfBaseAddresses = new List<Pointer>();
        var buffer = _byteArrayPool.Rent(_pointerSize);

        for (var currentSize = 0; currentSize < mainModule.Size; currentSize += _pointerSize)
        {
            if (!_nativeApi.TryReadVirtualMemory(processHandle, mainModule.BaseAddress + currentSize, (uint)_pointerSize, buffer))
                continue;

            var foundAddress = BitConverter.ToInt64(buffer);

            if (foundAddress == 0)
                continue;

            listOfBaseAddresses.Add(new Pointer
            {
                ModuleName = mainModule.Name,
                BaseAddress = mainModule.BaseAddress,
                BaseOffset = currentSize,
                PointingTo = (IntPtr)foundAddress
            });
        }

        _byteArrayPool.Return(buffer, clearArray: true);

        return listOfBaseAddresses;
    }

    private IList<Pointer> GetHeapPointers(SafeProcessHandle processHandle)
    {
        var virtualMemoryRegions = _nativeApi.GatherVirtualMemoryRegions(processHandle);
        var allAddresses = new List<Pointer>();

        foreach (var virtualMemoryRegion in virtualMemoryRegions)
        {
            var regionBytesAsSpan = virtualMemoryRegion.Bytes.AsSpan();
            for (int i = 0; i < (int)virtualMemoryRegion.RegionSize; i += _pointerSize)
            {
                if (i + _pointerSize > (int)virtualMemoryRegion.RegionSize)
                    continue;

                var bufferValue = BitConverter.ToInt64(regionBytesAsSpan[i..]);
                var pointer = new Pointer
                {
                    BaseAddress = virtualMemoryRegion.BaseAddress,
                    BaseOffset = i,
                    PointingTo = new IntPtr(bufferValue)
                };


                if (pointer.PointingTo == 0 || pointer.Address % 4 != 0)
                    continue;

                allAddresses.Add(pointer);
            }
        }

        return allAddresses;
    }
}
