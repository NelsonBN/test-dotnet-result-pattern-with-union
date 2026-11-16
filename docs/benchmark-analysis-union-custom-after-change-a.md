# ResultUnionCustom: results after change A (PR #3)

Follow-up to [benchmark-analysis-union-custom.md](benchmark-analysis-union-custom.md). That report found that `ResultUnionCustom` was 20-90% slower than the struct variants on every creation path and blamed the `IsDefault` normalization in the `ImmutableArray<Error>` constructor. Change A (PR #3, `worktree-improve-performance`) turned the constructors into plain stores plus an `_isError` flag.

| | Before | After |
|---|---|---|
| CI run | [43772005995](https://github.com/NelsonBN/test-dotnet-result-pattern-with-union/actions/runs/43772005995) | [43782547343](https://github.com/NelsonBN/test-dotnet-result-pattern-with-union/actions/runs/43782547343) |
| Code | `main` + kickoff PR (commit `72f78fc3`) | PR #3, head `cf8eae4` (CI built merge commit `10230f1b`) |
| Runner | `Linux/X64`, AMD EPYC 7763, 2 logical cores, shared | same hardware class, shared |
| Toolchain | .NET 11.0.0, BenchmarkDotNet 0.15.8, in-process | same |

## Verdict

**Improved on every creation path; no allocation change; no meaningful regression.** The effect is large where the original report predicted it, and read paths are level with the structs. Details on one trade-off (previously JIT-elided read benchmarks) are in section 4.

## 1. How the comparison is made

- **Same-run ratios only.** The shared runner drifts between runs: `Struct` itself moved by up to 50% for the same code (for example `Struct` from `Error[]`, value result: 9.9 ns before, 15.5 ns after). Absolute `UnionCustom` times across the two runs are therefore not comparable. The tables use `UnionCustom / Struct` **within each run**; below 1.00 means `UnionCustom` is faster.
- Single run per side, `--job short`-style CI settings: differences under about 10% are inside the noise and are called "level".
- Significance rule from the first report still applies (above ~1 ns and above ~15%).
- `0.00x ns` rows are JIT-elided work, not real speed.

## 2. Creation paths: improved

`UnionCustom` / `Struct`, same run:

| Scenario | Before | After | Change |
|---|---:|---:|---|
| `new(ImmutableArray)`, value | 3.80 ns vs 0.148 ns (**25.7x**) | 0.148 ns vs 0.149 ns (**0.99x**) | fixed |
| `new(ImmutableArray)`, void | 3.48 ns vs 0.001 ns | 0.003 ns vs 0.001 ns | fixed (both elided) |
| `From(Error)`, value | 1.92x (11.3 vs 5.9 ns) | **0.98x** (8.2 vs 8.4 ns) | fixed |
| `From(Error)`, void | 1.49x (9.3 vs 6.2 ns) | **0.98x** (6.3 vs 6.4 ns) | fixed |
| implicit `Error`, value | 1.77x | **1.03x** | fixed |
| implicit `Error`, void | 1.49x | **0.91x** | fixed |
| `Error[]`, value / void | 1.26x / 1.17x | **1.01x / 0.91x** | fixed |
| `List<Error>`, value / void | 1.32x / 1.17x | **0.95x / 1.01x** | fixed |
| implicit `Error[]`, value / void | 1.23x / 1.19x | **1.01x / 1.09x** | fixed / mostly |
| implicit `List<Error>`, value / void | 1.32x / 1.27x | **0.97x / 1.04x** | fixed |
| implicit `ReadOnlySpan<Error>`, value / void | 1.19x / 1.28x | **1.00x / 0.94x** | fixed |
| `HashSet<Error>`, value / void | 1.16x / 1.12x | 1.05x / 1.03x | level |
| `IEnumerable<Error>` (lazy), value / void | 1.15x / 1.09x | 1.03x / 1.07x | level |

Reading the table:

- Every row that was above the significance rule in the first report is now level with the structs (0.9-1.1x). The worst case (`new(ImmutableArray)`, about 3.5 ns of pure overhead) is gone, which confirms the constructor normalization was the cause.
- The only row still above 1.05x is implicit `Error[]` void (1.09x, about 0.6 ns on 7.3 ns), below the significance rule.
- **Allocations are identical before and after in every scenario** (32 B for one error, 64 B for five).

## 3. Read and check paths: level with the structs

| Scenario | UnionCustom / Struct before | after |
|---|---:|---:|
| `IsSuccess`, value (success / failure) | 0.92x / 1.20x | 0.79x / 1.11x |
| `IsSuccess`, void (failure / success) | elided | 0.9x / 0.8x (sub-0.02 ns) |
| `Errors`, value (1 / 5 errors) | 1.25x / 0.98x | 0.98x / 0.61x |
| `Errors`, void (1 / 5 errors) | elided (0.000 ns) | 0.38 ns vs 0.63 ns (**0.60x**) |
| `GetValue`, value | 0.84x | 0.95x |
| `TryGetValue(T)` | 1.34x (1.26 vs 0.94 ns) | 1.05x (0.46 vs 0.44 ns) |

All differences here are below 1 ns, so by the significance rule none of them is actionable. `TryGetValue(T)` stopped being the slowest (it was the 0.3 ns gap listed under "ignored" in the first report).

## 4. One trade-off: read benchmarks that used to be JIT-elided

Some read benchmarks reported about 0 ns for `UnionCustom` in the first report, and the first report read that as "fastest". That was an artefact: the JIT removed the work. After the change the same code does the work, so the numbers moved from about 0 to a real value:

| Scenario | Before | After (UnionCustom vs Struct) |
|---|---:|---:|
| `TryGetValue(out ImmutableArray<Error>)`, value (1 / 5 errors) | 0.003 / 0.004 ns | 3.47 / 3.48 ns vs 3.16 / 3.17 ns (**1.10x**) |
| `TryGetValue(out ImmutableArray<Error>)`, void (1 / 5 errors) | 0.001 / 0.000 ns | 3.57 / 3.57 ns vs 3.28 / 3.28 ns (**1.09x**) |
| `HasValue`, void (failure / success) | 0.019 / 0.003 ns | 0.134 / 0.141 ns vs 0.136 / 0.110 ns (1.0x / 1.28x) |
| `HasValue`, value, success | 0.202 ns | 0.311 ns vs 0.018 ns (sub-ns) |

- This is **not a real slowdown of the code path**; the earlier 0 ns was measurement elision, and the corrected statement is that `TryGetValue(out ImmutableArray)` now costs about the same as the structs (3.5 ns vs 3.2 ns, about 0.3 ns or 10% above them, under the significance rule).
- Two real, small costs come from the change and are worth knowing: reading errors now checks `_isError` and `IsDefault` (the empty fallback moved from write to read), and `HasValue` on a value result is `IsSuccess || _isError` instead of a constant like `Struct`. Both are sub-ns.
- Do not read the first report's "UnionCustom is fastest in `TryGetValue(out ImmutableArray)`" as a real advantage; that claim should be dropped.

## 5. Conclusion

| Question | Answer |
|---|---|
| Did the change improve the results? | **Yes**, clearly, on every creation path (the area the first report flagged). The 25x pass-through gap and the 20-90% gaps on `From(...)`/implicit conversions are closed. |
| Did it change allocations? | No (identical per scenario). |
| Did it cost anything? | Small, sub-ns read-side work (flag plus `IsDefault`), now visible because earlier near-0 rows were JIT-elided. Void struct 8 B to 16 B; value struct unchanged at 24 B. |
| Confidence | Creation-path gains are large and consistent across about 20 scenarios in a second independent CI run; small read differences are within noise. |

## 6. Caveats and next steps

- One CI run per side on a shared runner. For published numbers re-run on a dedicated machine with `--job medium`, and re-run the unchanged `Struct` rows as the control.
- The `ImplicitFromError*` rows now agree with the `From(Error)` rows (1.03x and 0.91x); the local short run mentioned earlier in the first report's section 5 was noise.
- The first report's section 5 text on read paths (and the "fastest in `TryGetValue(out ImmutableArray)`" line in section 3.5) should be corrected to match section 4 here.
- Option E is done (section 8 of the first report): the Tier1 disassembly confirms the constructor is a plain store. Still open from the first report: option F (Match/`switch` benchmark for the `object? Value` boxing risk).
