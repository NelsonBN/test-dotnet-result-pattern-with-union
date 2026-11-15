# AGENTS.md

This file provides guidance to AI coding assistants (Claude Code, GitHub Copilot, etc.) when working with code in this repository.


## Project Overview

**test-dotnet-result-pattern-with-union** is an experiment, not a library. It compares the **Result pattern** implemented the classic way (classes, record classes, structs, readonly structs, record structs) against the same pattern built on the **`union` type introduced in C# 15 / .NET 11**, and uses [BenchmarkDotNet](https://benchmarkdotnet.org/) to measure and analyse speed and allocations.

The goal is to answer: *does a C# 15 `union` make a better Result type than the existing approaches, in performance, allocations and ergonomics?*

### Tech Stack

- **SDK**: .NET 11 (`11.0.100`, pinned in `global.json`)
- **Language**: C# 15 (the default for `net11`; do **not** set `LangVersion`)
- **Benchmarking**: BenchmarkDotNet `0.15.8` (only in the benchmarks project)
- **Solution**: `test-dotnet-result-pattern-with-union.slnx`

### Projects

| Project | Type | Purpose |
|---------|------|---------|
| `src/ResultWithUnion.csproj` | Class library, `net11.0`, `Nullable` and `ImplicitUsings` enabled | The Result implementations under test (`ValueBasic/`, `Classic/`, `Union/`, `Common/`) |
| `tests/ResultWithUnion.Benchmarks/ResultWithUnion.Benchmarks.csproj` | Exe, `net11.0`, not packable | BenchmarkDotNet scenarios; references the library |


### Result Variants Being Compared

Every variant has a **value** result (`Result<Variant><TValue>`) and a **void** result (non-generic `Result<Variant>`, file `Result<Variant>Void.cs`).

| Variant | Shape | Folder / types | Status |
|---------|-------|----------------|--------|
| Struct | `struct` | `src/ValueBasic/` `ResultStruct` | Exists |
| Readonly struct | `readonly struct` | `src/ValueBasic/` `ResultReadonlyStruct` | Exists |
| Record struct | `record struct` | `src/ValueBasic/` `ResultRecordStruct` | Exists |
| Class | `sealed class` | `src/Classic/` `ResultClass` | Exists |
| Record class | `sealed record class` | `src/Classic/` `ResultRecordClass` | Exists |
| **Union, reference cases** | `union ResultUnion<T>(OkRef<T>, FailureRef)` (`record class` cases) | `src/Union/` | Exists |
| **Union, value cases** | `union ResultUnionStruct<T>(OkStruct<T>, FailureStruct)` (`readonly record struct` cases, **boxed**) | `src/Union/` | Exists |
| **Union, hand-written** | `[Union] readonly struct ResultUnionNoBox<T> : IUnion` with `HasValue`/`TryGetValue` (**no boxing**) | `src/Union/` | Exists |

