using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using System.Buffers;
using System.Collections;

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
            var pointerScan1 = new HashSet<Pointer2>();

            foreach (var pointer in pointsWhereIWant)
            {
                var firstOffset = pointerScanOptions.SearchedAddress - pointer.Address;
                pointer.Offsets.Add(firstOffset);
                pointer.PointingTo = pointerScanOptions.SearchedAddress;

                if (heapPointersByPointingTo.TryGetValue(pointer.Address, out var pointers))
                {
                    var clonedPointers = pointers.Select(x => x.Clone()).ToList();
                    clonedPointers.ForEach(p => p.Offsets.Add(firstOffset));
                    //pointerScan1.AddRange(clonedPointers);
                    foreach (var cPointer in clonedPointers)
                    {
                        pointerScan1.Add(new Pointer2(cPointer.Address, cPointer.PointingTo, cPointer.Offsets.ToArray()));
                    }
                }
            }

            var pointingThere = new HashSet<Pointer>();
            var counter = new Dictionary<IntPtr, int>();
            var alreadyTracking = new HashSet<IntPtr>();

            for (var currentLevel = 0; currentLevel < pointerScanOptions.MaxLevel; currentLevel++)
            {
                var pointerList = pointerScan1.OrderBy(x => x.Offsets.Last()).ToArray();
                //var pointerList = pointerScan1.ToArray();
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
                            //pointerScan1.AddRange(clonedPointers);
                            foreach (var cPointer in clonedPointers)
                            {
                                pointerScan1.Add(new Pointer2(cPointer.Address, cPointer.PointingTo, cPointer.Offsets.ToArray()));
                            }

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


    #region stackoverflow

    ////readonly PointerScanController _controller;

    ////public PointerScanController Controller => _controller;

    //private PointerScanOptions _pointerScanOptions;
    //private List<Pointer> _cachedValues;

    //void SetOptions(PointerScanOptions pointerScanOptions)
    //{
    //    _pointerScanOptions = pointerScanOptions;
    //    _cachedValues = GetHeapPointers(pointerScanOptions.ProcessHandle).OrderBy(x => x.PointingTo).ToList();
    //}

    //public async Task ScanAsync(nint targetAddress)
    //{
    //    var staticPointers = GetStaticPointers(_pointerScanOptions.ProcessId, _pointerScanOptions.ProcessHandle);
    //    var staticPointersByAddress = staticPointers.ToDictionary(x => x.Address);
    //    var pointerLists = new List<PointerList>();
    //    // TODO: mayebe 3
    //    for (var i = 0; i < _pointerScanOptions.MaxLevel + 1; i++)
    //    {
    //        var newList = new PointerList { Level = i };
    //        pointerLists.Add(newList);
    //        if (i > 0)
    //        {
    //            newList.Previous = pointerLists[i - 1];
    //            pointerLists[i - 1].Next = newList;
    //        }
    //    }

    //    for (var i = 0; i < pointerLists.Count; i++)
    //    {
    //        var currentList = pointerLists[i];
    //        var previousList = i > 0 ? pointerLists[i - 1] : null;
    //        if (previousList == null)
    //        {
    //            // 1) Start walking up the struct
    //            for (var address = targetAddress; address >= targetAddress - _pointerScanOptions.MaxOffset; address -= 8)
    //            {
    //                // 2) Find all pointers that point to this address
    //                var parents = BinarySearchFindAll(new Pointer { PointingTo = address });

    //                // 3) Add all pointers to to the list;
    //                foreach (var parent in parents)
    //                {
    //                    if (staticPointersByAddress.TryGetValue(parent.Address, out var startingPoint))
    //                    {
    //                        var newPointingThere = new Pointer
    //                        {
    //                            ModuleName = startingPoint.ModuleName,
    //                            BaseAddress = startingPoint.BaseAddress,
    //                            BaseOffset = startingPoint.BaseOffset,
    //                            Offsets = parent.Offsets,
    //                            PointingTo = targetAddress
    //                        };
    //                        currentList.Results.Add(newPointingThere);
    //                    }
    //                    else
    //                    {
    //                        currentList.Pointers.Add(parent);
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            // 1) Run through all potential pointers in the previous level.
    //            await Parallel
    //                .ForEachAsync(previousList.Pointers,
    //                              new ParallelOptions { MaxDegreeOfParallelism = 8 },
    //                              (pointer, token) =>
    //                              {
    //                                  var nodeDepth = 0;
    //                                  // 2) Start walking up the struct
    //                                  for (var address = pointer.Address;
    //                                       address >= pointer.Address - _pointerScanOptions.MaxOffset;
    //                                       address -= 8)
    //                                  {
    //                                      // 3) Find all pointers that point to this address
    //                                      var parents = BinarySearchFindAll(new Pointer { PointingTo = address });

    //                                      nodeDepth++;

    //                                      // 4) Add all pointers to to the list;
    //                                      foreach (var parent in parents)
    //                                      {
    //                                          if (staticPointersByAddress.TryGetValue(parent.Address, out var startingPoint))
    //                                          {
    //                                              var newPointingThere = new Pointer
    //                                              {
    //                                                  ModuleName = startingPoint.ModuleName,
    //                                                  BaseAddress = startingPoint.BaseAddress,
    //                                                  BaseOffset = startingPoint.BaseOffset,
    //                                                  Offsets = parent.Offsets,
    //                                                  PointingTo = targetAddress
    //                                              };
    //                                              currentList.Results.Add(newPointingThere);
    //                                          }

    //                                          if (currentList.Next == null)
    //                                              continue;

    //                                          lock (currentList.Pointers)
    //                                          {
    //                                              if (!currentList.Pointers.Contains(parent))
    //                                              {
    //                                                  currentList.Pointers.Add(parent);
    //                                              }
    //                                          }
    //                                      }

    //                                      if (nodeDepth > 3)//settings.MaxOffsetNodes)
    //                                          break;
    //                                  }

    //                                  return default;
    //                              });
    //        }

    //        Console.WriteLine($"Pointers Level {i} -- {pointerLists[i].Pointers.Count:#,###} pointers.");
    //    }

    //    foreach (var list in pointerLists)
    //        list.FinalizeToList();

    //    foreach (var l in pointerLists)
    //    {
    //        foreach (var result in l.Results)
    //        {
    //            var regionIx = _controller.GetBlockIndexFromAddress(result.Key.Address, false);
    //            var module = _controller.MemoryRegions[regionIx].Module;
    //            FindResultPointer(targetAddress, 0, result.Key, result.Key, l.Previous, new List<int> { (int)(result.Key.Address - module!.BaseAddress) });
    //        }
    //    }

    //    var r = _controller.Results;
    //    var maxOffset = r.Max(x => x.Offsets.Length);

    //    var sorted = r.OrderBy(x => true);
    //    for (var i = maxOffset - 1; i >= 0; i--)
    //    {
    //        var offsetIndex = i;

    //        //This is really hacky, but I want the main 1st set of offsets to be sorted and make sure 
    //        //the main big offset is grouped together as much as possible.
    //        if (offsetIndex == 1)
    //        {
    //            offsetIndex = 0;
    //        }
    //        else if (offsetIndex == 0)
    //        {
    //            offsetIndex = 1;
    //        }
    //        sorted = sorted.ThenBy(x => x.Offsets.Length > offsetIndex ? x.Offsets[offsetIndex] : -1);
    //    }

    //    _controller.Results = sorted.ToList();
    //}

    //bool FindResultPointer(nint targetAddress, int currentLevel, Pointer mainPointer, Pointer pointer, PointerList? nextLevel, List<int> currentOffsets)
    //{
    //    if (nextLevel == null)
    //    {
    //        //The first pointer list is special because any results in it are direct and there's no previous list to build from.
    //        //Need to manually work it and add its results.
    //        if (currentLevel == 0 && (targetAddress - pointer.Value) <= _controller.Settings.MaxOffset)
    //        {
    //            currentOffsets.Add((int)(targetAddress - pointer.Value));
    //            var regionIx = _controller.GetBlockIndexFromAddress(mainPointer.Address, false);
    //            _controller.Results.Add(new PointerScanResult
    //            {
    //                Origin = mainPointer,
    //                Module = _controller.MemoryRegions[regionIx].Module!,
    //                Offsets = currentOffsets.Select(x => x).ToArray()
    //            });
    //            return true;
    //        }

    //        return false;
    //    }

    //    //1) Find the child pointer
    //    var baseChildIndex = nextLevel.PointersList.BinarySearch(new Pointer { Address = pointer.Value });
    //    if (baseChildIndex < 0)
    //        baseChildIndex = (~baseChildIndex);

    //    bool hadResult = false;

    //    //2) Loop through all potential children/offsets
    //    var depth = 0;
    //    for (var i = baseChildIndex; i < nextLevel.PointersList.Count; i++)
    //    {
    //        var child = nextLevel.PointersList[i];
    //        if (child.Address > pointer.Value + _controller.Settings.MaxOffset)
    //            break;

    //        currentOffsets.Add((int)(child.Address - pointer.Value));

    //        if (!FindResultPointer(targetAddress, currentLevel + 1, mainPointer, child, nextLevel.Previous, currentOffsets))
    //        {
    //            if (targetAddress - child.Value <= _controller.Settings.MaxOffset)
    //            {
    //                hadResult = true;

    //                currentOffsets.Add((int)(targetAddress - child.Value));
    //                var regionIx = _controller.GetBlockIndexFromAddress(mainPointer.Address, true);

    //                _controller.Results.Add(new PointerScanResult
    //                {
    //                    Origin = mainPointer,
    //                    Module = _controller.MemoryRegions[regionIx].Module!,
    //                    Offsets = currentOffsets.Select(x => x).ToArray()
    //                });
    //                currentOffsets.RemoveAt(currentOffsets.Count - 1);
    //            }
    //        }
    //        else
    //        {
    //            hadResult = true;
    //        }

    //        currentOffsets.RemoveAt(currentOffsets.Count - 1);
    //    }

    //    return hadResult;
    //}

    //public class PointerComparer : Comparer<Pointer>
    //{
    //    // Call CaseInsensitiveComparer.Compare with the parameters reversed.
    //    public override int Compare(Pointer x, Pointer y)
    //    {
    //        return ((new CaseInsensitiveComparer()).Compare(x.PointingTo, y.PointingTo));
    //    }
    //}

    //List<Pointer> BinarySearchFindAll(Pointer serachedPointer)
    //{
    //    List<Pointer> pointers = new List<Pointer>();
    //    int index = _cachedValues.BinarySearch(serachedPointer, new PointerComparer());

    //    if (index >= 0)
    //    {
    //        // Find the first occurrence in case of duplicates
    //        while (index > 0 && _cachedValues[index - 1].PointingTo == serachedPointer.PointingTo)
    //        {
    //            index--;
    //        }

    //        // Collect all occurrences
    //        while (index < _cachedValues.Count && _cachedValues[index].PointingTo == serachedPointer.PointingTo)
    //        {
    //            pointers.Add(_cachedValues[index]);
    //            index++;
    //        }
    //    }

    //    return pointers;
    //}

    #endregion

}
