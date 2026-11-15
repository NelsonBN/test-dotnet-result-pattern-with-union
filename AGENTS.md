# AGENTS.md

Guidance for AI coding assistants working in this repository.

## Project Overview

An experiment, not a library: compares the **Result pattern** built the classic way (class, record class, struct, readonly struct, record struct) against one built on the **C# 15 / .NET 11 `union`**, measured with BenchmarkDotNet for speed and allocations. Question: *does a `union` make a better Result type in performance, allocations and ergonomics?*

- .NET 11 SDK (pinned in `global.json`), C# 15 (default for `net11`; do **not** set `LangVersion`, do **not** add older targets or `RuntimeMoniker` jobs).
- `src/` is the library (`ValueBasic/`, `VoidBasic/`, `Common/`); `tests/ResultWithUnion.Benchmarks/` holds the benchmarks.

> ## HARD RULE: variants differ only in the representation under test
>
> - The **union is the only variant allowed to look different** (it cannot declare fields, a `Value` member or constructors) and it is the **benchmark baseline** (`Union_*`, so `Ratio` reads "variant vs union").
> - The other five variants must be **near-identical**: same fields, members, constructor overloads and order, parameter names, exceptions and messages, nullability, statement style. Diffing any two must show only the type declaration and what the language forces. Start a new variant by copying an existing one.
> - **Same storage and semantics**: `ImmutableArray<Error>` errors, same wrong-state behaviour, no validation.
> - **Same work in benchmarks**: identical inputs and `Error` values for every variant.
> - **Change all or none**: any change to one non-union variant applies to all of them (value and void) in the same change, and to the union where the language allows.
> - **Every deviation is listed** under "Intentional deviations" below; an unlisted one is a bug.
> - Before declaring work done, compare the variants side by side.

## Result Variants

Each variant exists as a value result `Result<Variant><TValue>` in `src/ValueBasic/` and a void result `Result<Variant>` in `src/VoidBasic/`: one type per file, file-scoped namespace `ResultWithUnion.ValueBasic` / `ResultWithUnion.VoidBasic`.

Variants: `ResultStruct`, `ResultReadonlyStruct`, `ResultRecordStruct`, `ResultClass` (sealed), `ResultRecordClass` (sealed), and `ResultUnion` (`union ResultUnion<T>(T, ImmutableArray<Error>)` / `union ResultUnion(ImmutableArray<Error>)`). A second union, `ResultUnionNonBoxing` (hand-written `[Union]` readonly struct with `HasValue`/`TryGetValue`, benchmarked as `UnionNonBoxing_*`), measures the cost of boxing; `Union_*` stays the only baseline.

- Out of scope, do not add: closed hierarchies, tuples, exceptions.
- All variants share `src/Common/Error.cs`.
- API: `IsSuccess`, `Value` (value results), `Errors`; `Value`/`Errors` throw `InvalidOperationException` in the wrong state.
- Class/struct constructors: `(TValue)` / `()` for success, plus `(Error)`, `(ImmutableArray<Error>)` (zero copy), `(Error[])`, `(List<Error>)`, `(IEnumerable<Error>)`, `(params ReadOnlySpan<Error>)`. Dictionaries are unsupported by design. Every overload copies its input once.
- Only the `IEnumerable<Error>` path uses `src/Common/ErrorCollection.cs`; do not duplicate it. All others call `ImmutableArray.Create(...)` directly.
- The union offers `From(...)` static overloads for `Error[]`, `List<Error>`, `IEnumerable<Error>` and `params ReadOnlySpan<Error>`; the value and `ImmutableArray<Error>` convert implicitly; a single `Error` goes through `From(error)`.

### Intentional deviations (the only allowed ones)

