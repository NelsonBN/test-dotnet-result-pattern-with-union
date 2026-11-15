using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ImplicitFromErrorVoidResultBenchmarks
{
    private Error _error;

    [GlobalSetup]
    public void Setup()
        => _error = new("fake1");

    [Benchmark]
    public ResultRecordStruct RecordStruct_CreateErrors() => _error;

    [Benchmark]
    public ResultStruct Struct_CreateErrors() => _error;

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateErrors() => _error;

    [Benchmark]
    public ResultClass Class_CreateErrors() => _error;

    [Benchmark]
    public ResultRecordClass RecordClass_CreateErrors() => _error;

    [Benchmark(Baseline = true)]
    public ResultUnion Union_CreateErrors() => _error;

    [Benchmark]
    public ResultUnionCustom UnionCustom_CreateErrors() => _error;
}
