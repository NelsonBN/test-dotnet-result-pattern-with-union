# test-dotnet-result-pattern-with-union

An experiment, not a library: the **Result pattern** implemented the classic way versus built on the **C# 15 `union`** type (.NET 11), measured with [BenchmarkDotNet](https://benchmarkdotnet.org/) for speed and allocations.

The question: *does a C# 15 `union` make a better Result type than the existing approaches, in performance, allocations and ergonomics?*

## Fair comparison

The non-union variants (struct, readonly struct, record struct, class, record class) follow one nearly identical implementation: the same members, constructor overloads, storage (`ImmutableArray<Error>`), exceptions and benchmark inputs. The **union is the only variant allowed to differ** (the language does not let it declare fields, `Value` or constructors), and it is the **baseline** of every benchmark, so `Ratio` reads "variant vs union". The unavoidable differences are listed in `AGENTS.md` under *Intentional deviations*.

## Variants

Each variant exists as a value result (`src/ValueBasic/`, `Result<Variant><TValue>`) and a void result (`src/VoidBasic/`, `Result<Variant>`). All use the shared `Error` in `src/Common`.

| Variant | Types |
|---------|-------|
| struct, readonly struct, record struct | `ResultStruct`, `ResultReadonlyStruct`, `ResultRecordStruct` |
| sealed class, sealed record class | `ResultClass`, `ResultRecordClass` |
| C# 15 union | `union ResultUnion<T>(T, ImmutableArray<Error>)` / `union ResultUnion(ImmutableArray<Error>)` |
| C# 15 union, non-boxing | hand-written `[Union]` readonly struct `ResultUnionNonBoxing<T>` / `ResultUnionNonBoxing` with real fields, `HasValue` and `TryGetValue` (benchmarked as `UnionNonBoxing_*`) |

The declared `union` boxes its `ImmutableArray<Error>` case into one `object?` field: with 5 errors it allocates 88 B versus 64 B for the structs (24 B box). A single `Error` is created with `From(error)`. The non-boxing variant exists to measure how much of that gap the boxing explains.

Failed results accept a single `Error` or a collection of errors: `Error[]`, `List<Error>`, `ImmutableArray<Error>`, `ReadOnlySpan<Error>` and any `IEnumerable<Error>` (sets, queues, lazy sequences; dictionaries are not supported). The errors are copied once into an immutable array. The union uses `From(...)` helpers for the same collections.

**Union limitation:** `ResultUnion<T>` cannot represent a `T` that overlaps `ImmutableArray<Error>` (such as `object`), nor a `null` success value; the non-union value results accept both. The comparison covers non-null, non-overlapping payloads.

## Benchmarks

Benchmarks live in `tests/ResultWithUnion.Benchmarks/` (`ValueBasic/` and `VoidBasic/`):

| Scenario | Classes |
|----------|---------|
| create success (value / void) | `CreatingValueResultBenchmarks`, `CreatingSuccessVoidResultBenchmarks` |
| create error (value / void) | `CreatingErrorValueResultBenchmarks`, `CreatingErrorVoidResultBenchmarks` |
| errors from each collection type (array, `List`, `ImmutableArray`, `HashSet`, lazy `IEnumerable`) | `Collections/CreatingErrorsFrom<Collection>ValueResultBenchmarks` and `...VoidResultBenchmarks` |
| branch on `if(result.IsSuccess)`, read `Errors` (1 or 5 errors) and `Value`/`GetValue()` | `CheckingIsSuccess...`, `GettingErrors...` (value and void) and `GettingValueResultBenchmarks` |

Closed hierarchies, tuples and exceptions are out of scope.

## Running the benchmarks

```bash
dotnet build
dotnet run -c Release --project tests/ResultWithUnion.Benchmarks -- --filter "*CreatingValueResult*" --job short --inProcess
```

BenchmarkDotNet 0.15.8 does not recognise the net11 runtime, so `--inProcess` is currently required (the GitHub Actions workflow uses it too). See `AGENTS.md` for the full conventions.

## Results

No published results yet. Add them here, from a single machine and run, once the full benchmarks have been executed.
