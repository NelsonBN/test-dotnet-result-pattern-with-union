# AGENTS.md

Guidance for AI coding assistants working in this repository.

## Project Overview

An experiment, not a library: compares the **Result pattern** built the classic way (class, record class, struct, readonly struct, record struct) against one built on the **C# 15 / .NET 11 `union`**, measured with BenchmarkDotNet for speed and allocations. Question: *does a `union` make a better Result type in performance, allocations and ergonomics?*

- .NET 11 SDK (pinned in `global.json`), C# 15 (default for `net11`; do **not** set `LangVersion`, do **not** add older targets or `RuntimeMoniker` jobs).
- `src/` holds one project per variant (`ResultClass/`, `ResultRecordClass/`, `ResultStruct/`, `ResultReadonlyStruct/`, `ResultRecordStruct/`, `ResultUnion/`, `ResultUnionCustom/`, `ResultUnionCustomExtreme/`); `tests/Result.Benchmarks/` is the single benchmark project and references all of them; `tests/<Variant>.Tests/` is one unit-test project per variant with the same three test files (`ResultTests.cs`, `ErrorTests.cs`, `ErrorCollectionTests.cs`), kept as similar as the API allows. There is no shared project.

> ## HARD RULE: variants differ only in the representation under test
>
> - The **declared union is the only variant allowed to look different** (it cannot declare fields, a `Value` member or constructors). The **benchmark baseline** is `UnionCustom_*` (hand-written, non-boxing), so `Ratio` reads "variant vs custom union".
> - The other five variants must be **near-identical**: same fields, members, constructor overloads and order, parameter names, exceptions and messages, nullability, statement style. Diffing any two must show only the type declaration and what the language forces. Start a new variant by copying an existing one.
> - **Most similar API possible across all variants, unions included**: the same constructors (only where the language allows them), the same `From(...)` factory overloads, the same member names, parameter names and signatures. A new member or overload is added to every variant at once; the union differs only where the language forces it (for example `GetValue()` instead of `Value`, implicit conversions instead of constructors). Prefer changing the union's API toward the others, and the others toward the union, over letting them drift apart.
> - **Same storage and semantics**: `ImmutableArray<Error>` errors, same wrong-state behaviour, no validation.
> - **Same work in benchmarks**: identical inputs and `Error` values for every variant.
> - **Change all or none**: any change to one non-union variant applies to all of them (value and void) in the same change, and to the union where the language allows.
> - **Every deviation is listed** under "Intentional deviations" below; an unlisted one is a bug.
> - Before declaring work done, compare the variants side by side.

## Result Variants

Each variant is a project `src/Result<Variant>/Result<Variant>.csproj` with file-scoped namespace `Result<Variant>` and the **same type names in every project**, both in one file in the project root, `Result.cs` (`Result<TValue>` for value, `Result` for void), plus the project's own `Error.cs` and `ErrorCollection.cs`. Benchmarks refer to them by namespace (`ResultStruct.Result<FakeModel>`, `ResultStruct.Error`).

Variants: `ResultStruct`, `ResultReadonlyStruct`, `ResultRecordStruct`, `ResultClass` (sealed), `ResultRecordClass` (sealed), and `ResultUnion` (`union Result<T>(T, ImmutableArray<Error>)` / `union Result(ImmutableArray<Error>)`). A second union, `ResultUnionCustom` (hand-written `[Union]` readonly struct with `HasValue`/`TryGetValue`, benchmarked as `UnionCustom_*`), measures the cost of boxing and is the only baseline (`UnionCustom_*`). `ResultUnionCustomExtreme` is a copy of it for extreme performance experiments (`UnionCustomExtreme_*`, not a baseline).

