using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

/// <summary>Reads <c>Errors</c> from a failed result holding one error or several.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class GettingErrorsVoidResultBenchmarks
{
    private ResultRecordStruct _recordStruct;
    private ResultStruct _struct;
    private ResultReadonlyStruct _readonlyStruct;
    private ResultClass _class = null!;
    private ResultRecordClass _recordClass = null!;
    private ResultUnion _union;
    private ResultUnionCustom _unionCustom;

    [Params(1, 5)]
    public int ErrorsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Error[] errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

        _recordStruct = ErrorsCount == 1 ? ResultRecordStruct.From(errors[0]) : ResultRecordStruct.From(errors);
        _struct = ErrorsCount == 1 ? ResultStruct.From(errors[0]) : ResultStruct.From(errors);
        _readonlyStruct = ErrorsCount == 1 ? ResultReadonlyStruct.From(errors[0]) : ResultReadonlyStruct.From(errors);
        _class = ErrorsCount == 1 ? ResultClass.From(errors[0]) : ResultClass.From(errors);
        _recordClass = ErrorsCount == 1 ? ResultRecordClass.From(errors[0]) : ResultRecordClass.From(errors);
        _union = ResultUnion.From(errors);
        _unionCustom = ResultUnionCustom.From(errors);
    }

    [Benchmark]
    public ImmutableArray<Error> RecordStruct_Errors() => _recordStruct.Errors;

    [Benchmark]
    public ImmutableArray<Error> Struct_Errors() => _struct.Errors;

    [Benchmark]
    public ImmutableArray<Error> ReadonlyStruct_Errors() => _readonlyStruct.Errors;

    [Benchmark]
    public ImmutableArray<Error> Class_Errors() => _class.Errors;

    [Benchmark]
    public ImmutableArray<Error> RecordClass_Errors() => _recordClass.Errors;

    [Benchmark]
    public ImmutableArray<Error> Union_Errors() => _union.Errors;

    [Benchmark(Baseline = true)]
    public ImmutableArray<Error> UnionCustom_Errors() => _unionCustom.Errors;
}
