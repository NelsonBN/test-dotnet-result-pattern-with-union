# test-dotnet-result-pattern-with-union

An experiment, not a library: the **Result pattern** implemented the classic way versus built on the **C# 15 `union`** type (.NET 11), measured with [BenchmarkDotNet](https://benchmarkdotnet.org/) for speed and allocations.

The question: *does a C# 15 `union` make a better Result type than the existing approaches, in performance, allocations and ergonomics?*

## Fair comparison

The non-union variants (struct, readonly struct, record struct, class, record class) follow one nearly identical implementation: the same members, constructor overloads, storage (`ImmutableArray<Error>`), exceptions and benchmark inputs. The **union is the only variant allowed to differ** (the language does not let it declare fields, `Value` or constructors), while the hand-written non-boxing `ResultUnionCustom` project is the **baseline** of every benchmark, so `Ratio` reads "variant vs custom union". The unavoidable differences are listed in `AGENTS.md` under *Intentional deviations*.

## Variants

Each variant is its own project `src/Result<Variant>/` (namespace `Result<Variant>`) with the same type names in every project, both in one file in the project root, `Result.cs`: `Result<TValue>` (value) and `Result` (void). Each project also carries its own copy of `Error` and `ErrorCollection` (no shared project), so `ResultStruct.Error` and `ResultClass.Error` are different types.

| Variant | Types |
|---------|-------|
| struct, readonly struct, record struct | `ResultStruct`, `ResultReadonlyStruct`, `ResultRecordStruct` |
| sealed class, sealed record class | `ResultClass`, `ResultRecordClass` |
| C# 15 union | `union Result<T>(T, ImmutableArray<Error>)` / `union Result(ImmutableArray<Error>)` |
| C# 15 union, non-boxing | hand-written `[Union]` readonly struct `Result<T>` / `Result` in `ResultUnionCustom` with real fields, `HasValue` and `TryGetValue` (benchmarked as `UnionCustom_*`) |

The declared `union` boxes its `ImmutableArray<Error>` case into one `object?` field: with 5 errors it allocates 88 B versus 64 B for the structs (24 B box). A single `Error` is created with `From(error)`. The non-boxing variant exists to measure how much of that gap the boxing explains.

Failed results accept a single `Error` or a collection of errors: `Error[]`, `List<Error>`, `ImmutableArray<Error>`, `ReadOnlySpan<Error>` and any `IEnumerable<Error>` (sets, queues, lazy sequences; dictionaries are not supported). The errors are copied once into an immutable array. Every variant (including both unions) offers the same API: constructors only for the success value and an `ImmutableArray<Error>`, and `From(...)` factories for everything else. Each factory except `IEnumerable<Error>` (C# forbids conversions from an interface) also has an `implicit operator` twin that calls it, so `ResultX r = error;` works on every variant.

**Union limitation:** `ResultUnion.Result<T>` cannot represent a `T` that overlaps `ImmutableArray<Error>` (such as `object`), nor a `null` success value; the non-union value results accept both. The comparison covers non-null, non-overlapping payloads.

## Benchmarks

Benchmarks live in the single project `tests/Result.Benchmarks/` (it references every variant project and builds one `Error` input per variant), one folder per scenario with the value and void classes side by side:

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

## Unit tests

Each variant has its own test project, `tests/<Variant>.Tests/` (xunit v3, AwesomeAssertions, coverlet.MTP), with the same three files: `ResultTests.cs`, `ErrorTests.cs`, `ErrorCollectionTests.cs`. The five non-union suites are near-identical (record variants add equality and `ToString` tests; the struct variants add `default` tests); the two union suites differ only where the API does (`GetValue()`, no constructors, no `HasValue`/`TryGetValue` on the declared union).

```bash
dotnet test --solution test-dotnet-result-pattern-with-union.slnx
dotnet run .claude/skills/dotnet-tests/scripts/tests.cs -- coverage
dotnet run .claude/skills/dotnet-tests/scripts/tests.cs -- mutation
```

The tests are characterization tests of existing behaviour; Stryker.NET mutation runs prove they can fail (about 97% mutation score for the five non-union variants, about 95% for `ResultUnionCustom`). Line and branch coverage is 100% for every source file. Stryker.NET 5.1.0 (the newest release) cannot mutate `ResultUnion`: its bundled Roslyn does not parse the `union` declaration (it reads the members as top-level statements, the mutated copy fails to compile and Stryker crashes in its rollback code with `ArgumentOutOfRangeException`). That project is therefore ignored for mutation testing for now (`mutationIgnore` in `.config/dotnet-tests.json`, skipped in `mutation-tests.bat`/`.sh`); it is covered by the 100% line and branch gate only. Revisit when Stryker supports C# 15 unions.

## Running the benchmarks

```bash
dotnet build
dotnet run -c Release --project tests/Result.Benchmarks -- --filter "*CreatingValueResult*" --job short --inProcess
```

BenchmarkDotNet 0.15.8 does not recognise the net11 runtime, so `--inProcess` is currently required (the GitHub Actions workflow uses it too). See `AGENTS.md` for the full conventions.

## Results

No published results yet. Add them here, from a single machine and run, once the full benchmarks have been executed.
