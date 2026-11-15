using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingSuccessVoidResultBenchmarks
{
    [Benchmark]
    public ResultRecordStruct.Result RecordStruct_CreateSuccess() => new();

    [Benchmark]
    public ResultStruct.Result Struct_CreateSuccess() => new();

    [Benchmark]
    public ResultReadonlyStruct.Result ReadonlyStruct_CreateSuccess() => new();

    [Benchmark]
    public ResultClass.Result Class_CreateSuccess() => new();

    [Benchmark]
    public ResultRecordClass.Result RecordClass_CreateSuccess() => new();

    [Benchmark]
    public ResultUnion.Result Union_CreateSuccess() => new();

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.Result UnionCustom_CreateSuccess() => new();
}