- **Out of scope (not tested or benchmarked):** closed hierarchies, tuples and exceptions. Do not add them to the benchmark classes or to this table.
- `src/Common/Error.cs` holds the shared `Error` type (`readonly struct Error { string Description }`). Every variant must use the same `Error` so the comparison is fair.
- All variants store errors as `Error[]` (a single error is `[error]`) so allocation differences come from the representation, not the collection type. `Errors` is exposed as `IReadOnlyList<Error>`.
- Common surface: `IsSuccess`, `Value`, `Errors`, constructors/factories `(TValue)` / `(Error)` / `(Error[])`, a static `Create(TValue? value, Error[]? errors)` (void: `Create(Error[]? errors)`) and `Match(...)`. `Value`/`Errors` throw `InvalidOperationException` in the wrong state. New variants should keep equivalent semantics.
- Intentional deviations (document them, don't hide them):
  - **Unions** cannot declare a member named `Value` (the generated `object? Value` owns it), so they use `GetValue()` / `GetErrors()`.
- New variants go in their own folder under `src/`, one file per type, namespace `ResultWithUnion.<Folder>` (file-scoped).

### Keep in Mind When Analysing Unions

- A `union` lowers to a plain **struct** with a single `object? Value` field: **value-type cases are boxed**. This is the main allocation question for `Ok<T>`/`Error` struct cases, so benchmark reference-type cases (`ResultUnion`), value-type cases (`ResultUnionStruct`) and the hand-written non-boxing union (`ResultUnionNoBox`) side by side.
- Always add a `null => ...` arm to union `switch` expressions: `default(union)` holds null contents and would otherwise throw `SwitchExpressionException`.
- A union adds no equality, deconstruction or members beyond the generated constructors/`Value`; any helper (`IsSuccess`, `Match`, ...) is written by hand in the union body.
- Matching is done with an exhaustive `switch` (no discard arm needed). Compare it against `if (result.IsSuccess)` and `Match(...)` style consumption in the other variants.
- The union runtime types (`UnionAttribute`, `IUnion`) ship with the .NET 11 runtime; the compiler does not synthesize them, which is why this project is **net11 only**.


## Use the Latest .NET 11 / C# 15 Features and Syntax

New code must prefer the newest language and BCL features over legacy forms. Before writing or reviewing any C# 15 feature, use the [`csharp-15`](.claude/skills/csharp-15/SKILL.md) skill (it covers every C# 15 feature and the related diagnostics).

- **Unions**: `public union Result<T>(Ok<T>, Error);` with exhaustive `switch` expressions; prefer them over marker interfaces/abstract bases for the union variant.
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

- **BenchmarkDotNet 0.15.8 does not recognise the net11 runtime moniker**: without `--inProcess` it crashes with `GetRuntimeVersion not implemented for NotRecognized`. Use `--inProcess` (less isolated, so state this caveat next to any numbers) until a BenchmarkDotNet release that supports net11 is referenced.
- **Always run benchmarks in `Release`**, without a debugger attached. Debug results are meaningless.
- BenchmarkDotNet writes reports to `BenchmarkDotNet.Artifacts/` (git-ignored; never commit it).
- Validate a change with the [`dotnet-code-health`](.claude/skills/dotnet-code-health/SKILL.md) skill (`/dotnet-code-health [all|error|warning|info|deadcode|<code>]`): it scans build errors, warnings and analyzer/style findings, fixes them by severity and prints a report.


## Benchmark Conventions

Benchmarks live in `tests/ResultWithUnion.Benchmarks/Tests/`, one class per scenario, with shared fakes in `Fakes/` (for example `FakeModel`).

- **Runtime**: net11 only. The default job runs on the current runtime. Only add an explicit `[SimpleJob(RuntimeMoniker.Net11_0)]` after confirming that moniker exists in the referenced BenchmarkDotNet version.
- **Attributes**: `[MemoryDiagnoser]` is mandatory (allocations are half of this experiment); use `[RankColumn]` and `[Orderer(SummaryOrderPolicy.FastestToSlowest)]` for readable summaries.
- **Baseline**: exactly one `[Benchmark(Baseline = true)]` per scenario, so the `Ratio` column is meaningful. Prefer the simplest/most common implementation as the baseline.
- **Same work for every variant**: identical inputs, identical `Error` instances, identical outcome. Only the Result representation may differ.
- **Prevent dead-code elimination**: return the result from the benchmark method (or consume it with a `Consumer`/`Blackhole`) instead of assigning to an unused local.
- **Setup outside the measured code**: build inputs in `[GlobalSetup]`/`[IterationSetup]`; keep `[Benchmark]` bodies to the operation being measured.
- **Scenarios to cover** for every variant: create success with a value, create success without a value (void), create error (value result), create error (void result), single error vs multiple errors, factory from error-or-value, and **consuming** the result (`IsSuccess`/`switch`/`Match`) on both the success and failure path.
- **Naming**: benchmark classes `<Scenario>Benchmarks`; methods `<Variant>_<Action>`. Variant prefixes: `RecordStruct`, `Struct`, `ReadonlyStruct`, `Class`, `RecordClass`, `UnionRef`, `UnionStruct`, `UnionNoBox`. The baseline is `RecordStruct_*`.
- **Scenario → class map** (every class covers all variants):

  | Scenario | Class |
  |----------|-------|
  | create success with a value | `CreatingValueResultBenchmarks` |
  | create success, void | `CreatingVoidSuccessResultBenchmarks` |
  | create error, value (single error) | `CreatingErrorValueResultBenchmarks` |
  | create errors, value (multiple errors) | `CreatingMultipleErrorsValueResultBenchmarks` |
  | create error, void | `CreatingErrorVoidResultBenchmarks` |
  | factory from error-or-value | `FactoryResultFromErrorOrValueBenchmarks` (two `[BenchmarkCategory]` groups, one baseline each) |
  | consuming success and failure | `ConsumingResultBenchmarks` (`[Params(true, false)] Success`, one baseline per value) |

- **Program.cs**: register classes with `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)` so scenarios are chosen with `--filter` instead of editing commented-out `BenchmarkRunner.Run<...>()` lines.
- When adding a variant, add its method to **every** benchmark class above and keep the inputs identical.

### Analysing and Reporting Results

- Report **Mean**, **Allocated**, **Gen0** (and `Ratio` against the baseline) for each scenario, plus the CPU, OS and runtime reported by BenchmarkDotNet.
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
