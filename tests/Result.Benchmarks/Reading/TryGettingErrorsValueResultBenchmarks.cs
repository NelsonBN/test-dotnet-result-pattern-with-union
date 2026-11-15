using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

/// <summary>Reads the errors with <c>TryGetValue</c> from a failed result holding one error or several.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TryGettingErrorsValueResultBenchmarks
{
    private ResultRecordStruct.Result<FakeModel> _recordStruct;
    private ResultStruct.Result<FakeModel> _struct;
    private ResultReadonlyStruct.Result<FakeModel> _readonlyStruct;
    private ResultClass.Result<FakeModel> _class = null!;
    private ResultRecordClass.Result<FakeModel> _recordClass = null!;
    private ResultUnion.Result<FakeModel> _union;
    private ResultUnionCustom.Result<FakeModel> _unionCustom;

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

        _recordStruct = ErrorsCount == 1 ? ResultRecordStruct.Result<FakeModel>.From(errorsRecordStruct[0]) : ResultRecordStruct.Result<FakeModel>.From(errorsRecordStruct);
        _struct = ErrorsCount == 1 ? ResultStruct.Result<FakeModel>.From(errorsStruct[0]) : ResultStruct.Result<FakeModel>.From(errorsStruct);
        _readonlyStruct = ErrorsCount == 1 ? ResultReadonlyStruct.Result<FakeModel>.From(errorsReadonlyStruct[0]) : ResultReadonlyStruct.Result<FakeModel>.From(errorsReadonlyStruct);
        _class = ErrorsCount == 1 ? ResultClass.Result<FakeModel>.From(errorsClass[0]) : ResultClass.Result<FakeModel>.From(errorsClass);
        _recordClass = ErrorsCount == 1 ? ResultRecordClass.Result<FakeModel>.From(errorsRecordClass[0]) : ResultRecordClass.Result<FakeModel>.From(errorsRecordClass);
        _union = ResultUnion.Result<FakeModel>.From(errorsUnion);
        _unionCustom = ResultUnionCustom.Result<FakeModel>.From(errorsUnionCustom);
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
}
