using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorsFromImmutableArrayValueResultBenchmarks
{
    private const int ErrorsCount = 5;

    private ImmutableArray<ResultRecordStruct.Error> _errorsRecordStruct = default!;
    private ImmutableArray<ResultStruct.Error> _errorsStruct = default!;
    private ImmutableArray<ResultReadonlyStruct.Error> _errorsReadonlyStruct = default!;
    private ImmutableArray<ResultClass.Error> _errorsClass = default!;
    private ImmutableArray<ResultRecordClass.Error> _errorsRecordClass = default!;
    private ImmutableArray<ResultUnion.Error> _errorsUnion = default!;
    private ImmutableArray<ResultUnionCustom.Error> _errorsUnionCustom = default!;

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
    public ResultRecordStruct.ValueResult<FakeModel> RecordStruct_CreateErrors() => new(_errorsRecordStruct);

    [Benchmark]
    public ResultStruct.ValueResult<FakeModel> Struct_CreateErrors() => new(_errorsStruct);

    [Benchmark]
    public ResultReadonlyStruct.ValueResult<FakeModel> ReadonlyStruct_CreateErrors() => new(_errorsReadonlyStruct);

    [Benchmark]
    public ResultClass.ValueResult<FakeModel> Class_CreateErrors() => new(_errorsClass);

    [Benchmark]
    public ResultRecordClass.ValueResult<FakeModel> RecordClass_CreateErrors() => new(_errorsRecordClass);

    [Benchmark]
    public ResultUnion.ValueResult<FakeModel> Union_CreateErrors() => _errorsUnion;

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.ValueResult<FakeModel> UnionCustom_CreateErrors() => _errorsUnionCustom;
}
