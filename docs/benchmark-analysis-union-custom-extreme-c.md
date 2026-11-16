# ResultUnionCustomExtreme (improvement C): CI benchmark results

Source: PR [#2](https://github.com/NelsonBN/test-dotnet-result-pattern-with-union/pull/2), workflow run [43779986524](https://github.com/NelsonBN/test-dotnet-result-pattern-with-union/actions/runs/43779986524) ("Benchmarks Tests") on head commit `77cc366` (`feat(result): apply aggressive inlining optimizations to ResultUnionCustomExtreme`). 32 benchmark classes, 40 `UnionCustomExtreme_*` vs `UnionCustom_*` comparisons, read from the benchmark comments the workflow posted on the PR.

Question: did change **C** of [`benchmark-analysis-union-custom.md`](benchmark-analysis-union-custom.md) (`[MethodImpl(MethodImplOptions.AggressiveInlining)]` on the constructors, `From(...)` factories and implicit operators, applied to `ResultUnionCustomExtreme` only) improve the results, or make no difference?

## 1. Verdict

**Improved, on the creation paths. No difference elsewhere. Allocations are identical.**

- **15 of 40** comparisons improved by more than the significance rule (above ~1 ns and above ~15%): every creation path from a single error, an array, a `List` (value), a span, a lazy sequence (value) and the implicit conversions, typically **-1.4 to -7.5 ns, -15% to -38%**. `HashSet` stays under the rule (value -3.65 ns but -13%).
- **24 of 40** show no significant difference: all reads and checks (`IsSuccess`, `HasValue`, `GetValue`, `Errors`, `TryGetValue`), success creation, and the slow lazy `IEnumerable` void path. These members were not changed, so a flat result is the expected one.
- **1 of 40** regressed: `CreatingErrorsFromListVoidResult` 13.19 ns to 16.70 ns (+27%). See 4.
- **Allocated bytes are identical** in every scenario (32 B for one error, 64 B for five). Inlining does not change allocations.
- The largest item of the analysis, wrapping an existing `ImmutableArray<Error>` (3.3-3.6 ns for `UnionCustom`), dropped to about 0 ns (void, 0.004 ns) and 0.35 ns (value). The void value is the JIT removing the work, so it is a lower bound, not a real cost.

## 2. Setup and caveats

| Item | Value |
|---|---|
| Runner | `Linux/X64`, AMD EPYC 7763 3.24GHz, 1 CPU, 2 logical / 1 physical core (shared GitHub-hosted runner) |
| Runtime | .NET SDK 11.0.100, .NET 11.0.0, RyuJIT x86-64-v3 |
| Harness | BenchmarkDotNet 0.15.8, `InProcessEmitToolchain` (net11 is not recognised, so isolation is reduced) |

- Shared CI hardware: the numbers are indicative, one run per class, no repeated runs. Both variants ran in the same job on the same machine, so the **within-class comparison is fair**; absolute values are not comparable with other runs.
- Rule used (same as the previous report): a difference counts only above ~1 ns **and** ~15%. The Error/StdDev columns were not used to filter, so borderline cases near the thresholds could flip on a re-run.
- Rows below ~0.01 ns mean the JIT removed the work and give no usable ratio.
- Diff is `UnionCustomExtreme - UnionCustom`: negative means the extreme variant is faster.

## 3. Results

### Improved (15)

| Scenario | UnionCustom | UnionCustomExtreme | Diff | Diff % | Allocated (base / extreme) |
|---|---:|---:|---:|---:|---|
| CreatingErrorsFromArrayValueResult | 21.88 ns | 16.14 ns | -5.74 ns | -26% | 64 B / 64 B |
| CreatingErrorsFromArrayVoidResult | 8.821 ns | 7.466 ns | -1.35 ns | -15% | 64 B / 64 B |
| CreatingErrorsFromLazyValueResult | 39.53 ns | 32.01 ns | -7.52 ns | -19% | 64 B / 64 B |
| CreatingErrorsFromImmutableArrayVoidResult | 3.2906 ns | 0.0037 ns | -3.29 ns | -100% | - / - |
| CreatingErrorsFromImmutableArrayValueResult | 3.5747 ns | 0.3492 ns | -3.23 ns | -90% | - / - |
| CreatingErrorsFromListValueResult | 20.79 ns | 14.24 ns | -6.55 ns | -32% | 64 B / 64 B |
| CreatingErrorValueResult | 10.445 ns | 6.715 ns | -3.73 ns | -36% | 32 B / 32 B |
| CreatingErrorVoidResult | 5.526 ns | 3.583 ns | -1.94 ns | -35% | 32 B / 32 B |
| ImplicitFromArrayValueResult | 23.73 ns | 19.23 ns | -4.50 ns | -19% | 64 B / 64 B |
| ImplicitFromErrorValueResult | 15.210 ns | 9.467 ns | -5.74 ns | -38% | 32 B / 32 B |
| ImplicitFromErrorVoidResult | 9.396 ns | 6.254 ns | -3.14 ns | -33% | 32 B / 32 B |
| ImplicitFromListValueResult | 20.08 ns | 14.81 ns | -5.27 ns | -26% | 64 B / 64 B |
| ImplicitFromListVoidResult | 12.74 ns | 10.15 ns | -2.59 ns | -20% | 64 B / 64 B |
| ImplicitFromSpanVoidResult | 13.22 ns | 10.78 ns | -2.44 ns | -18% | 64 B / 64 B |
| ImplicitFromSpanValueResult | 21.17 ns | 16.95 ns | -4.22 ns | -20% | 64 B / 64 B |

### Regressed (1)

| Scenario | UnionCustom | UnionCustomExtreme | Diff | Diff % | Allocated (base / extreme) |
|---|---:|---:|---:|---:|---|
| CreatingErrorsFromListVoidResult | 13.19 ns | 16.70 ns | +3.51 ns | +27% | 64 B / 64 B |

### No significant difference (24)

| Scenario | UnionCustom | UnionCustomExtreme | Diff | Diff % | Allocated (base / extreme) |
|---|---:|---:|---:|---:|---|
| CheckingIsSuccessVoidResult [False] | 0.1322 ns | 0.1267 ns | -0.01 ns | -4% | - / - |
| CheckingIsSuccessVoidResult [True] | 0.1573 ns | 0.1223 ns | -0.03 ns | -22% | - / - |
| CheckingHasValueValueResult [False] | 0.0216 ns | 0.0210 ns | -0.00 ns | -3% | - / - |
| CheckingHasValueValueResult [True] | 0.0092 ns | 0.0086 ns | -0.00 ns | -7% | - / - |
| CheckingHasValueVoidResult [False] | 0.1362 ns | 0.1377 ns | +0.00 ns | +1% | - / - |
| CheckingHasValueVoidResult [True] | 0.1370 ns | 0.1423 ns | +0.01 ns | +4% | - / - |
| CheckingIsSuccessValueResult [False] | 0.1460 ns | 0.1316 ns | -0.01 ns | -10% | - / - |
| CheckingIsSuccessValueResult [True] | 0.1430 ns | 0.1395 ns | -0.00 ns | -2% | - / - |
| CreatingErrorsFromHashSetVoidResult | 15.87 ns | 13.76 ns | -2.11 ns | -13% | 64 B / 64 B |
| CreatingErrorsFromHashSetValueResult | 27.24 ns | 23.59 ns | -3.65 ns | -13% | 64 B / 64 B |
| CreatingErrorsFromLazyVoidResult | 129.6 ns | 126.3 ns | -3.30 ns | -3% | 64 B / 64 B |
| CreatingValueResult | 0.0399 ns | 0.0391 ns | -0.00 ns | -2% | - / - |
| ImplicitFromArrayVoidResult | 17.27 ns | 14.96 ns | -2.31 ns | -13% | 64 B / 64 B |
| CreatingSuccessVoidResult | 0.0044 ns | 0.2252 ns | +0.22 ns | +5018% | - / - |
| GettingErrorsValueResult [1] | 0.3203 ns | 0.5404 ns | +0.22 ns | +69% | - / - |
| GettingErrorsValueResult [5] | 0.3511 ns | 0.5553 ns | +0.20 ns | +58% | - / - |
| GettingValueResult | 0.9623 ns | 0.9788 ns | +0.02 ns | +2% | - / - |
| GettingErrorsVoidResult [1] | 0.0040 ns | 0.0000 ns | -0.00 ns | -100% | - / - |
| GettingErrorsVoidResult [5] | 0.0043 ns | 0.0012 ns | -0.00 ns | -72% | - / - |
| TryGettingErrorsValueResult [1] | 0.0026 ns | 0.0040 ns | +0.00 ns | +54% | - / - |
| TryGettingErrorsValueResult [5] | 0.0039 ns | 0.0024 ns | -0.00 ns | -38% | - / - |
| TryGettingValueResult | 0.5615 ns | 0.4984 ns | -0.06 ns | -11% | - / - |
| TryGettingErrorsVoidResult [1] | 0.0006 ns | 0.0008 ns | +0.00 ns | +33% | - / - |
| TryGettingErrorsVoidResult [5] | 0.0124 ns | 0.0007 ns | -0.01 ns | -94% | - / - |

## 4. The one regression

`CreatingErrorsFromListVoidResult`: 13.19 ns to 16.70 ns (+3.51 ns), same 64 B. The matching value scenario improved (20.79 ns to 14.24 ns) and the implicit `List` conversions improved for both (value 20.08 to 14.81 ns, void 12.74 to 10.15 ns), and the code path is the same `From(List<Error>)`. So this looks like run noise or code-layout noise on a shared 2-core runner, not a defect of inlining. It is **not proven**: re-run `*CreatingErrorsFromListVoidResult*` with `--job medium` on a quiet machine before drawing a conclusion.

## 5. Comparison with the local smoke run

The earlier local run (Ryzen 5 3600X, Windows 11, `--job short --inProcess`) showed larger gains for the same change: `From(Error)` 5.5 vs 9.4 ns (value), 3.9 vs 6.5 ns (void), and `new(ImmutableArray)` 0.80 and about 0 ns vs 2.8 ns. CI shows the same direction with smaller magnitudes (`From(Error)` 6.7 vs 10.4 ns value, 3.6 vs 5.5 ns void). Different CPU, OS and JIT target, so only the direction is comparable, and both agree.

## 6. Conclusions and next steps

- Change C **helps** and costs nothing in allocations; keep it in `ResultUnionCustomExtreme`.
- It does not explain everything: `new(ImmutableArray)` normalization (changes A/B) is still in the constructor, so the remaining creation gap to the structs has not been tested yet.
- Reads and checks are untouched and flat; D (collapsing the double checks) is still below the significance rule.
- Next: A or B, then E (`[DisassemblyDiagnoser]`) to confirm the inlining on the actual disassembly instead of inferring it from timings.
- Re-run the regression candidate (4) on a dedicated machine, and repeat the whole set with `--job medium` before publishing any number.
