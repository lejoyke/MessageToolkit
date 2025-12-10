using BenchmarkDotNet.Running;
using MessageToolkit.Benchmarks;

Console.WriteLine("MessageToolkit 性能测试");
Console.WriteLine("======================================");
Console.WriteLine();
Console.WriteLine("正在运行 Benchmark 测试，请稍候...");
Console.WriteLine();

var summary = BenchmarkRunner.Run<ProtocolCodecBenchmarks>();

Console.WriteLine();
Console.WriteLine("测试完成！");
