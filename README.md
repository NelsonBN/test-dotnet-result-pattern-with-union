# test-dotnet-result-pattern-with-union

An experiment, not a library: the **Result pattern** implemented the classic way versus built on the **C# 15 `union`** type (.NET 11), measured with [BenchmarkDotNet](https://benchmarkdotnet.org/) for speed and allocations.

## Variants

Each variant exists as a value result (`src/ValueBasic/`, `Result<Variant><TValue>`) and a void result (`src/VoidBasic/`, `Result<Variant>`). All use the shared `Error` in `src/Common`.

| Variant | Types |
|---------|-------|
| struct, readonly struct, record struct | `ResultStruct`, `ResultReadonlyStruct`, `ResultRecordStruct` |
| sealed class, sealed record class | `ResultClass`, `ResultRecordClass` |
| C# 15 union | `union ResultUnion<T>(T, Error)` / `union ResultUnion(Error)` |

## Running the benchmarks

```bash
dotnet build
dotnet run -c Release --project tests/ResultWithUnion.Benchmarks -- --filter "*CreatingValueResult*" --job short --inProcess
```

BenchmarkDotNet 0.15.8 does not recognise the net11 runtime, so `--inProcess` is currently required. See `AGENTS.md` for the scenario list and conventions.

## Results

No published results yet. Add them here, from a single machine and run, once the full benchmarks have been executed.
