using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Models.ObservableModels;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CelSerEngine.Wpf.ViewModels;

public partial class PointerScanResultsViewModel : ObservableRecipient
{
    [ObservableProperty]
    public List<Pointer> _foundPointers;

    private readonly SelectProcessViewModel _selectProcessViewModel;
    private int _pointerSize;
    private readonly ArrayPool<byte> _byteArrayPool;

    public PointerScanResultsViewModel(SelectProcessViewModel selectProcessViewModel)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _pointerSize = sizeof(long);
        _foundPointers = new List<Pointer>(0);
        _byteArrayPool = ArrayPool<byte>.Shared;
    }

    public void StartPointerScan(PointerScanOptionsViewModel pointerScanOptions)
    {
        var selectedProcess = _selectProcessViewModel.SelectedProcess!;
        var searchedAddress = long.Parse(pointerScanOptions.PointerScanAddress, NumberStyles.HexNumber);
        var staticPointers = GetStaticPointers(selectedProcess);
        var staticPointersByAddress = staticPointers.ToDictionary(x => x.Address);
        var heapPointers = GetHeapPointers(selectedProcess);
        var heapPointersByPointingTo = heapPointers
            .OrderBy(x => x.PointingTo)
            .GroupBy(x => x.PointingTo)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var pointsWhereIWant = heapPointers.Where(x => x.Address.ToInt64() >= searchedAddress - pointerScanOptions.MaxOffset && x.Address.ToInt64() <= searchedAddress).ToArray();
        var pointerScan1 = new List<Pointer>();

        foreach (var pointer in pointsWhereIWant)
        {
            var firstOffset = (IntPtr)(searchedAddress - pointer.Address.ToInt64());
            pointer.Offsets.Add(firstOffset);
            pointer.PointingTo = (IntPtr)searchedAddress;

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
                        PointingTo = (IntPtr)searchedAddress
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

                alreadyTracking.Add(pointer.Address);

                var newAddy = IntPtr.Zero;
                for (int i = 0; i < pointerScanOptions.MaxOffset; i += _pointerSize)
                {

                    newAddy = pointer.Address - i;
                    if (heapPointersByPointingTo.TryGetValue(newAddy, out var pointers))
                    {
                        var clonedPointers = pointers.Select(x => x.Clone()).ToList();
                        var offsets = new List<IntPtr>(pointer.Offsets);
                        offsets.Add((IntPtr)i);
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

        var resultsts = pointingThere.OrderBy(x => x.Offsets.Count).ToList();
        FoundPointers = resultsts;        
    }

    [RelayCommand]
    public void RescanScan(string nextAddress)
    {
        if (FoundPointers == null || FoundPointers.Count == 0)
            return;

        var selectedProcess = _selectProcessViewModel.SelectedProcess!;
        var nextAddy = (IntPtr)long.Parse(nextAddress, NumberStyles.HexNumber);
        var staticPointers = GetStaticPointers(selectedProcess);
        var staticPointersByAddress = staticPointers.ToDictionary(x => x.Address);
        var heapPointers = GetHeapPointers(selectedProcess);
        var heapPointerByAddress = heapPointers.GroupBy(x => x.Address).ToDictionary(x => x.Key, x => x.First());
        var foundPointer = new List<Pointer>();

        foreach (var resultItem in FoundPointers)
        {
            if (staticPointersByAddress.TryGetValue(resultItem.Address, out var pointsTo))
            {
                for (int i = resultItem.Offsets.Count - 1; i >= 0; i--)
                {
                    var offset = resultItem.Offsets[i];
                    var addressWithOffset = pointsTo.PointingTo + offset.ToInt32();
                    if (addressWithOffset == nextAddy)
                    {
                        foundPointer.Add(resultItem);
                    }
                    else if (heapPointerByAddress.TryGetValue(addressWithOffset, out var pointsToWithOffset))
                    {
                        pointsTo = pointsToWithOffset;
                    }
                }
            }
        }

        FoundPointers = foundPointer;
    }

    [RelayCommand]
    public void AddPointerToTrackedScanItem(Pointer? selectedItem)
    {
        if (selectedItem == null)
            return;

        var observablePointer = new ObservablePointer(selectedItem);
        //TODO: bad workaround for circular dependency Fix this later
        App.Current.Services.GetRequiredService<TrackedScanItemsViewModel>().TrackedScanItems.Add(new TrackedItem(observablePointer));
    }

    private IReadOnlyList<Pointer> GetStaticPointers(ProcessAdapter process)
    {
        var baseAddress = process.MainModule!.BaseAddress;
        var regionSize = process.MainModule!.ModuleMemorySize;
        var currentSize = 0;
        var listOfBaseAddresses = new List<Pointer>();
        var buffer = _byteArrayPool.Rent(_pointerSize);

        while (currentSize < regionSize)
        {
            NativeApi.ReadVirtualMemory(process.GetProcessHandle(), baseAddress + currentSize, (uint)_pointerSize, buffer);
            var foundAddress = BitConverter.ToInt64(buffer);
            listOfBaseAddresses.Add(new Pointer
            {
                ModuleName = process.MainModule.ModuleName ?? "",
                BaseAddress = baseAddress,
                BaseOffset = currentSize,
                PointingTo = (IntPtr)foundAddress
            });

            currentSize += _pointerSize;
        }

        _byteArrayPool.Return(buffer, clearArray: true);

        return listOfBaseAddresses;
    }

    private IReadOnlyList<Pointer> GetHeapPointers(ProcessAdapter process)
    {
        var pages = NativeApi.GatherVirtualPages(process.GetProcessHandle());
        var allAddresses = new List<Pointer>();

        foreach (var page in pages)
        {
            var pageSpan = page.Bytes.AsSpan();
            for (int i = 0; i < (int)page.RegionSize; i += _pointerSize)
            {
                if (i + _pointerSize > (int)page.RegionSize)
                {
                    continue;
                }
                var bufferValue = BitConverter.ToInt64(page.Bytes, i);
                var entry = new Pointer
                {
                    BaseAddress = new IntPtr((long)page.BaseAddress),
                    BaseOffset = i,
                    PointingTo = (IntPtr)bufferValue
                };
                allAddresses.Add(entry);
            }
        }

        return allAddresses
            .Where(x => x.PointingTo != IntPtr.Zero && x.Address.ToInt64() % 4 == 0)
            //.Where(x => x.PointingTo.ToInt64() >= 0x10000 && x.PointingTo.ToInt64() <= 0x7ffffffeffff)
            .ToArray();
    }

    public void ShowPointerScanResultsDialog()
    {
        var selectProcessWidnwow = new PointerScanResults();
        selectProcessWidnwow.Show();
    }
}
