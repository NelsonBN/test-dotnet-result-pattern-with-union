using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorsFromArrayValueResultBenchmarks
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
    public ResultRecordStruct.ValueResult<FakeModel> RecordStruct_CreateErrors() => ResultRecordStruct.ValueResult<FakeModel>.From(_errorsRecordStruct);

    [Benchmark]
    public ResultStruct.ValueResult<FakeModel> Struct_CreateErrors() => ResultStruct.ValueResult<FakeModel>.From(_errorsStruct);

    [Benchmark]
    public ResultReadonlyStruct.ValueResult<FakeModel> ReadonlyStruct_CreateErrors() => ResultReadonlyStruct.ValueResult<FakeModel>.From(_errorsReadonlyStruct);

    [Benchmark]
    public ResultClass.ValueResult<FakeModel> Class_CreateErrors() => ResultClass.ValueResult<FakeModel>.From(_errorsClass);

    [Benchmark]
    public ResultRecordClass.ValueResult<FakeModel> RecordClass_CreateErrors() => ResultRecordClass.ValueResult<FakeModel>.From(_errorsRecordClass);

    [Benchmark]
    public ResultUnion.ValueResult<FakeModel> Union_CreateErrors() => ResultUnion.ValueResult<FakeModel>.From(_errorsUnion);

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.ValueResult<FakeModel> UnionCustom_CreateErrors() => ResultUnionCustom.ValueResult<FakeModel>.From(_errorsUnionCustom);
}