- Out of scope, do not add: closed hierarchies, tuples, exceptions.
- Every test project csproj (`tests/*`) sets `<IsPackable>false</IsPackable>` and `<Nullable>enable</Nullable>`.
- Every project has its own identical copy of `Error.cs` (namespace aside); the types are distinct, so benchmarks build one `Error` input per variant with identical values. A change to `Error` or `ErrorCollection` goes to all seven copies.
- API: `IsSuccess`, `Value` (value results), `Errors`; `Value`/`Errors` throw `InvalidOperationException` in the wrong state.
- Same API in every variant: constructors `(TValue)` / `()` for success and `(ImmutableArray<Error>)` (zero copy) only; every other failure input goes through static factories `From(Error)`, `From(Error[])`, `From(List<Error>)`, `From(IEnumerable<Error>)`, `From(params ReadOnlySpan<Error>)` (also on both unions). Each factory except `From(IEnumerable<Error>)` has an `implicit operator` twin that only calls it (`Error`, `Error[]`, `List<Error>`, `ReadOnlySpan<Error>`; `params` becomes a plain span), on every variant including both unions. C# forbids user-defined conversions from an interface, so there is no `IEnumerable<Error>` operator anywhere. Non-throwing access follows the `[Union]` compiler convention on every non-declared-union variant: `HasValue`, `TryGetValue(out TValue)` (value results) and `TryGetValue(out ImmutableArray<Error>)`; there is deliberately no `TryGetErrors`, because the compiler only finds `TryGetValue`. Dictionaries are unsupported by design. Every overload copies its input once.
- Only the `IEnumerable<Error>` path uses the project's `ErrorCollection.cs`. All others call `ImmutableArray.Create(...)` directly.
- The union has no constructors; the value and `ImmutableArray<Error>` convert implicitly instead, and it offers the same `From(...)` factories.

### Intentional deviations (the only allowed ones)

