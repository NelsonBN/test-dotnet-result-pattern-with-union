# AGENTS.md

This file provides guidance to AI coding assistants (Claude Code, GitHub Copilot, etc.) when working with code in this repository.


## Project Overview

**test-dotnet-result-pattern-with-union** is an experiment, not a library. It compares the **Result pattern** implemented the classic way (classes, record classes, structs, readonly structs, record structs) against the same pattern built on the **`union` type introduced in C# 15 / .NET 11**, and uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure and analyse speed and allocations.

The goal is to answer: *does a C# 15 `union` make a better Result type than the existing approaches, in performance, allocations and ergonomics?*

> ## HARD RULE: keep the variants as equivalent as possible, the union is the reference
>
> The comparison is only valid if the variants differ in **one thing only: the representation under test**. Everything else must be as identical as the language allows. This rule overrides convenience, taste and "small improvements".
>
> - **The union is the only variant allowed to be different.** `union ResultUnion` cannot declare instance fields, a `Value` member or its own constructors, so it is the one implementation that may look different from the rest.
> - **The union is the baseline.** Every benchmark compares the other variants against the `Union_*` method (`[Benchmark(Baseline = true)]`), so the `Ratio` column answers "how does variant X compare to the union?".
> - **All the other variants (`struct`, `readonly struct`, `record struct`, `class`, `record class`) must follow one nearly identical implementation**: the same fields, properties, constructor overloads and their order, parameter names, exception types and messages, nullability annotations and statement style. A reviewer must be able to diff any two of them and see only the type declaration line and what the language forces (for example the `readonly` field in a readonly struct). Start a new variant by copying an existing one.
> - **Same storage and semantics everywhere**, union included wherever the language allows it: errors are an `ImmutableArray<Error>` built the same way (`ImmutableArray.Create(...)`, `ErrorCollection` for `IEnumerable<Error>`), the same wrong-state behaviour, the same "no validation" policy.
> - **Same work in benchmarks**: identical inputs, identical `Error` values and the same operation for every variant (see Benchmark Conventions).
> - **Change all or none**: any change to one non-union variant (a constructor, a member, a rename, a fix) is applied to **all** of them, value and void, in the same change, and to the union when the language allows it. Never leave variants drifting apart.
> - **Every deviation must be listed** under "Intentional deviations" in *Result Variants Being Compared*, with the reason. An unlisted deviation is a bug.
> - Before declaring work done, compare the variants side by side and confirm they are still equivalent.

### Tech Stack

- **SDK**: .NET 11 (`11.0.100`, pinned in `global.json`)
- **Language**: C# 15 (the default for `net11`; do **not** set `LangVersion`)
- **Benchmarking**: BenchmarkDotNet `0.15.8` (only in the benchmarks project)
- **Solution**: `test-dotnet-result-pattern-with-union.slnx`

### Projects

| Project | Type | Purpose |
|---------|------|---------|
| `src/ResultWithUnion.csproj` | Class library, `net11.0`, `Nullable` and `ImplicitUsings` enabled | The Result implementations under test (`ValueBasic/`, `VoidBasic/`, `Common/`) |
| `tests/ResultWithUnion.Benchmarks/ResultWithUnion.Benchmarks.csproj` | Exe, `net11.0`, not packable | BenchmarkDotNet scenarios; references the library |


### Result Variants Being Compared

Each variant exists twice: a **value** result `Result<Variant><TValue>` in `src/ValueBasic/` and a **void** result `Result<Variant>` in `src/VoidBasic/` (same file names, one type per file).

| Variant | Shape | Status |
|---------|-------|--------|
| Struct | `struct ResultStruct` | Exists |
| Readonly struct | `readonly struct ResultReadonlyStruct` | Exists |
| Record struct | `record struct ResultRecordStruct` | Exists |
| Class | `sealed class ResultClass` | Exists |
| Record class | `sealed record class ResultRecordClass` | Exists |
| **C# 15 union** | `union ResultUnion<T>(T, Error, ImmutableArray<Error>)` (value) and `union ResultUnion(Error, ImmutableArray<Error>)` (void) | Exists |

