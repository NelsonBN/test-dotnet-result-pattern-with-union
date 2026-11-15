using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Common;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorVoidResultBenchmarks
{
    private Error _error;

    [GlobalSetup]
    public void Setup() => _error = new("fake");

    [Benchmark]
    public ResultRecordStruct RecordStruct_CreateError() => new(_error);

    [Benchmark]
    public ResultStruct Struct_CreateError() => new(_error);

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateError() => new(_error);

    [Benchmark]
    public ResultClass Class_CreateError() => new(_error);

    [Benchmark]
    public ResultRecordClass RecordClass_CreateError() => new(_error);

    [Benchmark(Baseline = true)]
    public ResultUnion Union_CreateError() => ResultUnion.From(_error);

    [Benchmark]
    public ResultUnionNonBoxing UnionNonBoxing_CreateError() => ResultUnionNonBoxing.From(_error);
}