- **`ResultUnionCustom`**: hand-written union with fields; same case types, `GetValue()` and `From(...)` as the `ResultUnion` project, but nothing is boxed.
- **`ResultUnionCustomExtreme`**: starts as an exact copy of `ResultUnionCustom` (benchmarked as `UnionCustomExtreme_*`, never the baseline); sandbox for aggressive performance experiments, so it may diverge from `ResultUnionCustom` internals while keeping the same public API. Applied so far: `AggressiveInlining` on constructors, `From(...)` and implicit operators (improvement C of `docs/benchmark-analysis-union-custom.md`).
- **`ResultUnion`**: has no `HasValue`/`TryGetValue`. A declared union lowers patterns (`is T`, `is null`) onto those members when present, so declaring them makes `IsSuccess`/`GetValue()` recurse infinitely (stack overflow); use `is T value` / `is ImmutableArray<Error> errors` instead (the benchmarks do). On default, `ResultUnionCustom.Result<T>` reports no value (`HasValue` false, both `TryGetValue` false), whereas the struct value results treat default as a failure with empty errors (their `HasValue` is therefore always `true`, since they always hold one of the two cases).
- **Union**: exposes `GetValue()` instead of `Value`; created via implicit conversions and `From(...)` instead of constructors; boxes its `ImmutableArray<Error>` case (a 24 B box on every error creation). In the void union, success is `default`, so `default(ResultUnion.Result).IsSuccess` is `true` (the void structs' `default` is a failure).
- **`readonly struct`**: `IsSuccess` is a `readonly` field (no private setter allowed).
- **Record struct / record class**: compiler-generated equality, `ToString` and deconstruction are the variation under test. The synthesized `ToString` reads both `Value` and `Errors`, so it throws for every value result and for a successful void record, by design (accessor guards are not weakened).

### Union pitfalls

- A union lowers to a struct with one `object? Value` field; value-type cases are boxed.
- **Value payload limitation** (`ResultUnion.Result<T>`): `T` must not overlap `ImmutableArray<Error>` (e.g. `object`), because the union cannot tell which case was intended; a `T` of `object` can report `IsSuccess` for an error. A `null` success payload is also read as a failure and `GetValue()` throws, whereas the non-union value results accept it. Value-result comparisons therefore apply to non-null, non-overlapping payloads.
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
dotnet run -c Release --project tests/Result.Benchmarks -- --filter "*CreatingValueResult*" --job short --inProcess
```

- BenchmarkDotNet 0.15.8 does not recognise the net11 moniker: always pass `--inProcess` (also used by CI) and state that caveat next to any numbers.
- Always run benchmarks in `Release` without a debugger; `--job short` is a smoke run, not for published numbers.
- Validate with the [`dotnet-lint`](.claude/skills/dotnet-lint/SKILL.md) skill (on demand: `dotnet run .claude/skills/dotnet-lint/scripts/lint.cs -- --scope changed --reset`; cross-platform, needs only the .NET SDK).
- Tests (if a test project is ever added) follow the [`dotnet-tests`](.claude/skills/dotnet-tests/SKILL.md) skill; its Stop hook (also in `.claude/settings.json`) is a no-op while the repo has no test project, and `DOTNET_TESTS_HOOK=off` disables it. A test project mirrors its source project (`src/<P>/<P>.csproj` has `tests/<P>.Tests/<P>.Tests.csproj`, web projects also `tests/<P>.IntegrationTests/`; `src/<P>/<Folder>/<Name>.cs` has `tests/<P>.Tests/<Folder>/<Name>Tests.cs`).
- A `Stop` hook in [`.claude/settings.json`](.claude/settings.json) (read by Claude Code, VS Code and the Copilot CLI) runs that scan whenever code changed and blocks ending the turn while the lines that differ from `HEAD` have any finding (the hook never checks other code) (after 3 consecutive blocks it lets the agent stop with a warning). `DOTNET_LINT_HOOK=off` disables it.

## Benchmark Conventions

One class per scenario in `tests/Result.Benchmarks/` grouped in one folder per scenario (`Creating/`, `Collections/`, `Reading/`, `Checking/`), value and void side by side, namespace `Result.Benchmarks.Tests`, fakes in `FakeModel.cs`. `Program.cs` uses `BenchmarkSwitcher`, so select scenarios with `--filter`.

- Classes are named `<Scenario>Benchmarks`, methods `<Variant>_<Action>` (`RecordStruct`, `Struct`, `ReadonlyStruct`, `Class`, `RecordClass`, `Union`).
- `[MemoryDiagnoser]` is mandatory; also `[RankColumn]` and `[Orderer(SummaryOrderPolicy.FastestToSlowest)]`.
- Exactly one `[Benchmark(Baseline = true)]` per scenario (per `[BenchmarkCategory]` group), always `UnionCustom_*`.
- Return the result (or use a `Consumer`/`Blackhole`) to prevent dead-code elimination; build inputs in `[GlobalSetup]`/`[IterationSetup]`.
- Every class covers all variants; adding a variant means adding its method to every existing class with identical inputs.
- Scenarios: create success (value/void), create error (value/void), errors from each collection type, implicit conversion from `Error`/`Error[]`/`List<Error>`/`ReadOnlySpan<Error>` (`Implicit/`), `HasValue`/`TryGetValue` (`Checking/`, `Reading/`), `IsSuccess` branching on success and failure, reading `Errors` (1 and 5 errors), reading `Value`. **Planned, not yet written:** factory from error-or-value, `Match` and `switch` consumption.

### Reporting results

- Report Mean, Allocated, Gen0 and Ratio (vs custom union) plus CPU, OS and runtime; compare only within one run on one machine.
- Call out boxing/allocation differences explicitly.
- Record conclusions in `README.md` whenever scenarios or results change.

## Boundaries

- **NEVER** commit without human review; ask first. Commits are atomic Conventional Commits via the [`git-commit`](.claude/skills/git-commit/SKILL.md) skill.
- **ALWAYS** build with zero warnings and run the affected benchmark with `--job short` before declaring work done.
- **NEVER** commit `BenchmarkDotNet.Artifacts/`, `bin/`, `obj/` or secrets.
- **ALWAYS** test first (TDD) when an AI fixes or changes anything, whatever the source (ticket, review comment, instruction, debug finding): write the test that reproduces it, watch it fail (red), fix, watch the same test pass (green), then refactor. Never fix first and test afterwards; if it cannot be reproduced with a test, stop and ask. See the [`dotnet-tests`](.claude/skills/dotnet-tests/references/tdd-bug-workflow.md) skill.
- Update this file and `README.md` when variants, scenarios, conventions or conclusions change.
