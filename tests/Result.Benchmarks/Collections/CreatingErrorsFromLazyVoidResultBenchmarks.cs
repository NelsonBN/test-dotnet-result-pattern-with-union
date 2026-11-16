using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorsFromLazyVoidResultBenchmarks
{
    private const int ErrorsCount = 5;

    private IEnumerable<ResultRecordStruct.Error> _errorsRecordStruct = default!;
    private IEnumerable<ResultStruct.Error> _errorsStruct = default!;
    private IEnumerable<ResultReadonlyStruct.Error> _errorsReadonlyStruct = default!;
    private IEnumerable<ResultClass.Error> _errorsClass = default!;
    private IEnumerable<ResultRecordClass.Error> _errorsRecordClass = default!;
    private IEnumerable<ResultUnion.Error> _errorsUnion = default!;
    private IEnumerable<ResultUnionCustom.Error> _errorsUnionCustom = default!;
    private IEnumerable<ResultUnionCustomExtreme.Error> _errorsUnionCustomExtreme = default!;

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
        _errorsUnionCustomExtreme = Enumerable.Range(1, ErrorsCount).Select(static index => new ResultUnionCustomExtreme.Error($"fake{index}")).ToArray().Where(static _ => true);
    }

    [Benchmark]
    public ResultRecordStruct.Result RecordStruct_CreateErrors() => ResultRecordStruct.Result.From(_errorsRecordStruct);

    [Benchmark]
    public ResultStruct.Result Struct_CreateErrors() => ResultStruct.Result.From(_errorsStruct);

    [Benchmark]
    public ResultReadonlyStruct.Result ReadonlyStruct_CreateErrors() => ResultReadonlyStruct.Result.From(_errorsReadonlyStruct);

    [Benchmark]
    public ResultClass.Result Class_CreateErrors() => ResultClass.Result.From(_errorsClass);

    [Benchmark]
    public ResultRecordClass.Result RecordClass_CreateErrors() => ResultRecordClass.Result.From(_errorsRecordClass);

    [Benchmark]
    public ResultUnion.Result Union_CreateErrors() => ResultUnion.Result.From(_errorsUnion);

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.Result UnionCustom_CreateErrors() => ResultUnionCustom.Result.From(_errorsUnionCustom);

    [Benchmark]
    public ResultUnionCustomExtreme.Result UnionCustomExtreme_CreateErrors() => ResultUnionCustomExtreme.Result.From(_errorsUnionCustomExtreme);
}
