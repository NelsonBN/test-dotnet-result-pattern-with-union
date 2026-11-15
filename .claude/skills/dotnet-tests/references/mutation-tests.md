# Mutation tests

Coverage says which lines ran; mutation testing says whether the tests would notice a bug. Stryker.NET changes the code (flips `>` to `>=`, removes a statement, returns a default) and reruns the tests; a mutant that no test fails is a **survivor**.

## Setup (always the same)

Stryker is a **local tool** pinned in the repository, never a global install, so every machine and CI run uses the same version.

| File (repo root) | Copy from | Purpose |
|------------------|-----------|---------|
| `.config/dotnet-tools.json` | [../examples/dotnet-tools.json](../examples/dotnet-tools.json) | Pins `dotnet-stryker` (restored with `dotnet tool restore`) |
| `stryker-config.json` | [../examples/stryker-config.json](../examples/stryker-config.json) | Runner, reporters, thresholds, excludes |
| `mutation-tests.bat` | [../examples/mutation-tests.bat](../examples/mutation-tests.bat) | Windows script. **Create it at the root** (the skill script refuses to run mutation without it) |
| `mutation-tests.sh` | [../examples/mutation-tests.sh](../examples/mutation-tests.sh) | Linux and macOS script. **Create it at the root**, same rule (keep LF line endings; `chmod +x`, or `git update-index --chmod=+x mutation-tests.sh`) |

Requirements, all verified with Stryker 5.1.0:

- A solution file (`.sln` or `.slnx`) at the repository root. Stryker then runs in solution context, where `--test-project` / `test-projects` is **rejected**; it finds the test projects from the solution. Use `--project <Source>.csproj` to limit it to one source project.
- `"test-runner": "mtp"` in the config: xunit v3 runs on Microsoft.Testing.Platform. With the default VSTest runner Stryker cannot see the tests and reports 0% ("test coverage capture failed"). The MTP runner is marked preview by Stryker; treat the score as indicative and verify surprising results.
- Add `StrykerOutput/` to `.gitignore`.
- Thresholds: `break` must be lower than or equal to `low` (Stryker refuses to start otherwise). The numbers in `stryker-config.json` and in `quality-gate.json` should match (80).

## Run

```bash
./mutation-tests.sh                       # Linux, macOS
mutation-tests.bat                        # Windows
./mutation-tests.sh --project MyProject.csproj --since:main     # extra arguments go to Stryker
```

The scripts do what a developer would do by hand: `dotnet tool restore`, `dotnet restore`, `dotnet build --no-restore`, then `dotnet stryker --config-file stryker-config.json --open-report` (the report opens in the browser; CI should not use the scripts' `--open-report`, call `dotnet stryker` directly).

For the agent and the quality gate use the skill script, which mutates only what changed and writes to `artifacts/dotnet-tests/mutation/<project>/`:

```bash
dotnet run .claude/skills/dotnet-tests/scripts/tests.cs -- mutation     # changed files, one source project per unit test project
dotnet run .claude/skills/dotnet-tests/scripts/tests.cs -- quality      # coverage + mutation, correlated (below)
```

`--since` needs a branch, tag or commit SHA (not `HEAD`); the script resolves it. Exit codes: `0` score at or above the threshold, `1` below it, `2` tooling error.

## Coverage x mutation: the `quality` report

Neither number is enough alone. `quality` runs both and joins them per file and per surviving mutant, writing `artifacts/dotnet-tests/quality.md`:

| File | Line | Branch | Mutation | Verdict |
|------|------|--------|----------|---------|
| src/Lib/Price.cs | 83.3% | 50.0% | 0.0% | FAIL: line coverage, branch coverage, mutation score |
| src/Lib/Calc.cs | 100% | 100% | 75.0% | FAIL: mutation score (covered but not verified) |

Each survivor is classified by whether coverage reached its line:

| Class | Meaning | Action |
|-------|---------|--------|
| **NOT COVERED** | No test executes the line (Stryker `NoCoverage`, or the line is in the coverage report's uncovered list) | Write a test that reaches it; this also lifts line and branch coverage |
| **COVERED BUT NOT VERIFIED** | A test executes the line but nothing fails when it changes | Strengthen the assertion, add the boundary or negative case. This is the gap coverage alone hides, so fix these first |

Pass only when every changed file meets line, branch **and** mutation gates.

## Killing survivors

1. Read each line of the "Surviving mutants" list: the file, the line, the mutation and its replacement (`price > 100` became `price >= 100`).
2. Add or tighten a test with an input that tells the original from the mutant: boundary values (100 and 101), the exact expected value instead of `BeGreaterThan(0)`, the negative path, an argument check where the interaction is the contract.
3. Re-run `quality` until the files pass.

Typical causes: boundary not tested, assertion too weak (`NotBeNull`, `BeGreaterThan(0)`), `Arg.Any` hiding an argument, missing negative case, result ignored.

## Equivalent mutants

Some mutants change nothing observable (for example `x < 0` to `x <= 0` before `-x`, since `-0 == 0`; logging text; a cache hint). Do not contort tests for them. List them in the report's "Decisions needed". Excluding mutants in the config (`mutate` globs, `ignore-mutations`, `ignore-methods`) requires the user's approval and a reason; never exclude to raise the score.

## Tips

- Keep unit tests fast: Stryker reruns them thousands of times. Do not mutate against integration tests.
- Run coverage first; plain tests fix NoCoverage cheaply.
- A high score on trivial code means little; spend effort on logic with branches and arithmetic.
