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
public class CreatingErrorsFromHashSetValueResultBenchmarks
{
    private const int ErrorsCount = 5;

    private HashSet<Error> _errors = default!;

    [GlobalSetup]
    public void Setup()
        => _errors = [.. Enumerable.Range(1, ErrorsCount).Select(static index => new Error($"fake{index}"))];

    [Benchmark]
    public ResultRecordStruct<FakeModel> RecordStruct_CreateErrors() => ResultRecordStruct<FakeModel>.From(_errors);

    [Benchmark]
    public ResultStruct<FakeModel> Struct_CreateErrors() => ResultStruct<FakeModel>.From(_errors);

    [Benchmark]
    public ResultReadonlyStruct<FakeModel> ReadonlyStruct_CreateErrors() => ResultReadonlyStruct<FakeModel>.From(_errors);

    [Benchmark]
    public ResultClass<FakeModel> Class_CreateErrors() => ResultClass<FakeModel>.From(_errors);

    [Benchmark]
    public ResultRecordClass<FakeModel> RecordClass_CreateErrors() => ResultRecordClass<FakeModel>.From(_errors);

    [Benchmark(Baseline = true)]
    public ResultUnion<FakeModel> Union_CreateErrors() => ResultUnion<FakeModel>.From(_errors);

    [Benchmark]
    public ResultUnionCustom<FakeModel> UnionCustom_CreateErrors() => ResultUnionCustom<FakeModel>.From(_errors);
}