- **Out of scope (not tested or benchmarked):** closed hierarchies, tuples and exceptions. Do not add them.
- `src/Common/Error.cs` holds the shared `Error` type (`readonly struct Error { string Description }`). Every variant must use the same `Error` so the comparison is fair.
- Errors are stored as an **`ImmutableArray<Error>`** (immutable, one reference, no boxing) and exposed as `ImmutableArray<Error> Errors`. Result types expose `IsSuccess`, `Value` (value results) and `Errors`; `Value`/`Errors` throw `InvalidOperationException` in the wrong state. New variants should keep equivalent semantics.
- **Error constructors** (every class/struct variant): `(TValue)` (value results) / `()` (void success), plus `(Error)`, `(ImmutableArray<Error>)` (zero copy), `(Error[])`, `(List<Error>)`, `(IEnumerable<Error>)` (arrays, lists, sets, queues, lazy sequences) and `(params ReadOnlySpan<Error>)`. Dictionaries are not supported by design (they enumerate `KeyValuePair`, not `Error`). Every overload copies the input once, so later changes to the caller's collection never reach the Result.
- There is no input validation. The single-`Error`, `Error[]`, `List<Error>`, `ReadOnlySpan<Error>` and `ImmutableArray<Error>` constructors call `ImmutableArray.Create(...)` directly (`CollectionsMarshal.AsSpan` for a `List`); only the `IEnumerable<Error>` path goes through the internal `src/Common/ErrorCollection.cs` helper (type fast paths, presized copy, `ImmutableArray.CreateRange` for lazy sequences). Do not duplicate it.
- The union exposes the collections through static `From(Error[])`, `From(List<Error>)`, `From(IEnumerable<Error>)` and `From(params ReadOnlySpan<Error>)` overloads in its body; an `ImmutableArray<Error>` converts implicitly. The `ImmutableArray` case is a struct, so it is **boxed** inside the union (an extra allocation the other variants do not have).
- The union mirrors the other variants with hand-written members: `IsSuccess` and `Errors` (same names, same `InvalidOperationException` messages) and `GetValue()` in place of `Value`. A single `Error` (and, for value results, the success value) is created through the generated implicit conversions (`ResultUnion<FakeModel> r = error;`).
- New variants must follow the HARD RULE (copy an existing variant and change only what the variation requires). They go in `src/ValueBasic/` and `src/VoidBasic/`, one file per type, namespace `ResultWithUnion.ValueBasic` / `ResultWithUnion.VoidBasic` (file-scoped).
- **Intentional deviations** (the only ones allowed, see the HARD RULE above):
  - **Union**: a `union` cannot declare instance fields, a member named `Value` or its own constructors. So it exposes `GetValue()` instead of `Value`, is created through the generated implicit conversions and `From(...)` static helpers instead of constructors, and boxes its struct cases (`Error`, `ImmutableArray<Error>`). A single `Error` stays a boxed `Error` case, so `Errors` builds a one-element array on every access (the other variants store it once). In the **void** union a success is the empty union (`default`, null contents), so `default(ResultUnion).IsSuccess` is `true`, while `default` of the void structs is a failure.
  - **`readonly struct`**: `IsSuccess` is a `readonly` field because a readonly struct cannot have a property with a private setter.
  - **Record struct / record class**: the compiler adds value equality, `ToString` and deconstruction. That is the variation under test, not a deviation to remove.

### Keep in Mind When Analysing Unions

- A `union` lowers to a plain **struct** with a single `object? Value` field: **value-type cases are boxed**. This is the main allocation question: `ResultUnion<T>(T, Error)` boxes the `Error` struct (and `T` when it is a value type). When adding union flavours, benchmark reference-type cases (`record class`), value-type cases (`record struct`) and optionally a hand-written non-boxing union (`HasValue`/`TryGetValue`) side by side.
- Always add a `null => ...` arm to union `switch` expressions: `default(union)` holds null contents and would otherwise throw `SwitchExpressionException`.
- A union body cannot declare a member named `Value` (the generated `object? Value` owns it); name hand-written accessors differently (for example `GetValue()` / `GetErrors()`).
- A union adds no equality, deconstruction or members beyond the generated constructors/`Value`; any helper (`IsSuccess`, `Match`, ...) is written by hand in the union body.
- Matching is done with an exhaustive `switch` (no discard arm needed). Compare it against `if (result.IsSuccess)` and `Match(...)` style consumption in the other variants.
- The union runtime types (`UnionAttribute`, `IUnion`) ship with the .NET 11 runtime; the compiler does not synthesize them, which is why this project is **net11 only**.


## Use the Latest .NET 11 / C# 15 Features and Syntax

