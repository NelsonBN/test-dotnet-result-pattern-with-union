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
    public ResultRecordStruct.Result<FakeModel> RecordStruct_CreateError() => ResultRecordStruct.Result<FakeModel>.From(_errorRecordStruct);

    [Benchmark]
    public ResultStruct.Result<FakeModel> Struct_CreateError() => ResultStruct.Result<FakeModel>.From(_errorStruct);

    [Benchmark]
    public ResultReadonlyStruct.Result<FakeModel> ReadonlyStruct_CreateError() => ResultReadonlyStruct.Result<FakeModel>.From(_errorReadonlyStruct);

    [Benchmark]
    public ResultClass.Result<FakeModel> Class_CreateError() => ResultClass.Result<FakeModel>.From(_errorClass);

    [Benchmark]
    public ResultRecordClass.Result<FakeModel> RecordClass_CreateError() => ResultRecordClass.Result<FakeModel>.From(_errorRecordClass);

    [Benchmark]
    public ResultUnion.Result<FakeModel> Union_CreateError() => ResultUnion.Result<FakeModel>.From(_errorUnion);

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.Result<FakeModel> UnionCustom_CreateError() => ResultUnionCustom.Result<FakeModel>.From(_errorUnionCustom);
}
