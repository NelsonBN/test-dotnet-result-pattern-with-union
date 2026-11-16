# ResultUnionCustom: benchmark analysis

Source: [GitHub Actions run 43772005995](https://github.com/NelsonBN/test-dotnet-result-pattern-with-union/actions/runs/43772005995), commit `72f78fc3fb8c41af84de8cc50ae368ebda298c1c`, 32 benchmark classes.

Question: where is `ResultUnionCustom` (the `UnionCustom_*` baseline, Ratio 1.00) slower than the other variants, why, and what could be improved?

## 1. Setup and caveats

| Item | Value |
|---|---|
| Runner | `Linux/X64`, AMD EPYC 7763 3.25GHz, 2 logical / 1 physical core (shared GitHub-hosted runner) |
| Runtime | .NET SDK 11.0.100, .NET 11.0.0, RyuJIT x86-64-v3 |
| Harness | BenchmarkDotNet 0.15.8, `InProcessEmitToolchain` (net11 is not recognised, so isolation is reduced), `--job short`-style CI run |

- Shared CI hardware: numbers are indicative; compare only within one table.
- Rows reading `0.0000 ns` (RecordStruct on `ImmutableArray` creation, `TryGetValue(T)`, `IsSuccess`/`HasValue` on failure; void `CreateSuccess`, void `Errors`) mean the JIT removed the work. They are not real speed and give no usable ratio.
- A `Ratio` below 1.00 means the variant is **faster** than `UnionCustom`.

## 2. Significance rule

A difference counts only if the gap is **above ~1 ns absolute and above ~15% relative**, and larger than the reported Error/StdDev. Everything else is ignored for now.

Ignored as insignificant (all at or below ~1 ns, mostly 0.1-0.3 ns, and `UnionCustom` is often the best of them):

- `IsSuccess` (value and void), `HasValue`, `GetValue`/`Errors` getters: 0.00-1.0 ns across all variants.
- `TryGetValue(T)`: `UnionCustom` 1.26 ns vs 0.94-0.98 ns for the structs and classes (about 0.3 ns).
- `TryGetValue(out ImmutableArray<Error>)`: `UnionCustom` is the fastest (about 0.003 ns vs 2.3-3.2 ns).
- Creating a success result: all at or below 0.3 ns, except the class variants (4.4-6.5 ns, 32-40 B).

## 3. Worst cases for ResultUnionCustom

`UnionCustom` is never the slowest, and it allocates exactly like the struct variants. It loses only to the three struct variants (`Struct`, `ReadonlyStruct`, `RecordStruct`) and only on the **creation** paths. Ranked by loss:

### 3.1 Wrapping an existing `ImmutableArray<Error>` (the constructor) — worst case

| Scenario | UnionCustom | Struct | ReadonlyStruct | Gap |
|---|---:|---:|---:|---:|
| Value, `new(ImmutableArray)` | 3.80 ns | 0.148 ns | 0.228 ns | about 25x (+3.65 ns) |
| Void, `new(ImmutableArray)` | 3.48 ns | 0.001 ns | 0.088 ns | about 40x (+3.4 ns) |

The other variants only store a field here. `UnionCustom` pays about 3.5 ns for what should be a copy. The same constant shows up in every other creation path below, so this is the root cause.

### 3.2 `From(Error)` and the implicit `Error` conversion (single error)

| Scenario | UnionCustom | Struct | RecordStruct | Struct ratio |
|---|---:|---:|---:|---:|
| `From(Error)`, value | 11.29 ns | 5.87 ns | 5.41 ns | 0.52 |
| `From(Error)`, void | 9.27 ns | 6.23 ns | 6.03 ns | 0.67 |
| implicit `Error`, value | 13.65 ns | 7.72 ns | 6.93 ns | 0.57 |
| implicit `Error`, void | 7.73 ns | 5.19 ns | 4.91 ns | 0.67 |

Gap of 2.5-5.9 ns on an operation of 5-14 ns: the largest relative loss on a realistic API.

### 3.3 Factories from `Error[]`, `List<Error>`, `ReadOnlySpan<Error>` (5 errors)

| Scenario | UnionCustom | Best struct | Ratio |
|---|---:|---:|---:|
| `Error[]`, value | 12.42 ns | 9.87 ns (Struct) | 0.79 |
| `Error[]`, void | 13.30 ns | 10.94 ns (ReadonlyStruct) | 0.82 |
| `List<Error>`, value | 22.19 ns | 16.54 ns (RecordStruct) | 0.75 |
| `List<Error>`, void | 13.67 ns | 9.65 ns (RecordStruct) | 0.71 |
| implicit `Error[]`, value | 11.14 ns | 8.70 ns (ReadonlyStruct) | 0.78 |
| implicit `Error[]`, void | 19.25 ns | 15.01 ns (RecordStruct) | 0.78 |
| implicit `List<Error>`, value | 21.42 ns | 16.12 ns (ReadonlyStruct) | 0.75 |
| implicit `List<Error>`, void | 21.35 ns | 16.79 ns (Struct) | 0.79 |
| implicit `ReadOnlySpan<Error>`, value | 22.03 ns | 17.45 ns (ReadonlyStruct) | 0.79 |
| implicit `ReadOnlySpan<Error>`, void | 19.24 ns | 12.21 ns (RecordStruct) | 0.63 |

A steady 2.5-7 ns (about 20-35%) behind the structs. It matches the constant from 3.1 plus noise, so it is the same cause, not a separate defect.

### 3.4 Below the rule (listed for completeness, not worth acting on)

- `IEnumerable<Error>`: lazy sequence 30.6 vs 26.6 ns (value) and 34.2 vs 31.5 ns (void); `HashSet<Error>` 21.6 vs 18.5 ns (value) and 24.6 vs 21.9 ns (void). The gap is 3-4 ns but under 15% (8-14%) because enumeration dominates.
- Allocations: identical to the structs in every scenario (32 B for one error, 64 B for five). `UnionCustom` allocates **less** than `Class`/`RecordClass` (72-104 B) and the declared `Union` (56-88 B, the extra 24 B is the boxed `ImmutableArray`).

### 3.5 Not a weakness

On creation `UnionCustom` is faster than `Class` and `RecordClass` everywhere, and faster than or level with the declared `Union` (for example `From(Error)` value: 11.3 ns vs 12.9, 13.7 and 11.1 ns; void: 9.3 ns vs 14.1, 14.4 and 13.3 ns). On reading it is the fastest or tied in `Errors` and `TryGetValue(out ImmutableArray)`.

## 4. Implementation issues from a performance perspective

All in `src/ResultUnionCustom/Result.cs`.

1. **Constructor `Result(ImmutableArray<Error>)` normalizes the input** (`errors.IsDefault ? ImmutableArray<Error>.Empty : errors`, on both `Result<T>` and `Result`).
   - The other five variants assign the field with no validation (AGENTS.md: "no validation").
   - It adds a branch and a static read of `ImmutableArray<Error>.Empty` on every creation path, and it is the only structural difference between this constructor and the struct constructors.
   - Evidence: the constant 3.4-3.65 ns in 3.1 reappears as the gap in 3.2 and 3.3.
   - Status: likeliest cause, **not yet proven**. Confirm with `[DisassemblyDiagnoser]` (is the constructor inlined, is the struct copied through the stack, is there a static-init helper call).
2. **Null check in `Result<T>(T value)`** (`if(value is null) { return; }`): cheap and below the rule, but it is a branch the other variants lack and it changes semantics (a null success is read as failure). It does not show in the success benchmarks (0.04 ns), so no action for performance.
3. **Double checks on the way out**:
   - `Errors` (value): `IsSuccess` then `_errors.IsDefault`.
   - `TryGetValue(out ImmutableArray<Error>)` (value): `!IsSuccess && !_errors.IsDefault`.
   - After the constructor has normalized, only `default(Result<T>)` can hit the second check. Currently below the rule (0.2 ns), and these read paths are already the fastest in the report.
4. **`object? Value` (the `[Union]` convention member) boxes both cases** (`T` when it is a value type, and the `ImmutableArray<Error>` on failure).
   - Benchmarks use `HasValue`/`TryGetValue` and `IsSuccess`, so it is not measured today.
   - Any consumer that switches through the `Value` property would silently reintroduce the box the custom union exists to avoid. The planned Match/`switch` scenarios must show which members the compiler picks.
5. **Void: `IsSuccess => _errors.IsDefault`** is a single field check. It is the strongest read path in the report and needs no change.

## 5. Possible improvements (experiments, not applied)

| # | Change | Expected effect | Cost / risk |
|---|---|---|---|
| A | Remove the `IsDefault` normalization from the `ImmutableArray` constructor; keep it only in the getters (`Errors`, `TryGetValue`), exactly like `Struct`. | Removes most of the 3.5 ns; should bring creation level with the structs. | Moves a documented behaviour; AGENTS.md "Intentional deviations" and README must be updated. |
| B | Keep normalization but make it branch-free or lazy (store as is, normalize in `Errors`). | Same as A without changing observable behaviour of `Errors`. | `TryGetValue` must normalize too. |
| C | Add `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to the constructors and `From(...)` if disassembly shows they are not inlined. | Removes call overhead on creation paths. | Only if A/B do not already fix it. |
| D | Collapse the double checks in `Errors` and `TryGetValue(out ImmutableArray)`. | Sub-ns; only worth it if A is done. | Below the significance rule. |
| E | Add `[DisassemblyDiagnoser]` to one creation class and re-run on a dedicated machine (`--job medium`, no CI noise). | Turns the hypothesis into evidence; confirms the 25x is not a measurement artefact. | None. |
| F | Add the planned Match/`switch` benchmark for `UnionCustom`. | Verifies whether `Value` boxing leaks into consumption. | New benchmark class for every variant (AGENTS.md: change all or none). |

Suggested order: E first (cheap, decides everything), then A or B, then re-run the creation scenarios and compare against the numbers in this report.

## 6. Constraints for any follow-up

- Fix with TDD (AGENTS.md): write the failing test that pins the intended `default`/`Empty` behaviour of `UnionCustom` first, then change the constructor.
- Variants must stay near-identical; this change is a deviation of `ResultUnionCustom` only, so it must be listed (or removed from the list) under "Intentional deviations" in AGENTS.md and README.
- Build with zero warnings and re-run the affected benchmarks (`CreatingError*`, `CreatingErrorsFromImmutableArray*`, `Implicit*`) before declaring done.

## 7. Raw numbers

Per-scenario tables are in the run's artifacts (`BenchmarkResults-<Class>`, `benchmark-report-<Class>.md`). Figures above were copied from those tables without rounding beyond two decimals.
