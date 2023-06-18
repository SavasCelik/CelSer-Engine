using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CelSerEngine.Core.Scanners;

public class PointerScanner
{
    private readonly INativeApi _nativeApi;
    private readonly int _pointerSize;
    private readonly ArrayPool<byte> _byteArrayPool;

    public PointerScanner(INativeApi nativeApi)
    {
        _byteArrayPool = ArrayPool<byte>.Shared;
        _nativeApi = nativeApi;
        _pointerSize = sizeof(long);
    }

    public async Task<IList<Pointer>> ScanForPointersAsync(PointerScanOptions pointerScanOptions)
    {
        var result = await Task.Run(() =>
        {
            var staticPointers = GetStaticPointers(pointerScanOptions.ProcessId, pointerScanOptions.ProcessHandle);
            var staticPointersByAddress = staticPointers.ToDictionary(x => x.Address);
            var heapPointers = GetHeapPointers(pointerScanOptions.ProcessHandle);
            var heapPointersByPointingTo = heapPointers
                .OrderBy(x => x.PointingTo)
                .GroupBy(x => x.PointingTo)
                .ToDictionary(x => x.Key, x => x.ToArray());
            var pointsWhereIWant = heapPointers
                .Where(x => x.Address.ToInt64() >= pointerScanOptions.SearchedAddress - pointerScanOptions.MaxOffset && x.Address.ToInt64() <= pointerScanOptions.SearchedAddress)
                .ToArray();
            var pointerScan1 = new List<Pointer>();

            foreach (var pointer in pointsWhereIWant)
            {
                var firstOffset = pointerScanOptions.SearchedAddress - pointer.Address;
                pointer.Offsets.Add(firstOffset);
                pointer.PointingTo = pointerScanOptions.SearchedAddress;

                if (heapPointersByPointingTo.TryGetValue(pointer.Address, out var pointers))
                {
                    var clonedPointers = pointers.Select(x => x.Clone()).ToList();
                    clonedPointers.ForEach(p => p.Offsets.Add(firstOffset));
                    pointerScan1.AddRange(clonedPointers);
                }
            }

            var pointingThere = new List<Pointer>();
            var counter = new Dictionary<IntPtr, int>();
            var alreadyTracking = new HashSet<IntPtr>();

            for (var currentLevel = 0; currentLevel < pointerScanOptions.MaxLevel; currentLevel++)
            {
                var pointerList = pointerScan1.OrderBy(x => x.Offsets.Last()).ToArray();
                pointerScan1.Clear();
                foreach (var pointer in pointerList)
                {
                    if (staticPointersByAddress.TryGetValue(pointer.Address, out var startingPoint))
                    {
                        var newPointingThere = new Pointer
                        {
                            ModuleName = startingPoint.ModuleName,
                            BaseAddress = startingPoint.BaseAddress,
                            BaseOffset = startingPoint.BaseOffset,
                            Offsets = pointer.Offsets,
                            PointingTo = pointerScanOptions.SearchedAddress
                        };
                        pointingThere.Add(newPointingThere);
                    }

                    if (alreadyTracking.Contains(pointer.Address))
                    {
                        continue;
                    }

                    if (currentLevel == pointerScanOptions.MaxLevel - 1)
                    {
                        continue;
                    }

                    //alreadyTracking.Add(pointer.Address);

                    var newAddy = IntPtr.Zero;
                    for (int i = 0; i < pointerScanOptions.MaxOffset; i += _pointerSize)
                    {

                        newAddy = pointer.Address - i;
                        if (heapPointersByPointingTo.TryGetValue(newAddy, out var pointers))
                        {
                            var clonedPointers = pointers.Select(x => x.Clone()).ToList();
                            var offsets = new List<IntPtr>(pointer.Offsets)
                        {
                            i
                        };
                            clonedPointers.ForEach(x => x.Offsets = new List<IntPtr>(offsets));
                            pointerScan1.AddRange(clonedPointers);
                            var countingFound = counter.TryGetValue(newAddy, out int count);
                            if (!staticPointersByAddress.ContainsKey(clonedPointers.First().Address) && countingFound && count >= 3)
                            {
                                heapPointersByPointingTo.Remove(newAddy);
                            }
                            else
                            {
                                count++;
                                if (!countingFound)
                                {
                                    counter.Add(newAddy, count);
                                }
                                counter[newAddy] = count;
                            }
                        }
                    }
                }
            }

            return pointingThere.OrderBy(x => x.Offsets.Count).ToList();
        }).ConfigureAwait(false);

        return result;
    }

    public async Task<IList<Pointer>> RescanPointersAsync(IEnumerable<Pointer> pointers, int processId, IntPtr processHandle, IntPtr searchedAddress)
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

    private IList<Pointer> GetStaticPointers(int processId, IntPtr processHandle)
    {
        var mainModule = _nativeApi.GetProcessMainModule(processId);
        var listOfBaseAddresses = new List<Pointer>();
        var buffer = _byteArrayPool.Rent(_pointerSize);

        for (var currentSize = 0; currentSize < mainModule.Size; currentSize += _pointerSize)
        {
            _nativeApi.ReadVirtualMemory(processHandle, mainModule.BaseAddress + currentSize, (uint)_pointerSize, buffer);
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

    private IList<Pointer> GetHeapPointers(IntPtr processHandle)
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
