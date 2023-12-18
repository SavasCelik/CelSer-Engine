using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using Moq;
using System.Text.Json;
using Xunit;

namespace CelSerEngine.Core.IntegrationTests.ScannerTests;

public class PointerScannerTests
{
    [Fact]
    public async Task PointerScanner_Should_Find_Pointer_With_Expected_Offsets()
    {
        var processId = 123;
        var processHandle = new IntPtr(0x1337);
        var searchedAddress = new IntPtr(0x1526B78);
        var expectedOffsets = "10, 18, 0, 18";
        var moduleBaseAddress = new IntPtr(0x100000000);
        var moduleSize = (uint)0x344000;

        // first scan
        var jsonOprions = new JsonSerializerOptions();
        jsonOprions.Converters.Add(new IntPtrJsonConverter());
        var stubVirtualMemoryRegions = 
            JsonSerializer.Deserialize<IList<VirtualMemoryRegion>>(
                await File.ReadAllTextAsync("ScannerTests/PointerScannerData/VirtualMemoryRegions.json"),
                jsonOprions)!;

        var stubNativeApi = new Mock<INativeApi>();
        stubNativeApi
            .Setup(x => x.GetProcessMainModule(processId))
            .Returns(new ProcessModuleInfo("TestModule", moduleBaseAddress, moduleSize));
        stubNativeApi
            .Setup(x => x.GatherVirtualMemoryRegions(processHandle))
            .Returns(stubVirtualMemoryRegions);
        stubNativeApi
            .Setup(x => x.ReadVirtualMemory(processHandle, It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<byte[]>()))
            .Callback((IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer) =>
            {
                ReadVirtualMemoryImpl(hProcess, address, numberOfBytesToRead, buffer, stubVirtualMemoryRegions);
            });

        var pointerScanner = new PointerScanner(stubNativeApi.Object);
        var scanOptions = new PointerScanOptions()
        {
            ProcessId = processId,
            ProcessHandle = processHandle,
            MaxLevel = 4,
            MaxOffset = 0x2000,
            SearchedAddress = searchedAddress
        };

        var foundPointers = await pointerScanner.ScanForPointersAsync(scanOptions);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == expectedOffsets).ToList();
        //var expectedPointer = foundPointers.Where(x => x.Offsets.Count == 4 && x.Offsets[i]);
        var ll = foundPointers.Where(x => x.Address == 0x0000000100325b00).ToList();

        Assert.Single(expectedPointer);

        // Rescan
        foundPointers = new List<Pointer>(expectedPointer);
        var searchedAddressAfterRescan = new IntPtr(0x863AAE8);
        stubVirtualMemoryRegions =
            JsonSerializer.Deserialize<IList<VirtualMemoryRegion>>(
                await File.ReadAllTextAsync("ScannerTests/PointerScannerData/Rescan_VirtualMemoryRegions.json"),
                jsonOprions)!;

        stubNativeApi
            .Setup(x => x.GatherVirtualMemoryRegions(processHandle))
            .Returns(stubVirtualMemoryRegions);

        var foundPointersAfterRescan = await pointerScanner.RescanPointersAsync(foundPointers, processId, processHandle, searchedAddressAfterRescan);
        var expectedPointerAfterRescan = foundPointersAfterRescan.Where(x => x.OffsetsDisplayString == expectedOffsets).ToList();

        Assert.Single(expectedPointerAfterRescan);
    }

    private void ReadVirtualMemoryImpl(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer, IList<VirtualMemoryRegion> virtualMemoryRegions)
    {
        var foundRegions = virtualMemoryRegions
            .Where(x => (x.BaseAddress + (long)x.RegionSize) >= address
            && (address - x.BaseAddress) < (long)x.RegionSize
            && x.BaseAddress < address)
            .ToList();

        if (foundRegions.Count == 0)
            return;

        var region = foundRegions.Single();
        var offset = address - region.BaseAddress;
        Array.Copy(region.Bytes, offset.ToInt32(), buffer, 0, (int)numberOfBytesToRead);
    }
}
