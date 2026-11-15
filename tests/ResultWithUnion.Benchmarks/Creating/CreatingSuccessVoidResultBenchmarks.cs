using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingSuccessVoidResultBenchmarks
{
    [Benchmark]
    public ResultRecordStruct RecordStruct_CreateSuccess() => new();

    [Benchmark]
    public ResultStruct Struct_CreateSuccess() => new();

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateSuccess() => new();

    [Benchmark]
    public ResultClass Class_CreateSuccess() => new();

    [Benchmark]
    public ResultRecordClass RecordClass_CreateSuccess() => new();

    [Benchmark]
    public ResultUnion Union_CreateSuccess() => new();

    [Benchmark(Baseline = true)]
    public ResultUnionCustom UnionCustom_CreateSuccess() => new();
}
