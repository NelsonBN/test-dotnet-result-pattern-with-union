using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ResultWithUnion.Benchmarks.Fakes;
using ResultWithUnion.ValueBasic;

namespace ResultWithUnion.Benchmarks.Tests;

/// <summary>Reads the value with <c>TryGetValue</c> from a successful result.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TryGettingValueResultBenchmarks
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
    public FakeModel RecordStruct_TryGetValue()
    {
        _recordStruct.TryGetValue(out FakeModel value);
        return value;
    }

    [Benchmark]
    public FakeModel Struct_TryGetValue()
    {
        _struct.TryGetValue(out FakeModel value);
        return value;
    }

    [Benchmark]
    public FakeModel ReadonlyStruct_TryGetValue()
    {
        _readonlyStruct.TryGetValue(out FakeModel value);
        return value;
    }

    [Benchmark]
    public FakeModel Class_TryGetValue()
    {
        _class.TryGetValue(out FakeModel value);
        return value;
    }

    [Benchmark]
    public FakeModel RecordClass_TryGetValue()
    {
        _recordClass.TryGetValue(out FakeModel value);
        return value;
    }

    [Benchmark(Baseline = true)]
    public FakeModel Union_TryGetValue()
    {
        return _union is FakeModel value ? value : default!;
    }

    [Benchmark]
    public FakeModel UnionCustom_TryGetValue()
    {
        _unionCustom.TryGetValue(out FakeModel value);
        return value;
    }
}
