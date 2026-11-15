using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.Common;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ImplicitFromListValueResultBenchmarks
{
    private const int ErrorsCount = 5;

    private List<Error> _errors = default!;

    [GlobalSetup]
    public void Setup()
        => _errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

    [Benchmark]
    public ResultRecordStruct<FakeModel> RecordStruct_CreateErrors() => _errors;

    [Benchmark]
    public ResultStruct<FakeModel> Struct_CreateErrors() => _errors;

    [Benchmark]
    public ResultReadonlyStruct<FakeModel> ReadonlyStruct_CreateErrors() => _errors;

    [Benchmark]
    public ResultClass<FakeModel> Class_CreateErrors() => _errors;

    [Benchmark]
    public ResultRecordClass<FakeModel> RecordClass_CreateErrors() => _errors;

    [Benchmark]
    public ResultUnion<FakeModel> Union_CreateErrors() => _errors;

    [Benchmark(Baseline = true)]
    public ResultUnionCustom<FakeModel> UnionCustom_CreateErrors() => _errors;
}
