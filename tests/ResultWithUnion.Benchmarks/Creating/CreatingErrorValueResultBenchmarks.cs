using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.Common;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorValueResultBenchmarks
{
    private Error _error;

    [GlobalSetup]
    public void Setup() => _error = new("fake");

    [Benchmark]
    public ResultRecordStruct<FakeModel> RecordStruct_CreateError() => ResultRecordStruct<FakeModel>.From(_error);

    [Benchmark]
    public ResultStruct<FakeModel> Struct_CreateError() => ResultStruct<FakeModel>.From(_error);

    [Benchmark]
    public ResultReadonlyStruct<FakeModel> ReadonlyStruct_CreateError() => ResultReadonlyStruct<FakeModel>.From(_error);

    [Benchmark]
    public ResultClass<FakeModel> Class_CreateError() => ResultClass<FakeModel>.From(_error);

    [Benchmark]
    public ResultRecordClass<FakeModel> RecordClass_CreateError() => ResultRecordClass<FakeModel>.From(_error);

    [Benchmark(Baseline = true)]
    public ResultUnion<FakeModel> Union_CreateError() => ResultUnion<FakeModel>.From(_error);

    [Benchmark]
    public ResultUnionCustom<FakeModel> UnionCustom_CreateError() => ResultUnionCustom<FakeModel>.From(_error);
}
