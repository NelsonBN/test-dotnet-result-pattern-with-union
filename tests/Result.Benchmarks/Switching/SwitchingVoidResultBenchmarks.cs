using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Switching;

/// <summary>
/// Consumes a void result by case: the unions use a <c>switch</c> expression, the other variants the equivalent <c>TryGetValue</c> test.
/// For <c>UnionCustom</c> this shows which members the compiler picks and whether <c>object? Value</c> boxes.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SwitchingVoidResultBenchmarks
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
    public int RecordStruct_Switch()
        => _recordStruct.TryGetValue(out var errors) ? errors.Length : 0;

    [Benchmark]
    public int Struct_Switch()
        => _struct.TryGetValue(out var errors) ? errors.Length : 0;

    [Benchmark]
    public int ReadonlyStruct_Switch()
        => _readonlyStruct.TryGetValue(out var errors) ? errors.Length : 0;

    [Benchmark]
    public int Class_Switch()
        => _class.TryGetValue(out var errors) ? errors.Length : 0;

    [Benchmark]
    public int RecordClass_Switch()
        => _recordClass.TryGetValue(out var errors) ? errors.Length : 0;

    [Benchmark]
    public int Union_Switch()
        => _union switch
        {
            ImmutableArray<ResultUnion.Error> errors => errors.Length,
            null => 0,
        };

    [Benchmark(Baseline = true)]
    public int UnionCustom_Switch()
        => _unionCustom switch
        {
            ImmutableArray<ResultUnionCustom.Error> errors => errors.Length,
            null => 0,
        };

    [Benchmark]
    public int UnionCustomExtreme_Switch()
        => _unionCustomExtreme switch
        {
            ImmutableArray<ResultUnionCustomExtreme.Error> errors => errors.Length,
            null => 0,
        };
}
