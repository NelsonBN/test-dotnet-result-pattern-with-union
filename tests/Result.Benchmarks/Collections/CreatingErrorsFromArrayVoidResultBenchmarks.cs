using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorsFromArrayVoidResultBenchmarks
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
    public ResultRecordStruct.VoidResult RecordStruct_CreateErrors() => ResultRecordStruct.VoidResult.From(_errorsRecordStruct);

    [Benchmark]
    public ResultStruct.VoidResult Struct_CreateErrors() => ResultStruct.VoidResult.From(_errorsStruct);

    [Benchmark]
    public ResultReadonlyStruct.VoidResult ReadonlyStruct_CreateErrors() => ResultReadonlyStruct.VoidResult.From(_errorsReadonlyStruct);

    [Benchmark]
    public ResultClass.VoidResult Class_CreateErrors() => ResultClass.VoidResult.From(_errorsClass);

    [Benchmark]
    public ResultRecordClass.VoidResult RecordClass_CreateErrors() => ResultRecordClass.VoidResult.From(_errorsRecordClass);

    [Benchmark]
    public ResultUnion.VoidResult Union_CreateErrors() => ResultUnion.VoidResult.From(_errorsUnion);

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.VoidResult UnionCustom_CreateErrors() => ResultUnionCustom.VoidResult.From(_errorsUnionCustom);
}
