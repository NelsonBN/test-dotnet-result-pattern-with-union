using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.VoidBasic;

namespace ResultWithUnion.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingSuccessVoidResultBenchmarks
{
    private FakeModel _model = null!;

    [GlobalSetup]
    public void Setup() => _model = new();

    [Benchmark(Baseline = true)]
    public ResultRecordStruct RecordStruct_CreateSuccess() => new();

    [Benchmark]
    public ResultStruct Struct_CreateSuccess() => new();

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateSuccess() => new();

    [Benchmark]
    public ResultClass Class_CreateSuccess() => new();

    [Benchmark]
    public ResultRecordClass RecordClass_CreateSuccess() => new();

    [Benchmark]
    public ResultUnion Union_CreateSuccess() => new();
}
