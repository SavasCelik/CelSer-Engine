using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using CelSerEngine.Native;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace CelSerEngine.ViewModels;


public class PointerScanResult
{
    public IntPtr PointingTo { get; set; }
    public IntPtr Address { get; set; }
    public List<IntPtr> Offsets { get; set; } = new List<IntPtr>();
    public IntPtr BaseAddress { get; set; }
    public IntPtr BaseOffset { get; set; }

    public PointerScanResult Clone()
    {
        return new PointerScanResult
        {
            PointingTo = PointingTo,
            Address = Address,
            BaseAddress = BaseAddress,
            Offsets = Offsets.ToList(),
            BaseOffset = BaseOffset
        };
    }
}

public partial class PointerScanOptionsViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string pointerScanAddress;
    private readonly SelectProcessViewModel _selectProcessViewModel;

    public PointerScanOptionsViewModel(SelectProcessViewModel selectProcessViewModel)
    {
        pointerScanAddress = "";
        _selectProcessViewModel = selectProcessViewModel;
    }

    [RelayCommand]
    public void StartPointerScan()
    {
        // TODO: refactor this pile of lines ^^
        var selectedProcess = _selectProcessViewModel.SelectedProcess!;
        var readBytes = IntPtr.Zero;
        var maxSize = 4000;
        var baseAddress = selectedProcess.MainModule?.BaseAddress!;
        var regionSize = selectedProcess.MainModule?.ModuleMemorySize;
        var currentSize = 0;
        var sizeOfAddress = sizeof(long);
        var searchedAddress = long.Parse(PointerScanAddress, NumberStyles.HexNumber);
        var listOfBaseAddresses = new List<PointerScanResult>();

        while (currentSize < regionSize)
        {
            var buffer = NativeApi.ReadVirtualMemory(selectedProcess.GetProcessHandle(), (IntPtr)baseAddress + currentSize, (uint)sizeOfAddress);
            var foundAddress = BitConverter.ToInt64(buffer);

            if (foundAddress == long.Parse(PointerScanAddress, NumberStyles.HexNumber))
            {
            }
            listOfBaseAddresses.Add(new PointerScanResult
            {
                Address = (IntPtr)baseAddress + currentSize,
                BaseAddress = (IntPtr)baseAddress.Value.ToInt64(),
                BaseOffset = (IntPtr)currentSize,
                PointingTo = (IntPtr)foundAddress
            });

            currentSize += sizeOfAddress;
        }

        var baseAddressesByAddress = listOfBaseAddresses.ToDictionary(x => x.Address);


        var pages = NativeApi.GatherVirtualPages(selectedProcess.GetProcessHandle());
        var allAddresses = new List<PointerScanResult>();

        foreach (var page in pages)
        {
            for (int i = 0; i < (int)page.RegionSize; i += sizeOfAddress)
            {
                if (i + sizeOfAddress > (int)page.RegionSize)
                {
                    continue;
                }
                var bufferValue = BitConverter.ToInt64(page.Bytes, i);
                var entry = new PointerScanResult
                {
                    Address = new IntPtr((long)page.BaseAddress + i),
                    PointingTo = new IntPtr(bufferValue),
                    BaseAddress = new IntPtr((long)page.BaseAddress)
                };
                entry.Offsets.Add((IntPtr)i);
                allAddresses.Add(entry);
            }
        }

        allAddresses = allAddresses.Where(x => x.PointingTo != IntPtr.Zero && x.Address.ToInt64() % 4 == 0).ToList();
        var addyByPointingTo = allAddresses
            .Where(x => x.PointingTo.ToInt64() >= 0x10000 && x.PointingTo.ToInt64() <= 0x7ffffffeffff) // could use this on allAddresses (i think)
            .OrderBy(x => x.PointingTo)
            .GroupBy(x => x.PointingTo).ToDictionary(x => x.Key, x => x.ToArray());
        var pointsWhereIWant = allAddresses.Where(x => x.Address.ToInt64() >= searchedAddress - maxSize && x.Address.ToInt64() <= searchedAddress).ToArray();
        var pointerScan1 = new List<PointerScanResult>();

        foreach (var pointer in pointsWhereIWant)
        {
            var firstOffset = (IntPtr)(searchedAddress - pointer.Address.ToInt64());
            var intPtrs = new List<IntPtr>
            {
                firstOffset
            };
            pointer.Offsets = intPtrs;
            pointer.PointingTo = (IntPtr)searchedAddress;

            if (addyByPointingTo.TryGetValue(pointer.Address, out var pointers))
            {
                var clonedPointers = pointers.Select(x => x.Clone()).ToList();
                var offsets = new List<IntPtr>(pointer.Offsets);
                clonedPointers.ForEach(x => x.Offsets = new List<IntPtr>(offsets));
                pointerScan1.AddRange(clonedPointers);
            }
        }

        var currentPointers = new List<PointerScanResult>(pointerScan1);
        var pointingThere = new List<PointerScanResult>();
        var level = 4;
        var counter = new Dictionary<IntPtr, int>();

        for (var currentLevel = 0; currentLevel < level; currentLevel++)
        {
            var pointerList = pointerScan1.OrderBy(x => x.Offsets.Last()).ToArray();
            pointerScan1.Clear();
            foreach (var pointer in pointerList)
            {
                if (baseAddressesByAddress.TryGetValue(pointer.Address, out var startingPoint))
                {
                    var newPointingThere = new PointerScanResult
                    {
                        Address = startingPoint.Address,
                        BaseAddress = startingPoint.BaseAddress,
                        BaseOffset = startingPoint.BaseOffset,
                        Offsets = pointer.Offsets,
                        PointingTo = (IntPtr)searchedAddress
                    };
                    pointingThere.Add(newPointingThere);
                }

                if (currentLevel == level - 1)
                {
                    continue;
                }

                var newAddy = IntPtr.Zero;
                for (int i = 0; i < maxSize; i += 4)
                {

                    newAddy = pointer.Address - i;
                    if (addyByPointingTo.TryGetValue(newAddy, out var pointers))
                    {
                        var clonedPointers = pointers.Select(x => x.Clone()).ToList();
                        var offsets = new List<IntPtr>(pointer.Offsets);
                        offsets.Add((IntPtr)i);
                        clonedPointers.ForEach(x => x.Offsets = new List<IntPtr>(offsets));
                        pointerScan1.AddRange(clonedPointers);
                        var countingFound = counter.TryGetValue(newAddy, out int count);
                        if (!baseAddressesByAddress.ContainsKey(clonedPointers.First().Address) && countingFound && count >= 3)
                        {
                            addyByPointingTo.Remove(newAddy);
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

        var resultsts = pointingThere.OrderBy(x => x.Offsets.Count).ToArray();

        //-- rescan --//
        var nextAddy = IntPtr.Zero;
        listOfBaseAddresses.Clear();
        currentSize = 0;
        while (currentSize < regionSize)
        {
            var buffer = NativeApi.ReadVirtualMemory(selectedProcess.GetProcessHandle(), (IntPtr)baseAddress + currentSize, (uint)sizeOfAddress);
            var foundAddress = BitConverter.ToInt64(buffer);

            if (foundAddress == long.Parse(PointerScanAddress, NumberStyles.HexNumber))
            {
            }
            listOfBaseAddresses.Add(new PointerScanResult
            {
                Address = (IntPtr)baseAddress + currentSize,
                BaseAddress = (IntPtr)baseAddress.Value.ToInt64(),
                BaseOffset = (IntPtr)currentSize,
                PointingTo = (IntPtr)foundAddress
            });

            currentSize += sizeOfAddress;
        }

        baseAddressesByAddress = listOfBaseAddresses.ToDictionary(x => x.Address);
        allAddresses.Clear();

        foreach (var page in pages)
        {
            page.ReReadMemory(selectedProcess.GetProcessHandle());
            for (int i = 0; i < (int)page.RegionSize; i += sizeOfAddress)
            {
                if (i + sizeOfAddress > (int)page.RegionSize)
                {
                    continue;
                }
                var bufferValue = BitConverter.ToInt64(page.Bytes, i);
                var entry = new PointerScanResult
                {
                    Address = new IntPtr((long)page.BaseAddress + i),
                    PointingTo = new IntPtr(bufferValue),
                    BaseAddress = new IntPtr((long)page.BaseAddress)
                };
                entry.Offsets.Add((IntPtr)i);
                allAddresses.Add(entry);
            }
        }

        allAddresses = allAddresses.Where(x => x.PointingTo != IntPtr.Zero).ToList();
        var allAddresses2 = allAddresses.Where(x => x.PointingTo != IntPtr.Zero && x.Address.ToInt64() % 4 == 0 && x.PointingTo.ToInt64() % 4 == 0).ToList(); 

        var addyByAddress = allAddresses
            .Where(x => x.PointingTo.ToInt64() >= 0x10000 && x.PointingTo.ToInt64() <= 0x7ffffffeffff) // could use this on allAddresses (i think)
            .GroupBy(x => x.Address).ToDictionary(x => x.Key, x => x.First());

        var addyByAddress2 = allAddresses2
            .Where(x => x.PointingTo.ToInt64() >= 0x10000 && x.PointingTo.ToInt64() <= 0x7ffffffeffff) // could use this on allAddresses (i think)
            .GroupBy(x => x.Address).ToDictionary(x => x.Key, x => x.First());

        var foundPointer = new List<PointerScanResult>();
        foreach (var resultItem in resultsts)
        {
            if (baseAddressesByAddress.TryGetValue(resultItem.Address, out var pointsTo))
            {
                for (int i = resultItem.Offsets.Count - 1; i >= 0; i--)
                {
                    var offset = resultItem.Offsets[i];
                    var key = pointsTo.PointingTo + offset.ToInt32();
                    if (key == nextAddy)
                    {
                        foundPointer.Add(resultItem);
                    }
                    else if (addyByAddress.TryGetValue(key, out var pointsToWithOffset))
                    {
                        pointsTo = pointsToWithOffset;
                    }
                }
            }
        }

        var foundPointer2 = new List<PointerScanResult>();
        foreach (var resultItem in resultsts)
        {
            if (baseAddressesByAddress.TryGetValue(resultItem.Address, out var pointsTo))
            {
                for (int i = resultItem.Offsets.Count - 1; i >= 0; i--)
                {
                    var offset = resultItem.Offsets[i];
                    var key = pointsTo.PointingTo + offset.ToInt32();
                    if (key == nextAddy)
                    {
                        foundPointer2.Add(resultItem);
                    }
                    else if (addyByAddress2.TryGetValue(key, out var pointsToWithOffset))
                    {
                        pointsTo = pointsToWithOffset;
                    }
                }
            }
        }

        var resultsts2 = foundPointer.ToArray();

    }

    public bool ShowPointerScanDialog(string pointerScanAddress = "")
    {
        PointerScanAddress = pointerScanAddress;
        var pointerScanOpstionsDlg = new PointerScanOptions()
        {
            Owner = App.Current.MainWindow
        };


        return pointerScanOpstionsDlg.ShowDialog() ?? false;
    }
}
