using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CreatingErrorVoidResultBenchmarks
{
    private ResultRecordStruct.Error _errorRecordStruct;
    private ResultStruct.Error _errorStruct;
    private ResultReadonlyStruct.Error _errorReadonlyStruct;
    private ResultClass.Error _errorClass;
    private ResultRecordClass.Error _errorRecordClass;
    private ResultUnion.Error _errorUnion;
    private ResultUnionCustom.Error _errorUnionCustom;

    [GlobalSetup]
    public void Setup()
    {
        _errorRecordStruct = new("fake");
        _errorStruct = new("fake");
        _errorReadonlyStruct = new("fake");
        _errorClass = new("fake");
        _errorRecordClass = new("fake");
        _errorUnion = new("fake");
        _errorUnionCustom = new("fake");
    }

    [Benchmark]
    public ResultRecordStruct.VoidResult RecordStruct_CreateError() => ResultRecordStruct.VoidResult.From(_errorRecordStruct);

    [Benchmark]
    public ResultStruct.VoidResult Struct_CreateError() => ResultStruct.VoidResult.From(_errorStruct);

    [Benchmark]
    public ResultReadonlyStruct.VoidResult ReadonlyStruct_CreateError() => ResultReadonlyStruct.VoidResult.From(_errorReadonlyStruct);

    [Benchmark]
    public ResultClass.VoidResult Class_CreateError() => ResultClass.VoidResult.From(_errorClass);

    [Benchmark]
    public ResultRecordClass.VoidResult RecordClass_CreateError() => ResultRecordClass.VoidResult.From(_errorRecordClass);

    [Benchmark]
    public ResultUnion.VoidResult Union_CreateError() => ResultUnion.VoidResult.From(_errorUnion);

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.VoidResult UnionCustom_CreateError() => ResultUnionCustom.VoidResult.From(_errorUnionCustom);
}
