using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ImplicitFromSpanValueResultBenchmarks
{
    private const int ErrorsCount = 5;

    private ResultRecordStruct.Error[] _errorsRecordStruct = default!;
    private ResultStruct.Error[] _errorsStruct = default!;
    private ResultReadonlyStruct.Error[] _errorsReadonlyStruct = default!;
    private ResultClass.Error[] _errorsClass = default!;
    private ResultRecordClass.Error[] _errorsRecordClass = default!;
    private ResultUnion.Error[] _errorsUnion = default!;
    private ResultUnionCustom.Error[] _errorsUnionCustom = default!;

    [GlobalSetup]
    public void Setup()
    {
        _errorsRecordStruct = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultRecordStruct.Error($"fake{index}"))];
        _errorsStruct = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultStruct.Error($"fake{index}"))];
        _errorsReadonlyStruct = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultReadonlyStruct.Error($"fake{index}"))];
        _errorsClass = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultClass.Error($"fake{index}"))];
        _errorsRecordClass = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultRecordClass.Error($"fake{index}"))];
        _errorsUnion = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultUnion.Error($"fake{index}"))];
        _errorsUnionCustom = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new ResultUnionCustom.Error($"fake{index}"))];
    }

    [Benchmark]
    public ResultRecordStruct.Result<FakeModel> RecordStruct_CreateErrors()
    {
        ReadOnlySpan<ResultRecordStruct.Error> errors = _errorsRecordStruct;
        return errors;
    }

    [Benchmark]
    public ResultStruct.Result<FakeModel> Struct_CreateErrors()
    {
        ReadOnlySpan<ResultStruct.Error> errors = _errorsStruct;
        return errors;
    }

    [Benchmark]
    public ResultReadonlyStruct.Result<FakeModel> ReadonlyStruct_CreateErrors()
    {
        ReadOnlySpan<ResultReadonlyStruct.Error> errors = _errorsReadonlyStruct;
        return errors;
    }

    [Benchmark]
    public ResultClass.Result<FakeModel> Class_CreateErrors()
    {
        ReadOnlySpan<ResultClass.Error> errors = _errorsClass;
        return errors;
    }

    [Benchmark]
    public ResultRecordClass.Result<FakeModel> RecordClass_CreateErrors()
    {
        ReadOnlySpan<ResultRecordClass.Error> errors = _errorsRecordClass;
        return errors;
    }

    [Benchmark]
    public ResultUnion.Result<FakeModel> Union_CreateErrors()
    {
        ReadOnlySpan<ResultUnion.Error> errors = _errorsUnion;
        return errors;
    }

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.Result<FakeModel> UnionCustom_CreateErrors()
    {
        ReadOnlySpan<ResultUnionCustom.Error> errors = _errorsUnionCustom;
        return errors;
    }
}
