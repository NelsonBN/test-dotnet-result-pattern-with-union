using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingIsSuccessValueResultBenchmarks
{
    private ResultRecordStruct.Result<FakeModel> _recordStruct;
    private ResultStruct.Result<FakeModel> _struct;
    private ResultReadonlyStruct.Result<FakeModel> _readonlyStruct;
    private ResultClass.Result<FakeModel> _class = null!;
    private ResultRecordClass.Result<FakeModel> _recordClass = null!;
    private ResultUnion.Result<FakeModel> _union;
    private ResultUnionCustom.Result<FakeModel> _unionCustom;
    private ResultUnionCustomExtreme.Result<FakeModel> _unionCustomExtreme;

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
        ResultUnionCustomExtreme.Error errorUnionCustomExtreme = new("fake");

        _recordStruct = Success ? new(model) : ResultRecordStruct.Result<FakeModel>.From(errorRecordStruct);
        _struct = Success ? new(model) : ResultStruct.Result<FakeModel>.From(errorStruct);
        _readonlyStruct = Success ? new(model) : ResultReadonlyStruct.Result<FakeModel>.From(errorReadonlyStruct);
        _class = Success ? new(model) : ResultClass.Result<FakeModel>.From(errorClass);
        _recordClass = Success ? new(model) : ResultRecordClass.Result<FakeModel>.From(errorRecordClass);
        _union = Success ? model : ResultUnion.Result<FakeModel>.From(errorUnion);
        _unionCustom = Success ? model : ResultUnionCustom.Result<FakeModel>.From(errorUnionCustom);
        _unionCustomExtreme = Success ? model : ResultUnionCustomExtreme.Result<FakeModel>.From(errorUnionCustomExtreme);
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

    [Benchmark]
    public bool UnionCustomExtreme_IsSuccess()
    {
        if(_unionCustomExtreme.IsSuccess)
        {
            return true;
        }

        return false;
    }
}
