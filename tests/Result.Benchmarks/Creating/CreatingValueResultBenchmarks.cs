using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingValueResultBenchmarks
{
    private FakeModel _model = null!;

    [GlobalSetup]
    public void Setup() => _model = new();

    [Benchmark]
    public ResultRecordStruct.ValueResult<FakeModel> RecordStruct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultStruct.ValueResult<FakeModel> Struct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultReadonlyStruct.ValueResult<FakeModel> ReadonlyStruct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultClass.ValueResult<FakeModel> Class_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultRecordClass.ValueResult<FakeModel> RecordClass_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultUnion.ValueResult<FakeModel> Union_CreateSuccess() => _model;

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.ValueResult<FakeModel> UnionCustom_CreateSuccess() => _model;
}
