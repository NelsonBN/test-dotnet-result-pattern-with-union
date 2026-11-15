using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

/// <summary>Reads <c>Errors</c> from a failed result holding one error or several.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class GettingErrorsVoidResultBenchmarks
{
    private ResultRecordStruct.Result _recordStruct;
    private ResultStruct.Result _struct;
    private ResultReadonlyStruct.Result _readonlyStruct;
    private ResultClass.Result _class = null!;
    private ResultRecordClass.Result _recordClass = null!;
    private ResultUnion.Result _union;
    private ResultUnionCustom.Result _unionCustom;

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

        _recordStruct = ErrorsCount == 1 ? ResultRecordStruct.Result.From(errorsRecordStruct[0]) : ResultRecordStruct.Result.From(errorsRecordStruct);
        _struct = ErrorsCount == 1 ? ResultStruct.Result.From(errorsStruct[0]) : ResultStruct.Result.From(errorsStruct);
        _readonlyStruct = ErrorsCount == 1 ? ResultReadonlyStruct.Result.From(errorsReadonlyStruct[0]) : ResultReadonlyStruct.Result.From(errorsReadonlyStruct);
        _class = ErrorsCount == 1 ? ResultClass.Result.From(errorsClass[0]) : ResultClass.Result.From(errorsClass);
        _recordClass = ErrorsCount == 1 ? ResultRecordClass.Result.From(errorsRecordClass[0]) : ResultRecordClass.Result.From(errorsRecordClass);
        _union = ResultUnion.Result.From(errorsUnion);
        _unionCustom = ResultUnionCustom.Result.From(errorsUnionCustom);
    }

    [Benchmark]
    public ImmutableArray<ResultRecordStruct.Error> RecordStruct_Errors() => _recordStruct.Errors;

    [Benchmark]
    public ImmutableArray<ResultStruct.Error> Struct_Errors() => _struct.Errors;

    [Benchmark]
    public ImmutableArray<ResultReadonlyStruct.Error> ReadonlyStruct_Errors() => _readonlyStruct.Errors;

    [Benchmark]
    public ImmutableArray<ResultClass.Error> Class_Errors() => _class.Errors;

    [Benchmark]
    public ImmutableArray<ResultRecordClass.Error> RecordClass_Errors() => _recordClass.Errors;

    [Benchmark]
    public ImmutableArray<ResultUnion.Error> Union_Errors() => _union.Errors;

    [Benchmark(Baseline = true)]
    public ImmutableArray<ResultUnionCustom.Error> UnionCustom_Errors() => _unionCustom.Errors;
}