New code must prefer the newest language and BCL features over legacy forms. Before writing or reviewing any C# 15 feature, use the [`csharp-15`](.claude/skills/csharp-15/SKILL.md) skill (it covers every C# 15 feature and the related diagnostics).

- **Unions**: `public union ResultUnion<T>(T, Error, ImmutableArray<Error>);` with exhaustive `switch` expressions; prefer them over marker interfaces/abstract bases for the union variant.
- **Collection expression arguments**: `[with(capacity: n), ...]` instead of `new List<T>(n) { ... }`; use collection expressions (`[]`, `[a, b]`) everywhere else.
- **Extension members** (including extension indexers) instead of classic `this` extension methods when adding helpers.
- **Labeled `break`/`continue`** instead of `goto` or flag variables in nested loops.
- Also use: file-scoped namespaces, primary constructors, `required`/`init`, pattern matching (`is not`, list/relational/property patterns), `field` keyword in properties, `readonly struct`/`record struct`, `ref`/`in` where it reduces copies, `ArgumentNullException.ThrowIfNull` and friends.
- Do **not** add `<LangVersion>latest</LangVersion>`; target `net11` and let the SDK pick C# 15.
- Do **not** add new `net5.0`-`net10.0` targets or `[SimpleJob(RuntimeMoniker.Net5x/6x/7x/8x)]` attributes.


## Commands

Run from the repository root.

```bash
# Restore + build everything
dotnet build

# Run every benchmark class (interactive switcher)
dotnet run -c Release --project tests/ResultWithUnion.Benchmarks

# Run only matching benchmarks
dotnet run -c Release --project tests/ResultWithUnion.Benchmarks -- --filter "*CreatingValueResult*"

# Quick smoke run (fewer iterations; not for published numbers)
dotnet run -c Release --project tests/ResultWithUnion.Benchmarks -- --filter "*" --job short

# Same, in-process (see the caveat below)
dotnet run -c Release --project tests/ResultWithUnion.Benchmarks -- --filter "*" --job short --inProcess
```

- **BenchmarkDotNet 0.15.8 does not recognise the net11 runtime moniker**: without `--inProcess` it crashes with `GetRuntimeVersion not implemented for NotRecognized`. Use `--inProcess` (less isolated, so state this caveat next to any numbers) until a BenchmarkDotNet release that supports net11 is referenced. The GitHub Actions workflow (`.github/workflows/benchmarks-tests.yml`) already passes `--inProcess` for this reason.
- **Always run benchmarks in `Release`**, without a debugger attached. Debug results are meaningless.
- BenchmarkDotNet writes reports to `BenchmarkDotNet.Artifacts/` (git-ignored; never commit it).
- Validate a change with the [`dotnet-code-health`](.claude/skills/dotnet-code-health/SKILL.md) skill (`/dotnet-code-health [all|error|warning|info|deadcode|<code>]`): it scans build errors, warnings and analyzer/style findings, fixes them by severity and prints a report.


## Benchmark Conventions

Benchmarks live in `tests/ResultWithUnion.Benchmarks/`, one class per scenario, split into `ValueBasic/` (value results) and `VoidBasic/` (void results); all classes use the namespace `ResultWithUnion.Benchmarks.Tests`. Shared fakes live in `FakeModel.cs` (namespace `ResultWithUnion.Benchmarks.Fakes`).

