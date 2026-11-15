using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CheckingIsSuccessVoidResultBenchmarks
{
    private ResultRecordStruct _recordStruct;
    private ResultStruct _struct;
    private ResultReadonlyStruct _readonlyStruct;
    private ResultClass _class = null!;
    private ResultRecordClass _recordClass = null!;
    private ResultUnion _union;
    private ResultUnionNonBoxing _unionNonBoxing;

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
        _unionNonBoxing = Success ? new ResultUnionNonBoxing() : ResultUnionNonBoxing.From(error);
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
