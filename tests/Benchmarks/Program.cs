using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

BenchmarkRunner.Run<PointerWriterBenchmark>();// (new DebugInProcessConfig());