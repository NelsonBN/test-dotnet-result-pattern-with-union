using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

/// <summary>Reads the errors with <c>TryGetValue</c> from a failed result holding one error or several.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TryGettingErrorsVoidResultBenchmarks
{
    private ResultRecordStruct.Result _recordStruct;
    private ResultStruct.Result _struct;
    private ResultReadonlyStruct.Result _readonlyStruct;
    private ResultClass.Result _class = null!;
    private ResultRecordClass.Result _recordClass = null!;
    private ResultUnion.Result _union;
    private ResultUnionCustom.Result _unionCustom;
    private ResultUnionCustomExtreme.Result _unionCustomExtreme;

    [Params(1, 5)]
    public int ErrorsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        ResultRecordStruct.Error[] errorsRecordStruct = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultRecordStruct.Error($"fake{index}"))];
        ResultStruct.Error[] errorsStruct = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultStruct.Error($"fake{index}"))];
        ResultReadonlyStruct.Error[] errorsReadonlyStruct = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultReadonlyStruct.Error($"fake{index}"))];
        ResultClass.Error[] errorsClass = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultClass.Error($"fake{index}"))];
        ResultRecordClass.Error[] errorsRecordClass = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultRecordClass.Error($"fake{index}"))];
        ResultUnion.Error[] errorsUnion = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultUnion.Error($"fake{index}"))];
        ResultUnionCustom.Error[] errorsUnionCustom = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultUnionCustom.Error($"fake{index}"))];
        ResultUnionCustomExtreme.Error[] errorsUnionCustomExtreme = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultUnionCustomExtreme.Error($"fake{index}"))];

        _recordStruct = ErrorsCount == 1 ? ResultRecordStruct.Result.From(errorsRecordStruct[0]) : ResultRecordStruct.Result.From(errorsRecordStruct);
        _struct = ErrorsCount == 1 ? ResultStruct.Result.From(errorsStruct[0]) : ResultStruct.Result.From(errorsStruct);
        _readonlyStruct = ErrorsCount == 1 ? ResultReadonlyStruct.Result.From(errorsReadonlyStruct[0]) : ResultReadonlyStruct.Result.From(errorsReadonlyStruct);
        _class = ErrorsCount == 1 ? ResultClass.Result.From(errorsClass[0]) : ResultClass.Result.From(errorsClass);
        _recordClass = ErrorsCount == 1 ? ResultRecordClass.Result.From(errorsRecordClass[0]) : ResultRecordClass.Result.From(errorsRecordClass);
        _union = ResultUnion.Result.From(errorsUnion);
        _unionCustom = ResultUnionCustom.Result.From(errorsUnionCustom);
        _unionCustomExtreme = ResultUnionCustomExtreme.Result.From(errorsUnionCustomExtreme);
    }

    [Benchmark]
    public ImmutableArray<ResultRecordStruct.Error> RecordStruct_TryGetErrors()
    {
        return _recordStruct.TryGetValue(out ImmutableArray<ResultRecordStruct.Error> errors) ? errors : default;
    }

    [Benchmark]
    public ImmutableArray<ResultStruct.Error> Struct_TryGetErrors()
    {
        return _struct.TryGetValue(out ImmutableArray<ResultStruct.Error> errors) ? errors : default;
    }

    [Benchmark]
    public ImmutableArray<ResultReadonlyStruct.Error> ReadonlyStruct_TryGetErrors()
    {
        return _readonlyStruct.TryGetValue(out ImmutableArray<ResultReadonlyStruct.Error> errors) ? errors : default;
    }

    [Benchmark]
    public ImmutableArray<ResultClass.Error> Class_TryGetErrors()
    {
        return _class.TryGetValue(out ImmutableArray<ResultClass.Error> errors) ? errors : default;
    }

    [Benchmark]
    public ImmutableArray<ResultRecordClass.Error> RecordClass_TryGetErrors()
    {
        return _recordClass.TryGetValue(out ImmutableArray<ResultRecordClass.Error> errors) ? errors : default;
    }

    [Benchmark]
    public ImmutableArray<ResultUnion.Error> Union_TryGetErrors()
    {
        return _union is ImmutableArray<ResultUnion.Error> errors ? errors : default;
    }

    [Benchmark(Baseline = true)]
    public ImmutableArray<ResultUnionCustom.Error> UnionCustom_TryGetErrors()
    {
        return _unionCustom.TryGetValue(out ImmutableArray<ResultUnionCustom.Error> errors) ? errors : default;
    }

    [Benchmark]
    public ImmutableArray<ResultUnionCustomExtreme.Error> UnionCustomExtreme_TryGetErrors()
        => _unionCustomExtreme.TryGetValue(out var errors) ? errors : default;
}
