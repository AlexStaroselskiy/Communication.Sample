using BenchmarkDotNet.Running;
using Communication.Sample.Benchmark;

var summary = BenchmarkRunner.Run<ChannelBenchmark>();
