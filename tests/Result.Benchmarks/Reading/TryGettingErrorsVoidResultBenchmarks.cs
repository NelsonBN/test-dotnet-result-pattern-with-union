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
    private ResultRecordStruct.VoidResult _recordStruct;
    private ResultStruct.VoidResult _struct;
    private ResultReadonlyStruct.VoidResult _readonlyStruct;
    private ResultClass.VoidResult _class = null!;
    private ResultRecordClass.VoidResult _recordClass = null!;
    private ResultUnion.VoidResult _union;
    private ResultUnionCustom.VoidResult _unionCustom;

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

        _recordStruct = ErrorsCount == 1 ? ResultRecordStruct.VoidResult.From(errorsRecordStruct[0]) : ResultRecordStruct.VoidResult.From(errorsRecordStruct);
        _struct = ErrorsCount == 1 ? ResultStruct.VoidResult.From(errorsStruct[0]) : ResultStruct.VoidResult.From(errorsStruct);
        _readonlyStruct = ErrorsCount == 1 ? ResultReadonlyStruct.VoidResult.From(errorsReadonlyStruct[0]) : ResultReadonlyStruct.VoidResult.From(errorsReadonlyStruct);
        _class = ErrorsCount == 1 ? ResultClass.VoidResult.From(errorsClass[0]) : ResultClass.VoidResult.From(errorsClass);
        _recordClass = ErrorsCount == 1 ? ResultRecordClass.VoidResult.From(errorsRecordClass[0]) : ResultRecordClass.VoidResult.From(errorsRecordClass);
        _union = ResultUnion.VoidResult.From(errorsUnion);
        _unionCustom = ResultUnionCustom.VoidResult.From(errorsUnionCustom);
    }

    [Benchmark]
    public ImmutableArray<ResultRecordStruct.Error> RecordStruct_TryGetErrors()
    {
        _recordStruct.TryGetValue(out ImmutableArray<ResultRecordStruct.Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<ResultStruct.Error> Struct_TryGetErrors()
    {
        _struct.TryGetValue(out ImmutableArray<ResultStruct.Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<ResultReadonlyStruct.Error> ReadonlyStruct_TryGetErrors()
    {
        _readonlyStruct.TryGetValue(out ImmutableArray<ResultReadonlyStruct.Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<ResultClass.Error> Class_TryGetErrors()
    {
        _class.TryGetValue(out ImmutableArray<ResultClass.Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<ResultRecordClass.Error> RecordClass_TryGetErrors()
    {
        _recordClass.TryGetValue(out ImmutableArray<ResultRecordClass.Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<ResultUnion.Error> Union_TryGetErrors()
    {
        return _union is ImmutableArray<ResultUnion.Error> errors ? errors : default;
    }

    [Benchmark(Baseline = true)]
    public ImmutableArray<ResultUnionCustom.Error> UnionCustom_TryGetErrors()
    {
        _unionCustom.TryGetValue(out ImmutableArray<ResultUnionCustom.Error> errors);
        return errors;
    }
}
