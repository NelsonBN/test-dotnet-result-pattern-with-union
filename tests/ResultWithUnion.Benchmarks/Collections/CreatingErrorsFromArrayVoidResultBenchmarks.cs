using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorsFromArrayVoidResultBenchmarks
{
    private const int ErrorsCount = 5;

    private Error[] _errors = default!;

    [GlobalSetup]
    public void Setup()
        => _errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

    [Benchmark]
    public ResultRecordStruct RecordStruct_CreateErrors() => ResultRecordStruct.From(_errors);

    [Benchmark]
    public ResultStruct Struct_CreateErrors() => ResultStruct.From(_errors);

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateErrors() => ResultReadonlyStruct.From(_errors);

    [Benchmark]
    public ResultClass Class_CreateErrors() => ResultClass.From(_errors);

    [Benchmark]
    public ResultRecordClass RecordClass_CreateErrors() => ResultRecordClass.From(_errors);

    [Benchmark(Baseline = true)]
    public ResultUnion Union_CreateErrors() => ResultUnion.From(_errors);

    [Benchmark]
    public ResultUnionCustom UnionCustom_CreateErrors() => ResultUnionCustom.From(_errors);
}
