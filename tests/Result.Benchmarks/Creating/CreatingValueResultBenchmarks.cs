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
    public ResultRecordStruct.Result<FakeModel> RecordStruct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultStruct.Result<FakeModel> Struct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultReadonlyStruct.Result<FakeModel> ReadonlyStruct_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultClass.Result<FakeModel> Class_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultRecordClass.Result<FakeModel> RecordClass_CreateSuccess() => new(_model);

    [Benchmark]
    public ResultUnion.Result<FakeModel> Union_CreateSuccess() => _model;

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.Result<FakeModel> UnionCustom_CreateSuccess() => _model;
}
