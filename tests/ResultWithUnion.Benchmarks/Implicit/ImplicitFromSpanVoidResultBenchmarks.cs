using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ImplicitFromSpanVoidResultBenchmarks
{
    private const int ErrorsCount = 5;

    private Error[] _errors = default!;

    [GlobalSetup]
    public void Setup()
        => _errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

    [Benchmark]
    public ResultRecordStruct RecordStruct_CreateErrors()
    {
        ReadOnlySpan<Error> errors = _errors;
        return errors;
    }

    [Benchmark]
    public ResultStruct Struct_CreateErrors()
    {
        ReadOnlySpan<Error> errors = _errors;
        return errors;
    }

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateErrors()
    {
        ReadOnlySpan<Error> errors = _errors;
        return errors;
    }

    [Benchmark]
    public ResultClass Class_CreateErrors()
    {
        ReadOnlySpan<Error> errors = _errors;
        return errors;
    }

    [Benchmark]
    public ResultRecordClass RecordClass_CreateErrors()
    {
        ReadOnlySpan<Error> errors = _errors;
        return errors;
    }

    [Benchmark(Baseline = true)]
    public ResultUnion Union_CreateErrors()
    {
        ReadOnlySpan<Error> errors = _errors;
        return errors;
    }

    [Benchmark]
    public ResultUnionCustom UnionCustom_CreateErrors()
    {
        ReadOnlySpan<Error> errors = _errors;
        return errors;
    }
}
