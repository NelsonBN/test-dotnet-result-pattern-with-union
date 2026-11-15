using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.Common;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingHasValueValueResultBenchmarks
{
    private ResultRecordStruct<FakeModel> _recordStruct;
    private ResultStruct<FakeModel> _struct;
    private ResultReadonlyStruct<FakeModel> _readonlyStruct;
    private ResultClass<FakeModel> _class = null!;
    private ResultRecordClass<FakeModel> _recordClass = null!;
    private ResultUnion<FakeModel> _union;
    private ResultUnionCustom<FakeModel> _unionCustom;

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
        _unionCustom = Success ? model : ResultUnionCustom<FakeModel>.From(error);
    }

    [Benchmark]
    public bool RecordStruct_HasValue()
    {
        if(_recordStruct.HasValue)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool Struct_HasValue()
    {
        if(_struct.HasValue)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool ReadonlyStruct_HasValue()
    {
        if(_readonlyStruct.HasValue)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool Class_HasValue()
    {
        if(_class.HasValue)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool RecordClass_HasValue()
    {
        if(_recordClass.HasValue)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool Union_HasValue()
    {
        if(_union is not null)
        {
            return true;
        }

        return false;
    }

    [Benchmark(Baseline = true)]
    public bool UnionCustom_HasValue()
    {
        if(_unionCustom.HasValue)
        {
            return true;
        }

        return false;
    }
}
