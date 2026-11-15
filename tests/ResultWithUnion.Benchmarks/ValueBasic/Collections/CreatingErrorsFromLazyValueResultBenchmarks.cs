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
public class CreatingErrorsFromLazyValueResultBenchmarks
{
    private const int ErrorsCount = 5;

    private IEnumerable<Error> _errors = default!;

    [GlobalSetup]
    public void Setup()
        => _errors = Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}")).ToArray().Where(static _ => true);

    [Benchmark]
    public ResultRecordStruct<FakeModel> RecordStruct_CreateErrors() => new(_errors);

    [Benchmark]
    public ResultStruct<FakeModel> Struct_CreateErrors() => new(_errors);

    [Benchmark]
    public ResultReadonlyStruct<FakeModel> ReadonlyStruct_CreateErrors() => new(_errors);

    [Benchmark]
    public ResultClass<FakeModel> Class_CreateErrors() => new(_errors);

    [Benchmark]
    public ResultRecordClass<FakeModel> RecordClass_CreateErrors() => new(_errors);

    [Benchmark(Baseline = true)]
    public ResultUnion<FakeModel> Union_CreateErrors() => ResultUnion<FakeModel>.From(_errors);

    [Benchmark]
    public ResultUnionNonBoxing<FakeModel> UnionNonBoxing_CreateErrors() => ResultUnionNonBoxing<FakeModel>.From(_errors);
}
