using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorsFromHashSetVoidResultBenchmarks
{
    private const int ErrorsCount = 5;

    private HashSet<Error> _errors = default!;

    [GlobalSetup]
    public void Setup()
        => _errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

    [Benchmark]
    public ResultRecordStruct RecordStruct_CreateErrors() => new(_errors);

    [Benchmark]
    public ResultStruct Struct_CreateErrors() => new(_errors);

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateErrors() => new(_errors);

    [Benchmark]
    public ResultClass Class_CreateErrors() => new(_errors);

    [Benchmark]
    public ResultRecordClass RecordClass_CreateErrors() => new(_errors);

    [Benchmark(Baseline = true)]
    public ResultUnion Union_CreateErrors() => ResultUnion.From(_errors);

    [Benchmark]
    public ResultUnionNonBoxing UnionNonBoxing_CreateErrors() => ResultUnionNonBoxing.From(_errors);
}
