using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorValueResultBenchmarks
{
    private ResultRecordStruct.Error _errorRecordStruct;
    private ResultStruct.Error _errorStruct;
    private ResultReadonlyStruct.Error _errorReadonlyStruct;
    private ResultClass.Error _errorClass;
    private ResultRecordClass.Error _errorRecordClass;
    private ResultUnion.Error _errorUnion;
    private ResultUnionCustom.Error _errorUnionCustom;

    [GlobalSetup]
    public void Setup()
    {
        _errorRecordStruct = new("fake");
        _errorStruct = new("fake");
        _errorReadonlyStruct = new("fake");
        _errorClass = new("fake");
        _errorRecordClass = new("fake");
        _errorUnion = new("fake");
        _errorUnionCustom = new("fake");
    }

    [Benchmark]
    public ResultRecordStruct.ValueResult<FakeModel> RecordStruct_CreateError() => ResultRecordStruct.ValueResult<FakeModel>.From(_errorRecordStruct);

    [Benchmark]
    public ResultStruct.ValueResult<FakeModel> Struct_CreateError() => ResultStruct.ValueResult<FakeModel>.From(_errorStruct);

    [Benchmark]
    public ResultReadonlyStruct.ValueResult<FakeModel> ReadonlyStruct_CreateError() => ResultReadonlyStruct.ValueResult<FakeModel>.From(_errorReadonlyStruct);

    [Benchmark]
    public ResultClass.ValueResult<FakeModel> Class_CreateError() => ResultClass.ValueResult<FakeModel>.From(_errorClass);

    [Benchmark]
    public ResultRecordClass.ValueResult<FakeModel> RecordClass_CreateError() => ResultRecordClass.ValueResult<FakeModel>.From(_errorRecordClass);

    [Benchmark]
    public ResultUnion.ValueResult<FakeModel> Union_CreateError() => ResultUnion.ValueResult<FakeModel>.From(_errorUnion);

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.ValueResult<FakeModel> UnionCustom_CreateError() => ResultUnionCustom.ValueResult<FakeModel>.From(_errorUnionCustom);
}
