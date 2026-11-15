using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingHasValueVoidResultBenchmarks
{
    private ResultRecordStruct.Result _recordStruct;
    private ResultStruct.Result _struct;
    private ResultReadonlyStruct.Result _readonlyStruct;
    private ResultClass.Result _class = null!;
    private ResultRecordClass.Result _recordClass = null!;
    private ResultUnion.Result _union;
    private ResultUnionCustom.Result _unionCustom;

    [Params(true, false)]
    public bool Success { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        ResultRecordStruct.Error errorRecordStruct = new("fake");
        ResultStruct.Error errorStruct = new("fake");
        ResultReadonlyStruct.Error errorReadonlyStruct = new("fake");
        ResultClass.Error errorClass = new("fake");
        ResultRecordClass.Error errorRecordClass = new("fake");
        ResultUnion.Error errorUnion = new("fake");
        ResultUnionCustom.Error errorUnionCustom = new("fake");

        _recordStruct = Success ? new() : ResultRecordStruct.Result.From(errorRecordStruct);
        _struct = Success ? new() : ResultStruct.Result.From(errorStruct);
        _readonlyStruct = Success ? new() : ResultReadonlyStruct.Result.From(errorReadonlyStruct);
        _class = Success ? new() : ResultClass.Result.From(errorClass);
        _recordClass = Success ? new() : ResultRecordClass.Result.From(errorRecordClass);
        _union = Success ? new ResultUnion.Result() : ResultUnion.Result.From(errorUnion);
        _unionCustom = Success ? new ResultUnionCustom.Result() : ResultUnionCustom.Result.From(errorUnionCustom);
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
