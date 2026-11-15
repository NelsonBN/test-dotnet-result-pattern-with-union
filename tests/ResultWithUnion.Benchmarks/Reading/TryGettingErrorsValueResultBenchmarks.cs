using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.Common;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

/// <summary>Reads the errors with <c>TryGetValue</c> from a failed result holding one error or several.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TryGettingErrorsValueResultBenchmarks
{
    private ResultRecordStruct<FakeModel> _recordStruct;
    private ResultStruct<FakeModel> _struct;
    private ResultReadonlyStruct<FakeModel> _readonlyStruct;
    private ResultClass<FakeModel> _class = null!;
    private ResultRecordClass<FakeModel> _recordClass = null!;
    private ResultUnion<FakeModel> _union;
    private ResultUnionCustom<FakeModel> _unionCustom;

    [Params(1, 5)]
    public int ErrorsCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Error[] errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

        _recordStruct = ErrorsCount == 1 ? ResultRecordStruct<FakeModel>.From(errors[0]) : ResultRecordStruct<FakeModel>.From(errors);
        _struct = ErrorsCount == 1 ? ResultStruct<FakeModel>.From(errors[0]) : ResultStruct<FakeModel>.From(errors);
        _readonlyStruct = ErrorsCount == 1 ? ResultReadonlyStruct<FakeModel>.From(errors[0]) : ResultReadonlyStruct<FakeModel>.From(errors);
        _class = ErrorsCount == 1 ? ResultClass<FakeModel>.From(errors[0]) : ResultClass<FakeModel>.From(errors);
        _recordClass = ErrorsCount == 1 ? ResultRecordClass<FakeModel>.From(errors[0]) : ResultRecordClass<FakeModel>.From(errors);
        _union = ResultUnion<FakeModel>.From(errors);
        _unionCustom = ResultUnionCustom<FakeModel>.From(errors);
    }

    [Benchmark]
    public ImmutableArray<Error> RecordStruct_TryGetErrors()
    {
        _recordStruct.TryGetValue(out ImmutableArray<Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<Error> Struct_TryGetErrors()
    {
        _struct.TryGetValue(out ImmutableArray<Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<Error> ReadonlyStruct_TryGetErrors()
    {
        _readonlyStruct.TryGetValue(out ImmutableArray<Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<Error> Class_TryGetErrors()
    {
        _class.TryGetValue(out ImmutableArray<Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<Error> RecordClass_TryGetErrors()
    {
        _recordClass.TryGetValue(out ImmutableArray<Error> errors);
        return errors;
    }

    [Benchmark]
    public ImmutableArray<Error> Union_TryGetErrors()
    {
        return _union is ImmutableArray<Error> errors ? errors : default;
    }

    [Benchmark(Baseline = true)]
    public ImmutableArray<Error> UnionCustom_TryGetErrors()
    {
        _unionCustom.TryGetValue(out ImmutableArray<Error> errors);
        return errors;
    }
}
