# Coverage

Coverage is a floor, not a goal: it finds code no test touches. Pair it with mutation testing to know the tests are meaningful.

## Tools

- `coverlet.MTP` in the test project collects coverage (Cobertura XML) on Microsoft.Testing.Platform, which xunit v3 uses. (`coverlet.collector` only works with the VSTest runner, which the .NET 10+ SDK no longer supports for xunit v3.) The repo needs the MTP opt-in in `global.json`, see [unit-tests.md](unit-tests.md).
- `dotnet-reportgenerator-globaltool` merges the files and writes summaries and HTML (`dotnet tool install -g dotnet-reportgenerator-globaltool`).

## Run

```bash
dotnet run .claude/skills/dotnet-tests/scripts/tests.cs -- coverage
```

Equivalent manual commands:

```bash
dotnet test --project <P>.Tests.csproj --coverlet --coverlet-output-format cobertura --results-directory artifacts/dotnet-tests/results
reportgenerator -reports:"artifacts/dotnet-tests/results/**/*.cobertura*.xml" -targetdir:artifacts/dotnet-tests/coverage -reporttypes:"TextSummary;Cobertura;Html"
```

The script also passes `--coverlet-exclude-by-file` (migrations, `*.g.cs`, `obj`) and `--coverlet-exclude-by-attribute` (`GeneratedCode`, `CompilerGenerated`); test assemblies are not instrumented by default.

## The gate

Defaults in [../quality-gate.json](../quality-gate.json): **line 90%, branch 85%**. Only `*.Tests` projects are run for coverage. The script evaluates the gate on the source files changed versus the default branch (the hook: versus `HEAD`), not on the whole solution, so legacy code does not block new work. Whole-solution numbers are printed for information.

If a file fails the gate, the output lists the uncovered line ranges and branches; write tests for them, then re-run.

## What may be excluded

Only code with no logic worth testing, and only with the user's approval and a comment explaining why:

- Generated code (`[GeneratedCode]`, `*.g.cs`, designer files), EF migrations, `Program.cs` bootstrapping.
- Pure DTOs/records without behaviour.

Never exclude to pass the gate: no `[ExcludeFromCodeCoverage]` on logic, no extra `--coverlet-exclude*` options, no `--coverlet-skip-auto-props` tricks.

## Reading the numbers

- Line coverage: share of executable lines hit. Branch coverage: share of `if`/`switch`/`?:`/`&&` outcomes hit; this is the one that exposes missing negative cases.
- 100% is not required; an unreachable defensive branch can stay uncovered (list it in the report).
- High coverage with assertion-free tests is worthless: that is what mutation testing catches. `tests.cs quality` correlates the two per file and per surviving mutant (see [mutation-tests.md](mutation-tests.md)).

## CI

Upload `artifacts/dotnet-tests/coverage` and fail the job on a non-zero script exit. Sample workflow: [quality-gate.md](quality-gate.md#ci).
