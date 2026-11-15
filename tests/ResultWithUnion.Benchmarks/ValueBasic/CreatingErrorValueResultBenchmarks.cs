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

    [Benchmark(Baseline = true)]
    public ResultRecordStruct<FakeModel> RecordStruct_CreateError() => new(_error);

    [Benchmark]
    public ResultStruct<FakeModel> Struct_CreateError() => new(_error);

    [Benchmark]
    public ResultReadonlyStruct<FakeModel> ReadonlyStruct_CreateError() => new(_error);

    [Benchmark]
    public ResultClass<FakeModel> Class_CreateError() => new(_error);

    [Benchmark]
    public ResultRecordClass<FakeModel> RecordClass_CreateError() => new(_error);

    [Benchmark]
    public ResultUnion<FakeModel> Union_CreateError() => _error;
}