- **Runtime**: net11 only. The default job runs on the current runtime. Only add an explicit `[SimpleJob(RuntimeMoniker.Net11_0)]` after confirming that moniker exists in the referenced BenchmarkDotNet version.
- **Attributes**: `[MemoryDiagnoser]` is mandatory (allocations are half of this experiment); use `[RankColumn]` and `[Orderer(SummaryOrderPolicy.FastestToSlowest)]` for readable summaries.
- **Baseline**: exactly one `[Benchmark(Baseline = true)]` per scenario (per `[BenchmarkCategory]` group), and it is always the **`Union_*`** method (the union is the reference, see the HARD RULE), so the `Ratio` column reads "variant vs union".
- **Same work for every variant** (part of the HARD RULE at the top): identical inputs, identical `Error` instances, identical outcome. Only the Result representation may differ.
- **Prevent dead-code elimination**: return the result from the benchmark method (or consume it with a `Consumer`/`Blackhole`) instead of assigning to an unused local.
- **Setup outside the measured code**: build inputs in `[GlobalSetup]`/`[IterationSetup]`; keep `[Benchmark]` bodies to the operation being measured.
- **Scenarios to cover** for every variant: create success with a value, create success without a value (void), create error (value result), create error (void result), single error vs multiple errors, factory from error-or-value, and **consuming** the result (`IsSuccess`/`switch`/`Match`) on both the success and failure path.
- **Naming**: benchmark classes `<Scenario>Benchmarks`; methods `<Variant>_<Action>`. Variant prefixes: `RecordStruct`, `Struct`, `ReadonlyStruct`, `Class`, `RecordClass`, `Union`. The baseline is `Union_*`.
- **Scenario → class map** (every class covers all variants):

  | Scenario | Class | Status |
  |----------|-------|--------|
  | create success with a value | `ValueBasic/CreatingValueResultBenchmarks` | Exists |
  | create error, value | `ValueBasic/CreatingErrorValueResultBenchmarks` | Exists |
  | create success, void | `VoidBasic/CreatingSuccessVoidResultBenchmarks` | Exists |
  | create error, void | `VoidBasic/CreatingErrorVoidResultBenchmarks` | Exists |
  | errors from each collection type: array, `List`, `ImmutableArray`, `HashSet`, lazy `IEnumerable` (5 errors) | one class per collection and result kind, in `ValueBasic/Collections/` and `VoidBasic/Collections/`: `CreatingErrorsFrom<Collection>ValueResultBenchmarks` / `CreatingErrorsFrom<Collection>VoidResultBenchmarks` (10 classes, `Union_CreateErrors` baseline in each) | Exists |
  | branch on `if(result.IsSuccess)` for a successful and a failed result (`[Params(true, false)] Success`) | `ValueBasic/CheckingIsSuccessValueResultBenchmarks` and `VoidBasic/CheckingIsSuccessVoidResultBenchmarks` | Exists |
  | read `Errors` from a failed result holding 1 or 5 errors (`[Params(1, 5)] ErrorsCount`) | `ValueBasic/GettingErrorsValueResultBenchmarks` and `VoidBasic/GettingErrorsVoidResultBenchmarks` | Exists |
  | read `Value` (union: `GetValue()`) from a successful result | `ValueBasic/GettingValueResultBenchmarks` (value results only, void results have no value) | Exists |
  | factory from error-or-value, `Match` and `switch` consumption | not yet written | Planned |

- **Program.cs**: register classes with `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)` so scenarios are chosen with `--filter` instead of editing commented-out `BenchmarkRunner.Run<...>()` lines.
- When adding a variant, add its method to **every** existing benchmark class and keep the inputs identical.

### Analysing and Reporting Results

- Report **Mean**, **Allocated**, **Gen0** (and `Ratio` against the baseline, which is the union) for each scenario, plus the CPU, OS and runtime reported by BenchmarkDotNet.
- Compare variants within one scenario and one run; do not mix numbers from different machines or runs.
- Call out boxing/allocation differences explicitly (union with struct cases vs reference cases).
- Record conclusions in `README.md` (or a `docs/` file linked from it) whenever scenarios or results change.


## Code Style

Formatting is enforced by `.editorconfig`: UTF-8, 4-space indentation (2 for `csproj`/JSON/YAML/XML/`slnx`), CRLF line endings, `insert_final_newline` for `*.cs`/`*.csproj`.

- File-scoped namespaces (`namespace ResultWithUnion.ValueBasic;`); this is enforced as a warning (`IDE0160`).
- Braces always, Allman style; control-flow keywords without a space before the parenthesis (`if(...)`, `switch(...)`), matching existing files.
- `using` directives sorted, `System` first; unused usings are warnings.
- Expression-bodied methods where it reads naturally; `Nullable` is enabled in the library, so annotate nullability correctly.
- Comment only code that needs clarification.


## Boundaries

- **NEVER** commit automatically without human review. **ALWAYS** ask for review before committing.
- Commits are atomic and follow Conventional Commits; use the [`git-commit`](.claude/skills/git-commit/SKILL.md) skill.
- **ALWAYS** build (`dotnet build`) with zero warnings and run the affected benchmark at least with `--job short` before declaring work done.
- **NEVER** commit `BenchmarkDotNet.Artifacts/`, `bin/` or `obj/`, or secrets of any kind.
- Keep variants behaviourally equivalent; a benchmark that compares unequal work is a bug.
- Update this file and `README.md` when variants, scenarios, conventions or conclusions change.
