//using BenchmarkDotNet.Attributes;
//using CelSerEngine.Core.IntegrationTests.ScannerTests;
//using CelSerEngine.Core.Models;
//using CelSerEngine.Core.Native;
//using CelSerEngine.Core.Scanners;
//using Moq;
//using System.Text.Json;
//using Microsoft.Win32.SafeHandles;

//namespace Benchmarks;

//[MemoryDiagnoser]
//public class PointerScannerBenchmark
//{
//    private PointerScanner _pointerScanner;
//    private PointerScanOptions _scanOptions;
//    private const string ExpectedOffsets = "10, 18, 0, 18";

//    [GlobalSetup]
//    public async Task SetupData()
//    {
//        var processId = 123;
//        var processHandle = new SafeProcessHandle();
//        var searchedAddress = new IntPtr(0x1526B78);
//        var moduleBaseAddress = new IntPtr(0x100000000);
//        var moduleSize = (uint)0x344000;

//        // first scan
//        var jsonOprions = new JsonSerializerOptions();
//        jsonOprions.Converters.Add(new IntPtrJsonConverter());
//        var stubVirtualMemoryRegions =
//            JsonSerializer.Deserialize<IList<VirtualMemoryRegion>>(
//                await File.ReadAllTextAsync("ScannerTests/PointerScannerData/VirtualMemoryRegions.json"),
//                jsonOprions)!;
//        var stubNativeApi = new Mock<INativeApi>();
//        stubNativeApi
//            .Setup(x => x.GetProcessMainModule(processId))
//            .Returns(() => new ProcessModuleInfo("TestModule", moduleBaseAddress, moduleSize));
//        stubNativeApi
//            .Setup(x => x.GatherVirtualMemoryRegions(processHandle))
//            .Returns(() => stubVirtualMemoryRegions.ToList());
//        stubNativeApi
//            .Setup(x => x.TryReadVirtualMemory(processHandle, It.IsAny<IntPtr>(), It.IsAny<uint>(), It.IsAny<byte[]>()))
//            .Returns((SafeProcessHandle hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer) =>
//            {
//                return ReadVirtualMemoryImpl(hProcess, address, numberOfBytesToRead, buffer, stubVirtualMemoryRegions);
//            });

//        _pointerScanner = new PointerScanner(stubNativeApi.Object);
//        _scanOptions = new PointerScanOptions()
//        {
//            ProcessId = processId,
//            ProcessHandle = processHandle,
//            MaxLevel = 4,
//            MaxOffset = 0x2000,
//            SearchedAddress = searchedAddress
//        };
//    }

//    //TODO: sometime the foundPointers finds more elements, this makes no sense since we use a snapshot of the memory
//    [Benchmark(Baseline = true)]
//    public async Task FindPointer()
//    {
//        var foundPointers = await _pointerScanner.ScanForPointersAsync(_scanOptions);
//        //var foundPointers2 = await _pointerScanner.ScanForPointersAsync(_scanOptions);

//        //while (foundPointers.Count == foundPointers2.Count)
//        //{
//        //    foundPointers2 = await _pointerScanner.ScanForPointersAsync(_scanOptions);
//        //}

//        //var filtered = foundPointers2.Where(x => !foundPointers.Any(y => x.OffsetsDisplayString == y.OffsetsDisplayString && x.Address == y.Address)).ToList();
//        //var filtered2 = foundPointers.Where(x => !foundPointers2.Any(y => x.OffsetsDisplayString == y.OffsetsDisplayString && x.Address == y.Address)).ToList();

//        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == ExpectedOffsets).ToList();
//    }

//    [Benchmark]
//    public async Task FindPointerParallel()
//    {
//        var foundPointers = await _pointerScanner.ScanForPointersParallelAsync(_scanOptions);
//        var expectedPointer = foundPointers.Where(x => x.OffsetsDisplayString == ExpectedOffsets).ToList();
//    }
//    private bool ReadVirtualMemoryImpl(SafeProcessHandle hProcess, IntPtr address, uint numberOfBytesToRead, byte[] buffer, IList<VirtualMemoryRegion> virtualMemoryRegions)
//    {
//        var foundRegions = virtualMemoryRegions
//            .Where(x => (x.BaseAddress + (long)x.RegionSize) >= address
//            && (address - x.BaseAddress) < (long)x.RegionSize
//            && x.BaseAddress < address)
//            .ToList();

//        if (foundRegions.Count == 0)
//            return false;

//        var region = foundRegions.Single();
//        var offset = address - region.BaseAddress;
//        Array.Copy(region.Bytes, offset.ToInt32(), buffer, 0, (int)numberOfBytesToRead);

//        return true;
//    }
//}
