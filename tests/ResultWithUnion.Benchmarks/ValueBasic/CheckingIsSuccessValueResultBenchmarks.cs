using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.Common;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingIsSuccessValueResultBenchmarks
{
    private ResultRecordStruct<FakeModel> _recordStruct;
    private ResultStruct<FakeModel> _struct;
    private ResultReadonlyStruct<FakeModel> _readonlyStruct;
    private ResultClass<FakeModel> _class = null!;
    private ResultRecordClass<FakeModel> _recordClass = null!;
    private ResultUnion<FakeModel> _union;
    private ResultUnionNonBoxing<FakeModel> _unionNonBoxing;

    [Params(true, false)]
    public bool Success { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        FakeModel model = new();
        Error error = new("fake");

        _recordStruct = Success ? new(model) : ResultRecordStruct<FakeModel>.From(error);
        _struct = Success ? new(model) : ResultStruct<FakeModel>.From(error);
        _readonlyStruct = Success ? new(model) : ResultReadonlyStruct<FakeModel>.From(error);
        _class = Success ? new(model) : ResultClass<FakeModel>.From(error);
        _recordClass = Success ? new(model) : ResultRecordClass<FakeModel>.From(error);
        _union = Success ? model : ResultUnion<FakeModel>.From(error);
        _unionNonBoxing = Success ? model : ResultUnionNonBoxing<FakeModel>.From(error);
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

    [Benchmark(Baseline = true)]
    public bool Union_IsSuccess()
    {
        if(_union.IsSuccess)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool UnionNonBoxing_IsSuccess()
    {
        if(_unionNonBoxing.IsSuccess)
        {
            return true;
        }

        return false;
    }
}
