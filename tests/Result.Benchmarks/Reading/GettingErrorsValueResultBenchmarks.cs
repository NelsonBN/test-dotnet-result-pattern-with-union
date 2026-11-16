using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

/// <summary>Reads <c>Errors</c> from a failed result holding one error or several.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class GettingErrorsValueResultBenchmarks
{
    private ResultRecordStruct.Result<FakeModel> _recordStruct;
    private ResultStruct.Result<FakeModel> _struct;
    private ResultReadonlyStruct.Result<FakeModel> _readonlyStruct;
    private ResultClass.Result<FakeModel> _class = null!;
    private ResultRecordClass.Result<FakeModel> _recordClass = null!;
    private ResultUnion.Result<FakeModel> _union;
    private ResultUnionCustom.Result<FakeModel> _unionCustom;
    private ResultUnionCustomExtreme.Result<FakeModel> _unionCustomExtreme;

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

        _recordStruct = ErrorsCount == 1 ? ResultRecordStruct.Result<FakeModel>.From(errorsRecordStruct[0]) : ResultRecordStruct.Result<FakeModel>.From(errorsRecordStruct);
        _struct = ErrorsCount == 1 ? ResultStruct.Result<FakeModel>.From(errorsStruct[0]) : ResultStruct.Result<FakeModel>.From(errorsStruct);
        _readonlyStruct = ErrorsCount == 1 ? ResultReadonlyStruct.Result<FakeModel>.From(errorsReadonlyStruct[0]) : ResultReadonlyStruct.Result<FakeModel>.From(errorsReadonlyStruct);
        _class = ErrorsCount == 1 ? ResultClass.Result<FakeModel>.From(errorsClass[0]) : ResultClass.Result<FakeModel>.From(errorsClass);
        _recordClass = ErrorsCount == 1 ? ResultRecordClass.Result<FakeModel>.From(errorsRecordClass[0]) : ResultRecordClass.Result<FakeModel>.From(errorsRecordClass);
        _union = ResultUnion.Result<FakeModel>.From(errorsUnion);
        _unionCustom = ResultUnionCustom.Result<FakeModel>.From(errorsUnionCustom);
        _unionCustomExtreme = ResultUnionCustomExtreme.Result<FakeModel>.From(errorsUnionCustomExtreme);
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

    [Benchmark]
    public ImmutableArray<ResultUnionCustomExtreme.Error> UnionCustomExtreme_Errors() => _unionCustomExtreme.Errors;
}
