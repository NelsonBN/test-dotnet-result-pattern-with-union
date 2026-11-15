using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingValueResultBenchmarks
{
    private FakeModel _model = null!;

    [GlobalSetup]
    public void Setup() => _model = new();

    [Benchmark]
    public ResultRecordStruct<FakeModel> RecordStruct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultStruct<FakeModel> Struct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultReadonlyStruct<FakeModel> ReadonlyStruct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultClass<FakeModel> Class_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultRecordClass<FakeModel> RecordClass_CreateSuccess() => new(_model);

    [Benchmark(Baseline = true)]
    public ResultUnion<FakeModel> Union_CreateSuccess() => _model;

    [Benchmark]
    public ResultUnionNonBoxing<FakeModel> UnionNonBoxing_CreateSuccess() => _model;
}
