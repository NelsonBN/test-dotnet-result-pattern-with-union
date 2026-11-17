# ResultUnionCustom: benchmark analysis

Source: [GitHub Actions run 43772005995](https://github.com/NelsonBN/test-dotnet-result-pattern-with-union/actions/runs/43772005995), commit `72f78fc3fb8c41af84de8cc50ae368ebda298c1c`, 32 benchmark classes.

Question: where is `ResultUnionCustom` (the `UnionCustom_*` baseline, Ratio 1.00) slower than the other variants, why, and what could be improved?

## Status checklist

Follow-up reports:

- Change A (PR #3): [benchmark-analysis-union-custom-after-change-a.md](benchmark-analysis-union-custom-after-change-a.md) (CI run 43782547343, ratios are `UnionCustom / Struct` within the same run).
- Change C (PR #2, `ResultUnionCustomExtreme` only): [benchmark-analysis-union-custom-extreme-c.md](benchmark-analysis-union-custom-extreme-c.md) (CI run 43779986524, `UnionCustomExtreme` vs `UnionCustom` within the same run).

Legend: [x] = done or closed with a decision; [ ] = still open.

**Worst cases (section 3)**

- [x] 3.1 `new(ImmutableArray)` 25x gap: fixed (0.99x in CI)
- [x] 3.2 `From(Error)` / implicit `Error` gap (1.5-1.9x): fixed (0.91-1.03x)
- [x] 3.3 `Error[]` / `List<Error>` / `ReadOnlySpan<Error>` factories (1.2-1.3x): fixed (0.91-1.09x)
- [x] 3.4 `HashSet` / lazy `IEnumerable` (below the significance rule): now level (1.03-1.07x)
- [x] Allocations unchanged (equal to the struct variants)

**Implementation issues (section 4)**

- [x] 4.1 Constructor `IsDefault` normalization: replaced by a plain store plus an `_isError` flag (value struct stays 24 B, void struct 8 B to 16 B); mechanism confirmed by the results and by the JIT disassembly (E, section 8)
- [x] 4.3 Double checks on read: the empty fallback now lives only on the read side (`Errors`, `TryGetValue`, `Value`); below the significance rule, no further action
- [x] 4.5 Void `IsSuccess`: reviewed, no change needed (now `!_isError`)
- [x] 4.2 Null check in `Result<T>(T)`: reviewed, closed as no action (cheap, no measurable impact, keeps documented semantics)
- [x] 4.4 `object? Value` boxing risk: measured with the `switch` benchmark (F): no allocation on the consumption path (section 9); a `Match` helper is still not benchmarked

**Improvements (section 5)**

- [x] A Remove the constructor normalization: done as a plain store plus `_isError`; tests first (3 characterization tests), 61/61 tests pass, lint clean, 100% coverage on `Result.cs`
- [x] Comments on `_isError` explaining why it must not be removed
- [x] `AGENTS.md` deviation updated for the flag
- [x] Second report with CI before/after numbers
- [x] B Lazy / branch-free alternative: closed, superseded by A (A reached the same goal and kept the observable behaviour)
- [x] C `AggressiveInlining` on constructors, `From(...)` and implicit operators: **applied to `ResultUnionCustomExtreme` only** (baseline `ResultUnionCustom` untouched); CI: 15 of 40 comparisons improved (-1.4 to -7.5 ns), 24 level, 1 probable-noise regression (`CreatingErrorsFromListVoidResult`), allocations identical; deliberately not applied to `ResultUnionCustom`, which stays the baseline
- [x] D Collapse the double checks: closed, below the significance rule (sub-ns after A), skipped
- [x] E Disassembly: done with `DOTNET_JitDisasm` (BDN `--disasm` does not work with `--inProcess` on net11); constructor confirmed a plain store, and `AggressiveInlining` (C) turned out to change nothing at Tier1 (section 8). The `--job medium` run on a dedicated machine is still not done
- [x] F `switch` benchmark for the `Value` boxing risk: done (`Switching/`, every variant, value and void); `Match` is still planned (no variant has it)
- [ ] Commit the pending docs and `AGENTS.md` changes: waiting for approval

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

On creation `UnionCustom` is faster than `Class` and `RecordClass` everywhere, and faster than or level with the declared `Union` (for example `From(Error)` value: 11.3 ns vs 12.9, 13.7 and 11.1 ns; void: 9.3 ns vs 14.1, 14.4 and 13.3 ns). On reading it is level with the structs (the near-0 ns rows for `Errors` and `TryGetValue(out ImmutableArray)` in this run were JIT-elided work, not a real advantage; see the second report).

## 4. Implementation issues from a performance perspective

All in `src/ResultUnionCustom/Result.cs`.

1. **Constructor `Result(ImmutableArray<Error>)` normalizes the input** (`errors.IsDefault ? ImmutableArray<Error>.Empty : errors`, on both `Result<T>` and `Result`).
   - The other five variants assign the field with no validation (AGENTS.md: "no validation").
   - It adds a branch and a static read of `ImmutableArray<Error>.Empty` on every creation path, and it is the only structural difference between this constructor and the struct constructors.
   - Evidence: the constant 3.4-3.65 ns in 3.1 reappears as the gap in 3.2 and 3.3.
   - Status: **solved by change A** (plain store plus `_isError` flag; CI shows the gap closed). The mechanism is confirmed by the Tier1 disassembly (section 8).
2. **Status: closed, no action.** **Null check in `Result<T>(T value)`** (`if(value is null) { return; }`): cheap and below the rule, but it is a branch the other variants lack and it changes semantics (a null success is read as failure). It does not show in the success benchmarks (0.04 ns), so no action for performance.
3. **Status: closed, below the rule** (after A the empty fallback lives on the read side; sub-ns, see the after-change-A report, section 4). **Double checks on the way out**:
   - `Errors` (value): `IsSuccess` then `_errors.IsDefault`.
   - `TryGetValue(out ImmutableArray<Error>)` (value): `!IsSuccess && !_errors.IsDefault`.
   - After the constructor has normalized, only `default(Result<T>)` can hit the second check. Currently below the rule (0.2 ns), and these read paths are already the fastest in the report.
4. **Status: open (F).** **`object? Value` (the `[Union]` convention member) boxes both cases** (`T` when it is a value type, and the `ImmutableArray<Error>` on failure).
   - Benchmarks use `HasValue`/`TryGetValue` and `IsSuccess`, so it is not measured today.
   - Any consumer that switches through the `Value` property would silently reintroduce the box the custom union exists to avoid. The planned Match/`switch` scenarios must show which members the compiler picks.
5. **Status: closed, no action.** **Void: `IsSuccess`** was `_errors.IsDefault` and is now `!_isError`: still a single field check, level with the structs.

## 5. Possible improvements

### Change A: applied

The constructors of `Result<T>` and `Result` are now plain stores (`_errors = errors; _isError = true;`). The `IsDefault` fallback moved to the read side (`Errors`, `TryGetValue`, `Value`).

Removing the normalization outright was not possible: the void result defines success as `_errors.IsDefault`, and the value result uses it to tell `default(Result<T>)` (no value) from a failure built with a default array. An `_isError` flag now carries that state, so `default` is still "no value" (value) or a success (void) and every existing test stays green. The value struct stays 24 B (the flag fits in padding); the void struct grows from 8 B to 16 B.

Local before/after, `--job short --inProcess`, Release, Windows 11, .NET 11, single run, so noisy (the `Error` column is large); the pass-through and single-error rows are far outside the noise:

| Scenario | Before | After | Struct (after) |
|---|---:|---:|---:|
| `new(ImmutableArray)`, value | 2.84 ns | 0.044 ns | 0.044 ns |
| `new(ImmutableArray)`, void | 2.58 ns | 0.020 ns | 0.033 ns |
| `From(Error)`, value | 9.10 ns | 5.26 ns | 5.28 ns |
| `From(Error)`, void | 6.39 ns | 5.48 ns | 5.21 ns |
| `Error[]`, value | 20.83 ns | 15.02 ns | 13.87 ns |

Allocations are unchanged. The implicit-`Error` rows (7.9 and 9.0 ns) did not move in this local short run, which was noise: the CI run shows them level with `Struct` (1.03x value, 0.91x void). The CI numbers in sections 3 and 4 are from before this change.

### All candidates and their status

| # | Change | Expected effect | Cost / risk | Status |
|---|---|---|---|---|
| A (original idea) | Remove the `IsDefault` normalization from the `ImmutableArray` constructor; keep it only in the getters (`Errors`, `TryGetValue`), exactly like `Struct`. | Removes most of the 3.5 ns; should bring creation level with the structs. | Moves a documented behaviour; AGENTS.md "Intentional deviations" and README must be updated. | **Done** (as plain store plus `_isError`, see above; `ResultUnionCustom`) |
| B | Keep normalization but make it branch-free or lazy (store as is, normalize in `Errors`). | Same as A without changing observable behaviour of `Errors`. | `TryGetValue` must normalize too. | **Closed**, superseded by A |
| C | Add `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to the constructors and `From(...)` if disassembly shows they are not inlined. | Removes call overhead on creation paths. | Only if A/B do not already fix it. | **Done** in `ResultUnionCustomExtreme` only ([report](benchmark-analysis-union-custom-extreme-c.md)) |
| D | Collapse the double checks in `Errors` and `TryGetValue(out ImmutableArray)`. | Sub-ns; only worth it if A is done. | Below the significance rule. | **Closed**, below the rule |
| E | Add `[DisassemblyDiagnoser]` to one creation class and re-run on a dedicated machine (`--job medium`, no CI noise). | Turns the hypothesis into evidence; confirms the 25x is not a measurement artefact. | None. | **Done**, see section 8 (done with `DOTNET_JitDisasm`, not `[DisassemblyDiagnoser]`) |
| F | Add the planned Match/`switch` benchmark for `UnionCustom`. | Verifies whether `Value` boxing leaks into consumption. | New benchmark class for every variant (AGENTS.md: change all or none). | **Done** for `switch`, see section 9; `Match` not written |

**C applied to `ResultUnionCustomExtreme` only** (`AggressiveInlining` on constructors, `From(...)` and implicit operators; `ResultUnionCustom` untouched as baseline). Local smoke run (Ryzen 5 3600X, Windows 11, `--job short --inProcess`, net11 moniker not recognised, so indicative only): `new(ImmutableArray)` 0.80 ns (value) and about 0 ns (void, work eliminated) vs 2.8 ns baseline; `From(Error)` 5.5 ns vs 9.4 ns (value) and 3.9 ns vs 6.5 ns (void); allocations unchanged (32 B). So the call overhead was a large part of the creation gap. A, B, D, E, F still open.

Remaining order: a `--job medium` run on a dedicated machine, then reconsider D. A, C, E and F are done, B is closed.

## 6. Constraints for any follow-up

- [x] Fix with TDD (AGENTS.md): write the failing test that pins the intended `default`/`Empty` behaviour of `UnionCustom` first, then change the constructor.
- [x] Variants must stay near-identical; this change is a deviation of `ResultUnionCustom` only, so it must be listed (or removed from the list) under "Intentional deviations" in AGENTS.md and README.
- [x] Build with zero warnings and re-run the affected benchmarks (`CreatingError*`, `CreatingErrorsFromImmutableArray*`, `Implicit*`) before declaring done.

## 7. Raw numbers

Per-scenario tables are in the run's artifacts (`BenchmarkResults-<Class>`, `benchmark-report-<Class>.md`). Figures above were copied from those tables without rounding beyond two decimals.

## 8. Disassembly evidence (E)

Method: `[DisassemblyDiagnoser]` / `--disasm` cannot be used here. With `--inProcess` on net11 every benchmark fails ("No benchmarks were disassembled", 0 benchmarks executed), and without `--inProcess` BenchmarkDotNet 0.15.8 does not recognise the net11 moniker. The runtime's own JIT dump works with no change to the code:

```powershell
$env:DOTNET_JitStdOutFile = "<path>\jit.txt"
$env:DOTNET_JitDisasm = "*_CreateError *Result*:From *Result*:.ctor"
dotnet run -c Release --project tests/Result.Benchmarks -- --filter "*CreatingErrorValueResultBenchmarks*" --job short --inProcess
```

Read the `(Tier1)` listings only (Tier0 and instrumented Tier0 are also dumped). Local run: Ryzen 5 3600X, Windows 11, .NET 11.0.0, x86-64-v3; indicative only.

Findings (value results):

1. **Change A confirmed.** `Result<T>(ImmutableArray<Error>)` is a plain store in `UnionCustom`: zero the value field, set the flag, store the array. There is no `IsDefault` test and no read of `ImmutableArray<Error>.Empty`. In `CreatingErrorsFromImmutableArray*`, the benchmark method is the same 6 instructions for `Struct`, `ReadonlyStruct`, `RecordStruct`, `UnionCustom` and `UnionCustomExtreme` (the constructor is inlined). The only difference is one store: `mov word ptr [..+8], 1` (`IsSuccess` + `_isError` written together) instead of `mov byte ptr [..+8], 0`. The old 25x gap is gone for that reason.
2. **Change C has no effect at Tier1.** In `UnionCustom` the JIT already inlines `From(Error)` and the constructor into the benchmark method, and the `From` body is instruction for instruction the same as in `UnionCustomExtreme` (only the method-table constant differs). `AggressiveInlining` therefore adds nothing in steady state. The CI gain reported in `benchmark-analysis-union-custom-extreme-c.md` is not explained by inlining at Tier1; likely noise, or tiering effects before Tier1 is reached. In the local run here `UnionCustomExtreme_CreateError` was even slower (8.8 ns vs 5.8 ns, ratio 1.53, `Error` 9.5 ns), which shows how noisy a short run is.
3. **`From(Error)` costs the same as `Struct`.** Same allocation of the 1-element array (`CORINFO_HELP_NEWARR_1_PTR`) and the same two write barriers; the union differs only in the flag store.

Consequences:

- Do not apply C to `ResultUnionCustom` and do not spend more time on inlining attributes: the JIT already does it.
- The earlier claim that C "removes call overhead" (section 5) is not supported by the disassembly; treat the CI numbers as noise until a `--job medium` run on a dedicated machine says otherwise (still open).
- `[DisassemblyDiagnoser]` was not added to any benchmark class: it would make every run, including CI, fail.

## 9. `switch` consumption (F)

New benchmarks: `Switching/SwitchingValueResultBenchmarks` and `Switching/SwitchingVoidResultBenchmarks` (`Success` true and false, all eight variants, baseline `UnionCustom_Switch`). The two unions consume the result with a `switch` expression (`FakeModel value => value.Id`, `ImmutableArray<Error> errors => errors.Length`, `null => ...`); the other variants use the equivalent `TryGetValue` chain, because they have no `Match` and no pattern support. No source file of any variant changed.

Local smoke run (`--job short --inProcess`, Ryzen 5 3600X, Windows 11, .NET 11.0.0; net11 moniker not recognised, one short run, so the `Error` column is large and only the allocation result is solid):

| Result | Success | UnionCustom | Struct | Union (declared) | Allocated |
|---|---|---:|---:|---:|---|
| value | true | 0.90 ns | 0.58 ns | 2.69 ns | 0 B for all |
| value | false | 3.55 ns | 2.64 ns | 1.01 ns | 0 B for all |
| void | true | 5.28 ns | 4.08 ns | 1.34 ns | 0 B for all |
| void | false | 2.82 ns | 2.47 ns | 1.30 ns | 0 B for all |

Findings:

1. **No boxing leaks into consumption.** `UnionCustom_Switch` allocates 0 B in every case. The Tier1 disassembly (`DOTNET_JitDisasm`, see section 8) shows why: the compiler lowers the `switch` onto `TryGetValue(out T)` and `HasValue`, not onto `object? Value`. The only `CORINFO_HELP_NEWSFAST` (a box of the union itself) sits on the `ThrowSwitchExpressionException` path, which is unreachable once every case is covered. The boxing risk in 4.4 is therefore real only for a consumer that reads `Value` directly.
2. **The declared `Union` does not allocate either on consumption**: its boxes were paid at creation (24 B for the error case). Consumption is a type check, so a success is slower (type check plus unbox, 2.7 ns vs 0.9 ns) and a failure is faster (1.0 ns vs 3.6 ns) than `UnionCustom`.
3. **`UnionCustom` is a little slower than the structs** (about 1 ns on value success, 0.9 ns on value failure, 1.2 ns on void success), but inside the run-to-run noise of a short job; below the significance rule. A repeat run after a cosmetic refactor of the benchmark code reversed one row (void, success: `Struct` 6.3 ns vs `UnionCustom` 3.7 ns), so treat every ns gap here as unproven; only the 0 B allocation result held in both runs. In the disassembly, `TryGetValue(out T)` and `HasValue` are real calls (not inlined) in the `switch`, which is the likely reason, and it is the only thing that could still be improved.
4. Not covered: `Match`. No variant has it, so adding it would be an API change for all variants and is left as planned work.
