using BenchmarkDotNet.Attributes;
using CelSerEngine.Core.IntegrationTests.ScannerTests;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using Microsoft.Win32.SafeHandles;
using Moq;
using System.Text.Json;
using static CelSerEngine.Core.Native.Structs;

namespace Benchmarks;

[MemoryDiagnoser]
public class NewPointerScannerBenchmark
{
    private Mock<INativeApi> _stubNativeApi;
    private const string ExpectedOffsets = "10, 18, 0, 18";

    [GlobalSetup]
    public async Task SetupData()
    {
        var jsonOprions = new JsonSerializerOptions();
        jsonOprions.Converters.Add(new IntPtrJsonConverter());

        var stubMemoryRegions = JsonSerializer.Deserialize<IList<MemoryRegionTestClass>>(
                File.ReadAllText("ScannerTests/PointerScannerData/NewWay/MemoryRegions.json"),
                jsonOprions)!;
        var modules = JsonSerializer.Deserialize<IList<ModuleInfo>>(
                File.ReadAllText("ScannerTests/PointerScannerData/NewWay/Modules.json"),
                jsonOprions)!;
        var stackStarts = JsonSerializer.Deserialize<IList<IntPtr>>(
                File.ReadAllText("ScannerTests/PointerScannerData/NewWay/StackStarts.json"),
                jsonOprions)!;
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
        _stubNativeApi = new Mock<INativeApi>();
        _stubNativeApi
            .Setup(x => x.GetProcessModules(It.IsAny<SafeProcessHandle>()))
            .Returns(() => modules.ToList());
        _stubNativeApi
            .Setup(x => x.GetStackStart(It.IsAny<SafeProcessHandle>(), It.IsAny<int>(), It.IsAny<ModuleInfo>()))
            .Returns((SafeProcessHandle hProcess, int threadNr, ModuleInfo? mi) => stackStarts[threadNr]);
        _stubNativeApi
            .Setup(x => x.EnumerateMemoryRegions(It.IsAny<SafeProcessHandle>(), null, null))
            .Returns(() => mbis.ToList());
        _stubNativeApi
            .Setup(x => x.TryReadVirtualMemory(It.IsAny<SafeProcessHandle>(), It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<byte[]>()))
            .Returns((SafeProcessHandle hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer) =>
            {
                return ReadVirtualMemoryImpl(hProcess, address, numberOfBytesToRead, buffer, stubMemoryRegions);
            });
    }

    [Benchmark]
    public async Task CheatEnginePointerScannerLevel4()
    {
        var scanOpts = new PointerScanOptions()
        {
            ProcessId = "DoesNotMatter".GetHashCode(),
            ProcessHandle = new SafeProcessHandle(),
            MaxLevel = 4,
            MaxOffset = 0x1000,
            SearchedAddress = new IntPtr(0x014FA308)
        };
        var scanner = new CheatEnginePointerScanner(_stubNativeApi.Object, scanOpts);
        var foundPointers = await scanner.StartPointerScanAsync(scanOpts.ProcessHandle);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == ExpectedOffsets).ToList();
    }

    [Benchmark]
    public async Task DefaultPointerScannerLevel4()
    {
        var scanOpts = new PointerScanOptions()
        {
            ProcessId = "DoesNotMatter".GetHashCode(),
            ProcessHandle = new SafeProcessHandle(),
            MaxLevel = 4,
            MaxOffset = 0x1000,
            SearchedAddress = new IntPtr(0x014FA308)
        };
        var scanner = new DefaultPointerScanner(_stubNativeApi.Object, scanOpts);
        var foundPointers = await scanner.StartPointerScanAsync(scanOpts.ProcessHandle);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == ExpectedOffsets).ToList();
    }

    [Benchmark]
    public async Task CheatEnginePointerScannerLevel6()
    {
        var scanOpts = new PointerScanOptions()
        {
            ProcessId = "DoesNotMatter".GetHashCode(),
            ProcessHandle = new SafeProcessHandle(),
            MaxLevel = 6,
            MaxOffset = 0x1000,
            SearchedAddress = new IntPtr(0x014FA308)
        };
        var scanner = new CheatEnginePointerScanner(_stubNativeApi.Object, scanOpts);
        var foundPointers = await scanner.StartPointerScanAsync(scanOpts.ProcessHandle);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == ExpectedOffsets).ToList();
    }

    [Benchmark]
    public async Task DefaultPointerScannerLevel6()
    {
        var scanOpts = new PointerScanOptions()
        {
            ProcessId = "DoesNotMatter".GetHashCode(),
            ProcessHandle = new SafeProcessHandle(),
            MaxLevel = 6,
            MaxOffset = 0x1000,
            SearchedAddress = new IntPtr(0x014FA308)
        };
        var scanner = new DefaultPointerScanner(_stubNativeApi.Object, scanOpts);
        var foundPointers = await scanner.StartPointerScanAsync(scanOpts.ProcessHandle);
        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == ExpectedOffsets).ToList();
    }

    private bool ReadVirtualMemoryImpl(SafeProcessHandle hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer, IList<MemoryRegionTestClass> memoryRegions)
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
}
