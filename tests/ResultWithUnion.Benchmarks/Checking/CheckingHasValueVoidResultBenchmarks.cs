using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingHasValueVoidResultBenchmarks
{
    private ResultRecordStruct _recordStruct;
    private ResultStruct _struct;
    private ResultReadonlyStruct _readonlyStruct;
    private ResultClass _class = null!;
    private ResultRecordClass _recordClass = null!;
    private ResultUnion _union;
    private ResultUnionCustom _unionCustom;

    [Params(true, false)]
    public bool Success { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Error error = new("fake");

        _recordStruct = Success ? new() : ResultRecordStruct.From(error);
        _struct = Success ? new() : ResultStruct.From(error);
        _readonlyStruct = Success ? new() : ResultReadonlyStruct.From(error);
        _class = Success ? new() : ResultClass.From(error);
        _recordClass = Success ? new() : ResultRecordClass.From(error);
        _union = Success ? new ResultUnion() : ResultUnion.From(error);
        _unionCustom = Success ? new ResultUnionCustom() : ResultUnionCustom.From(error);
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

    [Benchmark(Baseline = true)]
    public bool Union_HasValue()
    {
        if(_union is not null)
        {
            return true;
        }

        return false;
    }

    [Benchmark]
    public bool UnionCustom_HasValue()
    {
        if(_unionCustom.HasValue)
        {
            return true;
        }

        return false;
    }
}
