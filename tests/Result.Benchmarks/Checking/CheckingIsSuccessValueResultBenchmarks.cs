using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingIsSuccessValueResultBenchmarks
{
    private ResultRecordStruct.ValueResult<FakeModel> _recordStruct;
    private ResultStruct.ValueResult<FakeModel> _struct;
    private ResultReadonlyStruct.ValueResult<FakeModel> _readonlyStruct;
    private ResultClass.ValueResult<FakeModel> _class = null!;
    private ResultRecordClass.ValueResult<FakeModel> _recordClass = null!;
    private ResultUnion.ValueResult<FakeModel> _union;
    private ResultUnionCustom.ValueResult<FakeModel> _unionCustom;

    [Params(true, false)]
    public bool Success { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        FakeModel model = new();
        ResultRecordStruct.Error errorRecordStruct = new("fake");
        ResultStruct.Error errorStruct = new("fake");
        ResultReadonlyStruct.Error errorReadonlyStruct = new("fake");
        ResultClass.Error errorClass = new("fake");
        ResultRecordClass.Error errorRecordClass = new("fake");
        ResultUnion.Error errorUnion = new("fake");
        ResultUnionCustom.Error errorUnionCustom = new("fake");

        _recordStruct = Success ? new(model) : ResultRecordStruct.ValueResult<FakeModel>.From(errorRecordStruct);
        _struct = Success ? new(model) : ResultStruct.ValueResult<FakeModel>.From(errorStruct);
        _readonlyStruct = Success ? new(model) : ResultReadonlyStruct.ValueResult<FakeModel>.From(errorReadonlyStruct);
        _class = Success ? new(model) : ResultClass.ValueResult<FakeModel>.From(errorClass);
        _recordClass = Success ? new(model) : ResultRecordClass.ValueResult<FakeModel>.From(errorRecordClass);
        _union = Success ? model : ResultUnion.ValueResult<FakeModel>.From(errorUnion);
        _unionCustom = Success ? model : ResultUnionCustom.ValueResult<FakeModel>.From(errorUnionCustom);
    }

    [Benchmark]
    public bool RecordStruct_IsSuccess()
    {
        if(_recordStruct.IsSuccess)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool Struct_IsSuccess()
    {
        if(_struct.IsSuccess)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool ReadonlyStruct_IsSuccess()
    {
        if(_readonlyStruct.IsSuccess)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool Class_IsSuccess()
    {
        if(_class.IsSuccess)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool RecordClass_IsSuccess()
    {
        if(_recordClass.IsSuccess)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool Union_IsSuccess()
    {
        if(_union.IsSuccess)
        {
            return true;
        }

        return false;
    }

    [Benchmark(Baseline = true)]
    public bool UnionCustom_IsSuccess()
    {
        if(_unionCustom.IsSuccess)
        {
            return true;
        }

        return false;
    }
}
