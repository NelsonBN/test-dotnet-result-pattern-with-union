using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingIsSuccessVoidResultBenchmarks
{
    private ResultRecordStruct.Result _recordStruct;
    private ResultStruct.Result _struct;
    private ResultReadonlyStruct.Result _readonlyStruct;
    private ResultClass.Result _class = null!;
    private ResultRecordClass.Result _recordClass = null!;
    private ResultUnion.Result _union;
    private ResultUnionCustom.Result _unionCustom;
    private ResultUnionCustomExtreme.Result _unionCustomExtreme;

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
        ResultUnionCustomExtreme.Error errorUnionCustomExtreme = new("fake");

        _recordStruct = Success ? new() : ResultRecordStruct.Result.From(errorRecordStruct);
        _struct = Success ? new() : ResultStruct.Result.From(errorStruct);
        _readonlyStruct = Success ? new() : ResultReadonlyStruct.Result.From(errorReadonlyStruct);
        _class = Success ? new() : ResultClass.Result.From(errorClass);
        _recordClass = Success ? new() : ResultRecordClass.Result.From(errorRecordClass);
        _union = Success ? new ResultUnion.Result() : ResultUnion.Result.From(errorUnion);
        _unionCustom = Success ? new ResultUnionCustom.Result() : ResultUnionCustom.Result.From(errorUnionCustom);
        _unionCustomExtreme = Success ? new ResultUnionCustomExtreme.Result() : ResultUnionCustomExtreme.Result.From(errorUnionCustomExtreme);
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
