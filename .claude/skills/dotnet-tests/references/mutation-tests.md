# Mutation tests

Coverage says which lines ran; mutation testing says whether the tests would notice a bug. Stryker.NET changes the code (flips `>` to `>=`, removes a statement, returns a default) and reruns the tests; a mutant that no test fails is a **survivor**.

## Setup

```bash
dotnet tool install -g dotnet-stryker      # once; or a local tool manifest: dotnet new tool-manifest && dotnet tool install dotnet-stryker
```

Config: [../examples/stryker-config.example.json](../examples/stryker-config.example.json) copied to the test project as `stryker-config.json`. The break threshold comes from `quality-gate.json` (80).

## Run

```bash
dotnet run .claude/skills/dotnet-tests/scripts/tests.cs -- mutation
```

It runs, from the test project folder: `dotnet stryker --since:<default-branch> --config-file stryker-config.json --threshold-break 80`, so only code changed versus the default branch is mutated. Output: `artifacts/dotnet-tests/mutation/` (HTML and JSON reports). Run the whole project (`--since` omitted) only on request, for example before a release.

Exit codes: `0` score at or above the threshold, `1` below it, `2` tooling error.

## Reading the result

| Status | Meaning | Action |
|--------|---------|--------|
| Killed | A test failed | Good |
| Survived | Tests passed with the bug | Fix (see below) |
| NoCoverage | No test reaches the code | Write a test |
| Timeout | Mutant caused an infinite loop | Counts as killed |
| Ignored / CompileError | Excluded or invalid | Ignore |

## Killing survivors

1. Open the report, take each survivor, read the mutation (`a > b` became `a >= b`).
2. Add or tighten a test with an input that distinguishes the original from the mutant (boundary values, exact expected value, a call-count or argument check where the interaction is the contract).
3. Re-run until the score meets the threshold.

Typical causes: boundary not tested, assertion too weak (`NotBeNull` instead of the value), `Arg.Any` hiding an argument, missing negative case, result ignored.

## Equivalent mutants

Some mutants change nothing observable (for example logging text, a cache hint). Do not contort tests for them. List them in the report's "Decisions needed". Excluding mutants in the Stryker config (`mutate` globs, `ignore-mutations`, `ignore-methods`) requires the user's approval and a reason; never exclude to raise the score.

## Tips

- Keep unit tests fast: Stryker reruns them thousands of times. Do not mutate against integration tests.
- Run coverage first; fix NoCoverage cheaply with plain tests.
- A high score on trivial code means little; spend effort on logic with branches and arithmetic.