- **`ResultUnionNonBoxing`**: hand-written union with fields and a tag; same case types, `GetValue()` and `From(...)` as `ResultUnion`, but nothing is boxed.
- **Union**: exposes `GetValue()` instead of `Value`; created via implicit conversions and `From(...)` instead of constructors; boxes its `ImmutableArray<Error>` case (a 24 B box on every error creation). In the void union, success is `default`, so `default(ResultUnion).IsSuccess` is `true` (the void structs' `default` is a failure).
- **`readonly struct`**: `IsSuccess` is a `readonly` field (no private setter allowed).
- **Record struct / record class**: compiler-generated equality, `ToString` and deconstruction are the variation under test.

### Union pitfalls

- A union lowers to a struct with one `object? Value` field; value-type cases are boxed.
- **Value payload limitation** (`ResultUnion<T>`): `T` must not overlap `ImmutableArray<Error>` (e.g. `object`), because the union cannot tell which case was intended; a `T` of `object` can report `IsSuccess` for an error. A `null` success payload is also read as a failure and `GetValue()` throws, whereas the non-union value results accept it. Value-result comparisons therefore apply to non-null, non-overlapping payloads.
- Always add a `null => ...` arm to `switch` expressions (`default` has null contents).
- Helpers (`IsSuccess`, `Match`, ...) are hand-written; the union adds no equality or deconstruction.
- Runtime types (`UnionAttribute`, `IUnion`) ship with .NET 11, hence net11 only.

## C# 15 / .NET 11 Style

Prefer the newest features; consult the [`csharp-15`](.claude/skills/csharp-15/SKILL.md) skill before writing or reviewing them: unions, collection expression arguments (`[with(capacity: n), ...]`), extension members, labeled `break`/`continue`, `field` keyword, primary constructors, pattern matching, `ArgumentNullException.ThrowIfNull`.

Formatting follows `.editorconfig` (CRLF, 4 spaces). Beyond it: braces always, Allman, no space after control-flow keywords (`if(...)`, `switch(...)`), sorted usings with `System` first, annotate nullability, comment only what needs clarifying.

## Commands

From the repository root:

```bash
dotnet build
dotnet run -c Release --project tests/ResultWithUnion.Benchmarks -- --filter "*CreatingValueResult*" --job short --inProcess
```

- BenchmarkDotNet 0.15.8 does not recognise the net11 moniker: always pass `--inProcess` (also used by CI) and state that caveat next to any numbers.
- Always run benchmarks in `Release` without a debugger; `--job short` is a smoke run, not for published numbers.
- Validate with the [`dotnet-code-health`](.claude/skills/dotnet-code-health/SKILL.md) skill.

## Benchmark Conventions

One class per scenario in `tests/ResultWithUnion.Benchmarks/` (`ValueBasic/`, `VoidBasic/`, plus `Collections/` subfolders), namespace `ResultWithUnion.Benchmarks.Tests`, fakes in `FakeModel.cs`. `Program.cs` uses `BenchmarkSwitcher`, so select scenarios with `--filter`.

- Classes are named `<Scenario>Benchmarks`, methods `<Variant>_<Action>` (`RecordStruct`, `Struct`, `ReadonlyStruct`, `Class`, `RecordClass`, `Union`).
- `[MemoryDiagnoser]` is mandatory; also `[RankColumn]` and `[Orderer(SummaryOrderPolicy.FastestToSlowest)]`.
- Exactly one `[Benchmark(Baseline = true)]` per scenario (per `[BenchmarkCategory]` group), always `Union_*`.
- Return the result (or use a `Consumer`/`Blackhole`) to prevent dead-code elimination; build inputs in `[GlobalSetup]`/`[IterationSetup]`.
- Every class covers all variants; adding a variant means adding its method to every existing class with identical inputs.
- Scenarios: create success (value/void), create error (value/void), errors from each collection type, `IsSuccess` branching on success and failure, reading `Errors` (1 and 5 errors), reading `Value`. **Planned, not yet written:** factory from error-or-value, `Match` and `switch` consumption.

### Reporting results

- Report Mean, Allocated, Gen0 and Ratio (vs union) plus CPU, OS and runtime; compare only within one run on one machine.
- Call out boxing/allocation differences explicitly.
- Record conclusions in `README.md` whenever scenarios or results change.

## Boundaries

- **NEVER** commit without human review; ask first. Commits are atomic Conventional Commits via the [`git-commit`](.claude/skills/git-commit/SKILL.md) skill.
- **ALWAYS** build with zero warnings and run the affected benchmark with `--job short` before declaring work done.
- **NEVER** commit `BenchmarkDotNet.Artifacts/`, `bin/`, `obj/` or secrets.
- Update this file and `README.md` when variants, scenarios, conventions or conclusions change.
