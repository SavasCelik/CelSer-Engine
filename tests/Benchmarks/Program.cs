using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

BenchmarkRunner.Run<PointerScanBm>();// (new DebugInProcessConfig());