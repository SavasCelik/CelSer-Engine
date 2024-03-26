using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using Moq;
using System.Text.Json;
using Xunit;
using static CelSerEngine.Core.Native.Structs;

namespace CelSerEngine.Core.IntegrationTests.ScannerTests;
public class NewPointerScannerTests
{
    private PointerScanOptions _scanOptions;
    private string _expectedOffsets;
    private JsonSerializerOptions _jsonSerializerOptions;

    public NewPointerScannerTests()
    {
        _expectedOffsets = "10, 18, 0, 18";
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new IntPtrJsonConverter());
        _scanOptions = new PointerScanOptions()
        {
            ProcessId = "DoesNotMatter".GetHashCode(),
            ProcessHandle = "DoesNotMatter".GetHashCode(),
            MaxLevel = 4,
            MaxOffset = 0x1000,
            SearchedAddress = new IntPtr(0x014FA308)
        };
    }

    [Fact]
    public async Task PointerScanner_Should_Find_Pointer_With_Expected_Offsets()
    {
        var memoryRegions = JsonSerializer.Deserialize<IList<MemoryRegionTestClass>>(
                File.ReadAllText("ScannerTests/PointerScannerData/NewWay/MemoryRegions.json"),
                _jsonSerializerOptions)!;
        var stubNativeApi = GetStubNativeApi(memoryRegions);
        var pointerScanner = new DefaultPointerScanner(stubNativeApi.Object, _scanOptions);
        var foundPointers = await pointerScanner.StartPointerScanAsync(_scanOptions.ProcessHandle);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == _expectedOffsets).ToList();

        Assert.Single(expectedPointer);
    }


    private Mock<INativeApi> GetStubNativeApi(IList<MemoryRegionTestClass> stubMemoryRegions)
    {
        var modules = JsonSerializer.Deserialize<IList<ModuleInfo>>(
                File.ReadAllText("ScannerTests/PointerScannerData/NewWay/Modules.json"),
                _jsonSerializerOptions)!;
        var stackStarts = JsonSerializer.Deserialize<IList<IntPtr>>(
                File.ReadAllText("ScannerTests/PointerScannerData/NewWay/StackStarts.json"),
                _jsonSerializerOptions)!;
        var mbis = stubMemoryRegions.Select(x => new MEMORY_BASIC_INFORMATION64()
        {
            BaseAddress = x.BaseAddress,
            AllocationBase = x.AllocationBase,
            AllocationProtect = x.AllocationProtect,
            Protect = x.Protect,
            RegionSize = x.RegionSize,
            State = x.State,
            Type = x.Type
        });
        var stubNativeApi = new Mock<INativeApi>();
        stubNativeApi
            .Setup(x => x.GetProcessModules(It.IsAny<IntPtr>()))
            .Returns(modules);
        stubNativeApi
            .Setup(x => x.GetStackStart(It.IsAny<IntPtr>(), It.IsAny<int>(), It.IsAny<ModuleInfo>()))
            .Returns((IntPtr hProcess, int threadNr, ModuleInfo? mi) => stackStarts[threadNr]);
        stubNativeApi
            .Setup(x => x.EnumerateMemoryRegions(It.IsAny<IntPtr>()))
            .Returns(mbis);
        stubNativeApi
            .Setup(x => x.TryReadVirtualMemory(It.IsAny<IntPtr>(), It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<byte[]>()))
            .Returns((IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer) =>
            {
                return ReadVirtualMemoryImpl(hProcess, address, numberOfBytesToRead, buffer, stubMemoryRegions);
            });

        return stubNativeApi;
    }

    /// <summary>
    /// Performs a ReadVirtualMemory with the given memory regions
    /// </summary>
    private bool ReadVirtualMemoryImpl(IntPtr hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer, IList<MemoryRegionTestClass> memoryRegions)
    {
        var foundRegions = memoryRegions
            .Where(x => ((IntPtr)x.BaseAddress + (long)x.RegionSize) >= address
            && (address - (IntPtr)x.BaseAddress) < (long)x.RegionSize
            && (IntPtr)x.BaseAddress <= address)
            .ToList();

        if (foundRegions.Count == 0)
            return false;

        var region = foundRegions.Single();
        var offset = address - (IntPtr)region.BaseAddress;
        var dataLength = (int)region.RegionSize - offset.ToInt32();
        if (dataLength >= numberOfBytesToRead)
        {
            Array.Copy(region.Data, offset.ToInt32(), buffer, 0, (int)numberOfBytesToRead);
            return true;
        }

        //Since the desired numberOfBytesToRead is larger than the RegionSize, we get the next contiguous memory region
        Array.Copy(region.Data, offset.ToInt32(), buffer, 0, dataLength);
        var newBuffer = new byte[numberOfBytesToRead - dataLength];
        ReadVirtualMemoryImpl(hProcess, (IntPtr)(region.BaseAddress + (uint)dataLength), (uint)newBuffer.Length, newBuffer, memoryRegions);
        Array.Copy(newBuffer, 0, buffer, dataLength, newBuffer.Length);

        return true;
    }

    /// <summary>
    /// Save current memory regions as .json file
    /// </summary>
    private void SaveMemorySnapShot()
    {
        var nativeAPi = new NativeApi();
        var processHandle = nativeAPi.OpenProcess(19000);
        var currentMemRegs = nativeAPi.EnumerateMemoryRegions(processHandle)
            .Where(x => x.State == Enums.MEMORY_STATE.MEM_COMMIT
                && !x.Protect.HasFlag(Enums.MEMORY_PROTECTION.PAGE_GUARD)
                && !x.Protect.HasFlag(Enums.MEMORY_PROTECTION.PAGE_NOACCESS))
            .Select(x => new MemoryRegionTestClass()
            {
                BaseAddress = x.BaseAddress,
                AllocationBase = x.AllocationBase,
                AllocationProtect = x.AllocationProtect,
                RegionSize = x.RegionSize,
                State = x.State,
                Protect = x.Protect,
                Type = x.Type
            }).ToArray();

        var buffer = new byte[currentMemRegs.Max(x => x.RegionSize)];
        foreach (var memReg in currentMemRegs)
        {
            if (nativeAPi.TryReadVirtualMemory(processHandle, (IntPtr)memReg.BaseAddress, (uint)memReg.RegionSize, out buffer))
                memReg.Data = buffer.ToArray();
        }
        var jsonString = JsonSerializer.Serialize(currentMemRegs);
        File.WriteAllText("ScannerTests/PointerScannerData/NewWay/MemoryRegions.json", jsonString);
    }
}
