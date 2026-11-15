using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingHasValueVoidResultBenchmarks
{
    private ResultRecordStruct.VoidResult _recordStruct;
    private ResultStruct.VoidResult _struct;
    private ResultReadonlyStruct.VoidResult _readonlyStruct;
    private ResultClass.VoidResult _class = null!;
    private ResultRecordClass.VoidResult _recordClass = null!;
    private ResultUnion.VoidResult _union;
    private ResultUnionCustom.VoidResult _unionCustom;

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

        _recordStruct = Success ? new() : ResultRecordStruct.VoidResult.From(errorRecordStruct);
        _struct = Success ? new() : ResultStruct.VoidResult.From(errorStruct);
        _readonlyStruct = Success ? new() : ResultReadonlyStruct.VoidResult.From(errorReadonlyStruct);
        _class = Success ? new() : ResultClass.VoidResult.From(errorClass);
        _recordClass = Success ? new() : ResultRecordClass.VoidResult.From(errorRecordClass);
        _union = Success ? new ResultUnion.VoidResult() : ResultUnion.VoidResult.From(errorUnion);
        _unionCustom = Success ? new ResultUnionCustom.VoidResult() : ResultUnionCustom.VoidResult.From(errorUnionCustom);
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
