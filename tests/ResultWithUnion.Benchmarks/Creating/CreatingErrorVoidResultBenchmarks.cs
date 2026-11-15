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
    public ResultRecordStruct RecordStruct_CreateError() => ResultRecordStruct.From(_error);

    [Benchmark]
    public ResultStruct Struct_CreateError() => ResultStruct.From(_error);

    [Benchmark]
    public ResultReadonlyStruct ReadonlyStruct_CreateError() => ResultReadonlyStruct.From(_error);

    [Benchmark]
    public ResultClass Class_CreateError() => ResultClass.From(_error);

    [Benchmark]
    public ResultRecordClass RecordClass_CreateError() => ResultRecordClass.From(_error);

    [Benchmark(Baseline = true)]
    public ResultUnion Union_CreateError() => ResultUnion.From(_error);

    [Benchmark]
    public ResultUnionCustom UnionCustom_CreateError() => ResultUnionCustom.From(_error);
}
