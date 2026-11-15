using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingSuccessVoidResultBenchmarks
{
    [Benchmark]
    public ResultRecordStruct.VoidResult RecordStruct_CreateSuccess() => new();

    [Benchmark]
    public ResultStruct.VoidResult Struct_CreateSuccess() => new();

    [Benchmark]
    public ResultReadonlyStruct.VoidResult ReadonlyStruct_CreateSuccess() => new();

    [Benchmark]
    public ResultClass.VoidResult Class_CreateSuccess() => new();

    [Benchmark]
    public ResultRecordClass.VoidResult RecordClass_CreateSuccess() => new();

    [Benchmark]
    public ResultUnion.VoidResult Union_CreateSuccess() => new();

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.VoidResult UnionCustom_CreateSuccess() => new();
}
