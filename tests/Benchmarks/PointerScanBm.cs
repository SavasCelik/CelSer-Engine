using BenchmarkDotNet.Attributes;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scanners;
using Microsoft.VSDiagnostics;
using Microsoft.Win32.SafeHandles;

namespace Benchmarks;

//[CPUUsageDiagnoser]
[MemoryDiagnoser]
public  class PointerScanBm
{
    private PointerScanOptions _pointerScanOptions = null!;
    private INativeApi _nativeApi = null!;
    private SafeProcessHandle _processHandle = null!;
    private const string FileName = "C:\\Users\\CelSer\\Documents\\CelSerEngine PointerLists\\testBm.ptrlist";

    [GlobalSetup]
    public async Task SetupData()
    {
        _nativeApi = new NativeApi();
        _processHandle = _nativeApi.OpenProcess("SmallGame");
        _pointerScanOptions = new PointerScanOptions
        {
            SearchedAddress = (IntPtr)0x2381AFF7AF8,
            MaxOffset = 4096,
            MaxLevel = 10,
            MaxParallelWorkers = 6
        };
    }

    [Benchmark]
    public async Task PointerScan()
    {
        var pointerScanner = new DefaultPointerScanner(_nativeApi, _pointerScanOptions);
        await pointerScanner.StartPointerScanAsync(_processHandle).ConfigureAwait(false);
    }

    //[Benchmark]
    //public async Task PointerScan3()
    //{
    //    var pointerScanner = new Ds3(_nativeApi, _pointerScanOptions);
    //    await pointerScanner.StartPointerScanAsync(_processHandle).ConfigureAwait(false);
    //}

    [Benchmark]
    public async Task PointerScanNative()
    {
        var psNative = new PsNative(_nativeApi);
        psNative.Start(_processHandle, _pointerScanOptions.SearchedAddress, _pointerScanOptions.MaxLevel, _pointerScanOptions.MaxOffset);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _processHandle.Dispose();
    }
}
