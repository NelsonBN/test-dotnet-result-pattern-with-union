using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ImplicitFromArrayVoidResultBenchmarks
{
    private const int ErrorsCount = 5;

    private Error[] _errors = default!;

    [GlobalSetup]
    public void Setup()
        => _errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

    [Benchmark]
    public ResultRecordStruct RecordStruct_CreateErrors() => _errors;

    [Benchmark]
    public ResultStruct Struct_CreateErrors() => _errors;

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateErrors() => _errors;

    [Benchmark]
    public ResultClass Class_CreateErrors() => _errors;

    [Benchmark]
    public ResultRecordClass RecordClass_CreateErrors() => _errors;

    [Benchmark]
    public ResultUnion Union_CreateErrors() => _errors;

    [Benchmark(Baseline = true)]
    public ResultUnionCustom UnionCustom_CreateErrors() => _errors;
}
