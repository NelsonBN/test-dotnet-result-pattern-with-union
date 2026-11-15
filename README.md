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
| C# 15 union, non-boxing | hand-written `[Union]` readonly struct `ResultUnionCustom<T>` / `ResultUnionCustom` with real fields, `HasValue` and `TryGetValue` (benchmarked as `UnionCustom_*`) |

The declared `union` boxes its `ImmutableArray<Error>` case into one `object?` field: with 5 errors it allocates 88 B versus 64 B for the structs (24 B box). A single `Error` is created with `From(error)`. The non-boxing variant exists to measure how much of that gap the boxing explains.

Failed results accept a single `Error` or a collection of errors: `Error[]`, `List<Error>`, `ImmutableArray<Error>`, `ReadOnlySpan<Error>` and any `IEnumerable<Error>` (sets, queues, lazy sequences; dictionaries are not supported). The errors are copied once into an immutable array. Every variant (including both unions) offers the same API: constructors only for the success value and an `ImmutableArray<Error>`, and `From(...)` factories for everything else. Each factory except `IEnumerable<Error>` (C# forbids conversions from an interface) also has an `implicit operator` twin that calls it, so `ResultX r = error;` works on every variant.

**Union limitation:** `ResultUnion<T>` cannot represent a `T` that overlaps `ImmutableArray<Error>` (such as `object`), nor a `null` success value; the non-union value results accept both. The comparison covers non-null, non-overlapping payloads.

## Benchmarks

Benchmarks live in `tests/ResultWithUnion.Benchmarks/`, one folder per scenario with the value and void classes side by side:

| Scenario | Classes |
|----------|---------|
| create success (value / void) | `Creating/CreatingValueResultBenchmarks`, `Creating/CreatingSuccessVoidResultBenchmarks` |
| create error (value / void) | `Creating/CreatingErrorValueResultBenchmarks`, `Creating/CreatingErrorVoidResultBenchmarks` |
| errors from each collection type (array, `List`, `ImmutableArray`, `HashSet`, lazy `IEnumerable`) | `Collections/CreatingErrorsFrom<Collection>ValueResultBenchmarks` and `...VoidResultBenchmarks` |
| implicit conversion from Error, Error[], List<Error>, ReadOnlySpan<Error> (value / void) | `Implicit/ImplicitFrom<Source>ValueResultBenchmarks` and `...VoidResultBenchmarks` |
| non-throwing read with `TryGetValue` (value, errors with 1 and 5; value / void) | `Reading/TryGetting{Value,ErrorsValue,ErrorsVoid}ResultBenchmarks` (the union uses `is` patterns) |
| `HasValue` on success and failure (value / void; the union uses `is not null`) | `Checking/CheckingHasValue{Value,Void}ResultBenchmarks` |
| branch on `if(result.IsSuccess)`, read `Errors` (1 or 5 errors) and `Value`/`GetValue()` | `Checking/CheckingIsSuccess...`, `Reading/GettingErrors...` (value and void) and `Reading/GettingValueResultBenchmarks` |

Closed hierarchies, tuples and exceptions are out of scope.

## Running the benchmarks

```bash
dotnet build
dotnet run -c Release --project tests/ResultWithUnion.Benchmarks -- --filter "*CreatingValueResult*" --job short --inProcess
```

BenchmarkDotNet 0.15.8 does not recognise the net11 runtime, so `--inProcess` is currently required (the GitHub Actions workflow uses it too). See `AGENTS.md` for the full conventions.

## Results

No published results yet. Add them here, from a single machine and run, once the full benchmarks have been executed.
