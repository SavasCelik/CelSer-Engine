//using BenchmarkDotNet.Attributes;
//using CelSerEngine.Core.Scanners.Serialization;

//namespace Benchmarks;

//[MemoryDiagnoser]
//public class PointerWriterBenchmark
//{
//    private Random _random = default!;
//    private const int ItemsCount = 30_000_000;
//    private List<(int level, int moduleIndex, long moduleOffset, int firstOffset, int secondOffset)> _data = [];
//    private const int MaxModuleIndex = 12;
//    private const uint MaxModuleOffset = 0x60000U;
//    private const int MaxLevel = 4;
//    private const int MaxOffset = 0x1000;

//    [GlobalSetup]
//    public async Task SetupData()
//    {
//        _random = new Random();
//        _data = new List<(int level, int moduleIndex, long moduleOffset, int firstOffset, int secondOffset)>(ItemsCount);

//        for (var i = 0; i < ItemsCount; i++)
//        {
//            _data.Add(
//                (_random.Next(0, MaxLevel), 
//                _random.Next(0, MaxModuleIndex), 
//                _random.NextInt64(-MaxModuleOffset, MaxModuleOffset), 
//                _random.Next(0, MaxOffset), 
//                _random.Next(0, MaxOffset))
//            );
//        }
//    }

//    [Benchmark]
//    public void PointerBitWriter()
//    {
//        var pLayout = new PointerBitLayout(MaxModuleIndex, MaxModuleOffset, MaxLevel, MaxOffset);
//        using var pWriter = new PointerBitWriter(Stream.Null, pLayout);

//        foreach (var (level, moduleIndex, moduleOffset, firstOffset, secondOffset) in _data)
//        {
//            pWriter.Write(level, moduleIndex, moduleOffset, [firstOffset, secondOffset]);
//        }
//    }

//    [Benchmark]
//    public void Pointer7BitWriter()
//    {
//        var pLayout = new Pointer7BitLayout(MaxModuleIndex, MaxModuleOffset, MaxLevel, MaxOffset);
//        using var pWriter = new Pointer7BitWriter(Stream.Null, pLayout);
        
//        foreach (var (level, moduleIndex, moduleOffset, firstOffset, secondOffset) in _data)
//        {
//            pWriter.Write(level, moduleIndex, moduleOffset, [firstOffset, secondOffset]);
//        }
//    }
//}
