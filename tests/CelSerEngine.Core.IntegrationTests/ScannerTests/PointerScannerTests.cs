using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using Microsoft.Win32.SafeHandles;
using Moq;
using System.Text.Json;
using Xunit;

namespace CelSerEngine.Core.IntegrationTests.ScannerTests;

public class PointerScannerTests
{
    private PointerScanOptions _scanOptions;
    private string _expectedOffsets;
    private JsonSerializerOptions _jsonSerializerOptions;
    private SafeProcessHandle _processHandle;
    private int _processId;

    public PointerScannerTests()
    {
        _processId = 123;
        _processHandle = new SafeProcessHandle();
        _expectedOffsets = "10, 18, 0, 18";
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new IntPtrJsonConverter());
        _scanOptions = new PointerScanOptions()
        {
            ProcessId = _processId,
            ProcessHandle = _processHandle,
            MaxLevel = 4,
            MaxOffset = 0x2000,
            SearchedAddress = new IntPtr(0x1526B78)
        };
    }

    [Fact]
    public async Task PointerScanner_Should_Find_Pointer_With_Expected_Offsets()
    {
        var stubVirtualMemoryRegions =
            JsonSerializer.Deserialize<IList<VirtualMemoryRegion>>(
                File.ReadAllText("ScannerTests/PointerScannerData/VirtualMemoryRegions.json"),
                _jsonSerializerOptions)!;
        var stubNativeApi = GetStubNativeApi(stubVirtualMemoryRegions);
        var pointerScanner = new PointerScanner(stubNativeApi.Object);

        var foundPointers = await pointerScanner.ScanForPointersAsync(_scanOptions);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == _expectedOffsets).ToList();

        Assert.Single(expectedPointer);

        await RescanPointerTest(foundPointers);
    }

    [Fact]
    public async Task PointerScannerParallel_Should_Find_Pointer_With_Expected_Offsets()
    {
        var stubVirtualMemoryRegions =
            JsonSerializer.Deserialize<IList<VirtualMemoryRegion>>(
                File.ReadAllText("ScannerTests/PointerScannerData/VirtualMemoryRegions.json"),
                _jsonSerializerOptions)!;
        var stubNativeApi = GetStubNativeApi(stubVirtualMemoryRegions);
        var pointerScanner = new PointerScanner(stubNativeApi.Object);

        var foundPointers = await pointerScanner.ScanForPointersParallelAsync(_scanOptions);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == _expectedOffsets).ToList();
        var expectedPointer2 = foundPointers.Where(x => x.Offsets.Count == 4 && x.Offsets.First() == 0x18).ToList();

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
        var stubNativeApi = GetStubNativeApi(stubVirtualMemoryRegions);
        var pointerScanner = new PointerScanner(stubNativeApi.Object);

        var foundPointersAfterRescan = await pointerScanner.RescanPointersAsync(firstScanPointers, _processId, _processHandle, searchedAddressAfterRescan);
        var expectedPointerAfterRescan = foundPointersAfterRescan.Where(x => x.OffsetsDisplayString == _expectedOffsets).ToList();

        Assert.Single(expectedPointerAfterRescan);
    }

    private Mock<INativeApi> GetStubNativeApi(IList<VirtualMemoryRegion> stubVirtualMemoryRegions)
    {
        var moduleBaseAddress = new IntPtr(0x100000000);
        var moduleSize = (uint)0x344000;
        var stubNativeApi = new Mock<INativeApi>();
        stubNativeApi
            .Setup(x => x.GetProcessMainModule(_processId))
            .Returns(new ProcessModuleInfo("TestModule", moduleBaseAddress, moduleSize));
        stubNativeApi
            .Setup(x => x.GatherVirtualMemoryRegions(_processHandle))
            .Returns(() => stubVirtualMemoryRegions.ToList());
        stubNativeApi
            .Setup(x => x.TryReadVirtualMemory(_processHandle, It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<byte[]>()))
            .Returns((SafeProcessHandle hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer) =>
            {
                return ReadVirtualMemoryImpl(hProcess, address, numberOfBytesToRead, buffer, stubVirtualMemoryRegions);
            });

        return stubNativeApi;
    }

    private bool ReadVirtualMemoryImpl(SafeProcessHandle hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer, IList<VirtualMemoryRegion> virtualMemoryRegions)
    {
        var foundRegions = virtualMemoryRegions
            .Where(x => (x.BaseAddress + (long)x.RegionSize) >= address
            && (address - x.BaseAddress) < (long)x.RegionSize
            && x.BaseAddress < address)
            .ToList();

        if (foundRegions.Count == 0)
            return false;

        var region = foundRegions.Single();
        var offset = address - region.BaseAddress;
        Array.Copy(region.Bytes, offset.ToInt32(), buffer, 0, (int)numberOfBytesToRead);
        return true;
    }
}
