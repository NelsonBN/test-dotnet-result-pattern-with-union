using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Result.Benchmarks.Fakes;

namespace Result.Benchmarks.Tests;

/// <summary>Reads the value with <c>TryGetValue</c> from a successful result.</summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TryGettingValueResultBenchmarks
{
    private ResultRecordStruct.Result<FakeModel> _recordStruct;
    private ResultStruct.Result<FakeModel> _struct;
    private ResultReadonlyStruct.Result<FakeModel> _readonlyStruct;
    private ResultClass.Result<FakeModel> _class = null!;
    private ResultRecordClass.Result<FakeModel> _recordClass = null!;
    private ResultUnion.Result<FakeModel> _union;
    private ResultUnionCustom.Result<FakeModel> _unionCustom;
    private ResultUnionCustomExtreme.Result<FakeModel> _unionCustomExtreme;

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
        _unionCustomExtreme = model;
    }

    [Benchmark]
    public FakeModel RecordStruct_TryGetValue()
    {
        return _recordStruct.TryGetValue(out FakeModel value) ? value : default!;
    }

    [Benchmark]
    public FakeModel Struct_TryGetValue()
    {
        return _struct.TryGetValue(out FakeModel value) ? value : default!;
    }

    [Benchmark]
    public FakeModel ReadonlyStruct_TryGetValue()
    {
        return _readonlyStruct.TryGetValue(out FakeModel value) ? value : default!;
    }

    [Benchmark]
    public FakeModel Class_TryGetValue()
    {
        return _class.TryGetValue(out FakeModel value) ? value : default!;
    }

    [Benchmark]
    public FakeModel RecordClass_TryGetValue()
    {
        return _recordClass.TryGetValue(out FakeModel value) ? value : default!;
    }

    [Benchmark]
    public FakeModel Union_TryGetValue()
    {
        return _union is FakeModel value ? value : default!;
    }

    [Benchmark(Baseline = true)]
    public FakeModel UnionCustom_TryGetValue()
    {
        return _unionCustom.TryGetValue(out FakeModel value) ? value : default!;
    }

    [Benchmark]
    public FakeModel UnionCustomExtreme_TryGetValue()
        => _unionCustomExtreme.TryGetValue(out FakeModel value) ? value : default!;
}
