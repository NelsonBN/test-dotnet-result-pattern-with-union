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
    public ResultRecordStruct.Result RecordStruct_CreateError() => ResultRecordStruct.Result.From(_errorRecordStruct);

    [Benchmark]
    public ResultStruct.Result Struct_CreateError() => ResultStruct.Result.From(_errorStruct);

    [Benchmark]
    public ResultReadonlyStruct.Result ReadonlyStruct_CreateError() => ResultReadonlyStruct.Result.From(_errorReadonlyStruct);

    [Benchmark]
    public ResultClass.Result Class_CreateError() => ResultClass.Result.From(_errorClass);

    [Benchmark]
    public ResultRecordClass.Result RecordClass_CreateError() => ResultRecordClass.Result.From(_errorRecordClass);

    [Benchmark]
    public ResultUnion.Result Union_CreateError() => ResultUnion.Result.From(_errorUnion);

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.Result UnionCustom_CreateError() => ResultUnionCustom.Result.From(_errorUnionCustom);
}
