using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

BenchmarkRunner.Run<PointerScannerBenchmark>();// (new DebugInProcessConfig());