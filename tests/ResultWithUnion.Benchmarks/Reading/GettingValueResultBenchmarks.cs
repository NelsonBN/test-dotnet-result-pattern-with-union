using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

/// <summary>Reads <c>Value</c> (<c>GetValue()</c> for the union) from a successful result.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class GettingValueResultBenchmarks
{
    private ResultRecordStruct<FakeModel> _recordStruct;
    private ResultStruct<FakeModel> _struct;
    private ResultReadonlyStruct<FakeModel> _readonlyStruct;
    private ResultClass<FakeModel> _class = null!;
    private ResultRecordClass<FakeModel> _recordClass = null!;
    private ResultUnion<FakeModel> _union;
    private ResultUnionCustom<FakeModel> _unionCustom;

    [GlobalSetup]
    public void Setup()
    {
        FakeModel model = new();

        _recordStruct = new(model);
        _struct = new(model);
        _readonlyStruct = new(model);
        _class = new(model);
        _recordClass = new(model);
        _union = model;
        _unionCustom = model;
    }

    [Benchmark]
    public FakeModel RecordStruct_GetValue() => _recordStruct.Value;

    [Benchmark]
    public FakeModel Struct_GetValue() => _struct.Value;

    [Benchmark]
    public FakeModel ReadonlyStruct_GetValue() => _readonlyStruct.Value;

    [Benchmark]
    public FakeModel Class_GetValue() => _class.Value;

    [Benchmark]
    public FakeModel RecordClass_GetValue() => _recordClass.Value;

    [Benchmark(Baseline = true)]
    public FakeModel Union_GetValue() => _union.GetValue();

    [Benchmark]
    public FakeModel UnionCustom_GetValue() => _unionCustom.GetValue();
}
