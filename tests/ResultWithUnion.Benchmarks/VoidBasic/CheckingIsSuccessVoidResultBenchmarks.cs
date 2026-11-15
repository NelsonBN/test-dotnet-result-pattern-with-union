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

    [Params(true, false)]
    public bool Success { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Error error = new("fake");

        _recordStruct = Success ? new() : new(error);
        _struct = Success ? new() : new(error);
        _readonlyStruct = Success ? new() : new(error);
        _class = Success ? new() : new(error);
        _recordClass = Success ? new() : new(error);
        _union = Success ? new ResultUnion() : error;
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
}
