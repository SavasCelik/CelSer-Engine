using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using Moq;
using System.Text.Json;
using Xunit;

namespace CelSerEngine.Core.IntegrationTests.ScannerTests;

public class PointerScannerTests
{
    private PointerScanner _pointerScanner;
    private PointerScanOptions _scanOptions;
    private string _expectedOffsets;
    private Mock<INativeApi> _stubNativeApi;
    private JsonSerializerOptions _jsonSerializerOptions;
    private IntPtr _processHandle;
    private int _processId;

    public PointerScannerTests()
    {
        _processId = 123;
        _processHandle = new IntPtr(0x1337);
        var searchedAddress = new IntPtr(0x1526B78);
        var moduleBaseAddress = new IntPtr(0x100000000);
        var moduleSize = (uint)0x344000;
        _expectedOffsets = "10, 18, 0, 18";

        // first scan
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new IntPtrJsonConverter());
        var stubVirtualMemoryRegions =
            JsonSerializer.Deserialize<IList<VirtualMemoryRegion>>(
                File.ReadAllText("ScannerTests/PointerScannerData/VirtualMemoryRegions.json"),
                _jsonSerializerOptions)!;

        _stubNativeApi = new Mock<INativeApi>();
        _stubNativeApi
            .Setup(x => x.GetProcessMainModule(_processId))
            .Returns(new ProcessModuleInfo("TestModule", moduleBaseAddress, moduleSize));
        _stubNativeApi
            .Setup(x => x.GatherVirtualMemoryRegions(_processHandle))
            .Returns(stubVirtualMemoryRegions);
        _stubNativeApi
            .Setup(x => x.ReadVirtualMemory(_processHandle, It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<byte[]>()))
            .Callback((IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer) =>
            {
                ReadVirtualMemoryImpl(hProcess, address, numberOfBytesToRead, buffer, stubVirtualMemoryRegions);
            });

        _pointerScanner = new PointerScanner(_stubNativeApi.Object);
        _scanOptions = new PointerScanOptions()
        {
            ProcessId = _processId,
            ProcessHandle = _processHandle,
            MaxLevel = 4,
            MaxOffset = 0x2000,
            SearchedAddress = searchedAddress
        };
    }

    [Fact]
    public async Task PointerScanner_Should_Find_Pointer_With_Expected_Offsets()
    {
        var foundPointers = await _pointerScanner.ScanForPointersAsync(_scanOptions);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == _expectedOffsets).ToList();

        Assert.Single(expectedPointer);

        await RescanPointerTest(foundPointers);
    }

    private async Task RescanPointerTest(IEnumerable<Pointer> firstScanPointers)
    {
        var searchedAddressAfterRescan = new IntPtr(0x863AAE8);
        var stubVirtualMemoryRegions =
            JsonSerializer.Deserialize<IList<VirtualMemoryRegion>>(
                await File.ReadAllTextAsync("ScannerTests/PointerScannerData/Rescan_VirtualMemoryRegions.json"),
                _jsonSerializerOptions)!;

        _stubNativeApi
            .Setup(x => x.GatherVirtualMemoryRegions(_processHandle))
            .Returns(stubVirtualMemoryRegions);
        _stubNativeApi
            .Setup(x => x.ReadVirtualMemory(_processHandle, It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<byte[]>()))
            .Callback((IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer) =>
            {
                ReadVirtualMemoryImpl(hProcess, address, numberOfBytesToRead, buffer, stubVirtualMemoryRegions);
            });

        var foundPointersAfterRescan = await _pointerScanner.RescanPointersAsync(firstScanPointers, _processId, _processHandle, searchedAddressAfterRescan);
        var expectedPointerAfterRescan = foundPointersAfterRescan.Where(x => x.OffsetsDisplayString == _expectedOffsets).ToList();

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
