using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Switching;

/// <summary>
/// Consumes a value result by case: the unions use a <c>switch</c> expression, the other variants the equivalent <c>TryGetValue</c> chain.
/// For <c>UnionCustom</c> this shows which members the compiler picks and whether <c>object? Value</c> boxes.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SwitchingValueResultBenchmarks
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
    public int RecordStruct_Switch()
    {
        if(_recordStruct.TryGetValue(out FakeModel value))
        {
            return value.Id;
        }

        return _recordStruct.TryGetValue(out ImmutableArray<ResultRecordStruct.Error> errors) ? errors.Length : -1;
    }

    [Benchmark]
    public int Struct_Switch()
    {
        if(_struct.TryGetValue(out FakeModel value))
        {
            return value.Id;
        }

        return _struct.TryGetValue(out ImmutableArray<ResultStruct.Error> errors) ? errors.Length : -1;
    }

    [Benchmark]
    public int ReadonlyStruct_Switch()
    {
        if(_readonlyStruct.TryGetValue(out FakeModel value))
        {
            return value.Id;
        }

        return _readonlyStruct.TryGetValue(out ImmutableArray<ResultReadonlyStruct.Error> errors) ? errors.Length : -1;
    }

    [Benchmark]
    public int Class_Switch()
    {
        if(_class.TryGetValue(out FakeModel value))
        {
            return value.Id;
        }

        return _class.TryGetValue(out ImmutableArray<ResultClass.Error> errors) ? errors.Length : -1;
    }

    [Benchmark]
    public int RecordClass_Switch()
    {
        if(_recordClass.TryGetValue(out FakeModel value))
        {
            return value.Id;
        }

        return _recordClass.TryGetValue(out ImmutableArray<ResultRecordClass.Error> errors) ? errors.Length : -1;
    }

    [Benchmark]
    public int Union_Switch()
        => _union switch
        {
            FakeModel value => value.Id,
            ImmutableArray<ResultUnion.Error> errors => errors.Length,
            null => -1,
        };

    [Benchmark(Baseline = true)]
    public int UnionCustom_Switch()
        => _unionCustom switch
        {
            FakeModel value => value.Id,
            ImmutableArray<ResultUnionCustom.Error> errors => errors.Length,
            null => -1,
        };

    [Benchmark]
    public int UnionCustomExtreme_Switch()
        => _unionCustomExtreme switch
        {
            FakeModel value => value.Id,
            ImmutableArray<ResultUnionCustomExtreme.Error> errors => errors.Length,
            null => -1,
        };
}
