using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.Common;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

/// <summary>Reads <c>Errors</c> from a failed result holding one error or several.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class GettingErrorsValueResultBenchmarks
{
    private ResultRecordStruct<FakeModel> _recordStruct;
    private ResultStruct<FakeModel> _struct;
    private ResultReadonlyStruct<FakeModel> _readonlyStruct;
    private ResultClass<FakeModel> _class = null!;
    private ResultRecordClass<FakeModel> _recordClass = null!;
    private ResultUnion<FakeModel> _union;
    private ResultUnionNonBoxing<FakeModel> _unionNonBoxing;

    [Params(1, 5)]
    public int ErrorsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Error[] errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

        _recordStruct = ErrorsCount == 1 ? new(errors[0]) : new(errors);
        _struct = ErrorsCount == 1 ? new(errors[0]) : new(errors);
        _readonlyStruct = ErrorsCount == 1 ? new(errors[0]) : new(errors);
        _class = ErrorsCount == 1 ? new(errors[0]) : new(errors);
        _recordClass = ErrorsCount == 1 ? new(errors[0]) : new(errors);
        _union = ErrorsCount == 1 ? errors[0] : ResultUnion<FakeModel>.From(errors);
        _unionNonBoxing = ErrorsCount == 1 ? errors[0] : ResultUnionNonBoxing<FakeModel>.From(errors);
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

    [Benchmark(Baseline = true)]
    public ImmutableArray<Error> Union_Errors() => _union.Errors;

    [Benchmark]
    public ImmutableArray<Error> UnionNonBoxing_Errors() => _unionNonBoxing.Errors;
}
