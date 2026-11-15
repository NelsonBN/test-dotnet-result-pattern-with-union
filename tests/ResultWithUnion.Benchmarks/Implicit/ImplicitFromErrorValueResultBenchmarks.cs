using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.Common;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ImplicitFromErrorValueResultBenchmarks
{
    private Error _error;

    [GlobalSetup]
    public void Setup()
        => _error = new("fake1");

    [Benchmark]
    public ResultRecordStruct<FakeModel> RecordStruct_CreateErrors() => _error;

    [Benchmark]
    public ResultStruct<FakeModel> Struct_CreateErrors() => _error;

    [Benchmark]
    public ResultReadonlyStruct<FakeModel> ReadonlyStruct_CreateErrors() => _error;

    [Benchmark]
    public ResultClass<FakeModel> Class_CreateErrors() => _error;

    [Benchmark]
    public ResultRecordClass<FakeModel> RecordClass_CreateErrors() => _error;

    [Benchmark(Baseline = true)]
    public ResultUnion<FakeModel> Union_CreateErrors() => _error;

    [Benchmark]
    public ResultUnionCustom<FakeModel> UnionCustom_CreateErrors() => _error;
}
