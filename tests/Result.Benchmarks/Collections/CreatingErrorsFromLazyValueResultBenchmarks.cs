using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorsFromLazyValueResultBenchmarks
{
    private const int ErrorsCount = 5;

    private IEnumerable<ResultRecordStruct.Error> _errorsRecordStruct = default!;
    private IEnumerable<ResultStruct.Error> _errorsStruct = default!;
    private IEnumerable<ResultReadonlyStruct.Error> _errorsReadonlyStruct = default!;
    private IEnumerable<ResultClass.Error> _errorsClass = default!;
    private IEnumerable<ResultRecordClass.Error> _errorsRecordClass = default!;
    private IEnumerable<ResultUnion.Error> _errorsUnion = default!;
    private IEnumerable<ResultUnionCustom.Error> _errorsUnionCustom = default!;

    [GlobalSetup]
    public void Setup()
    {
        _errorsRecordStruct = Enumerable.Range(1, ErrorsCount).Select(static index => new ResultRecordStruct.Error($"fake{index}")).ToArray().Where(static _ => true);
        _errorsStruct = Enumerable.Range(1, ErrorsCount).Select(static index => new ResultStruct.Error($"fake{index}")).ToArray().Where(static _ => true);
        _errorsReadonlyStruct = Enumerable.Range(1, ErrorsCount).Select(static index => new ResultReadonlyStruct.Error($"fake{index}")).ToArray().Where(static _ => true);
        _errorsClass = Enumerable.Range(1, ErrorsCount).Select(static index => new ResultClass.Error($"fake{index}")).ToArray().Where(static _ => true);
        _errorsRecordClass = Enumerable.Range(1, ErrorsCount).Select(static index => new ResultRecordClass.Error($"fake{index}")).ToArray().Where(static _ => true);
        _errorsUnion = Enumerable.Range(1, ErrorsCount).Select(static index => new ResultUnion.Error($"fake{index}")).ToArray().Where(static _ => true);
        _errorsUnionCustom = Enumerable.Range(1, ErrorsCount).Select(static index => new ResultUnionCustom.Error($"fake{index}")).ToArray().Where(static _ => true);
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
