using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ImplicitFromErrorValueResultBenchmarks
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
        _errorRecordStruct = new("fake1");
        _errorStruct = new("fake1");
        _errorReadonlyStruct = new("fake1");
        _errorClass = new("fake1");
        _errorRecordClass = new("fake1");
        _errorUnion = new("fake1");
        _errorUnionCustom = new("fake1");
    }

    [Benchmark]
    public ResultRecordStruct.ValueResult<FakeModel> RecordStruct_CreateErrors() => _errorRecordStruct;

    [Benchmark]
    public ResultStruct.ValueResult<FakeModel> Struct_CreateErrors() => _errorStruct;

    [Benchmark]
    public ResultReadonlyStruct.ValueResult<FakeModel> ReadonlyStruct_CreateErrors() => _errorReadonlyStruct;

    [Benchmark]
    public ResultClass.ValueResult<FakeModel> Class_CreateErrors() => _errorClass;

    [Benchmark]
    public ResultRecordClass.ValueResult<FakeModel> RecordClass_CreateErrors() => _errorRecordClass;

    [Benchmark]
    public ResultUnion.ValueResult<FakeModel> Union_CreateErrors() => _errorUnion;

    [Benchmark(Baseline = true)]
    public ResultUnionCustom.ValueResult<FakeModel> UnionCustom_CreateErrors() => _errorUnionCustom;
}
