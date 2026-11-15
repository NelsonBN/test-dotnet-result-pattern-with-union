---
name: dotnet-tests
description: 'Write, run and gate tests for .NET code: unit tests (xunit, AwesomeAssertions, NSubstitute), narrow integration tests (Testcontainers, WebApplicationFactory, Assertions.Web) and mutation tests (Stryker.NET), all in one fixed Arrange-Act-Assert / Given_When_Then pattern, with a coverage gate (coverlet.MTP + ReportGenerator) and a Stop hook that sends the agent back to finish the tests when they fail or coverage is too low. Use when implementing or changing a feature, fixing a bug (regression test first), adding or reviewing tests, checking coverage, killing surviving mutants, or before declaring a change done. Command form: /dotnet-tests [unit|integration|coverage|mutation|all].'
argument-hint: '[unit|integration|coverage|mutation|all]'
license: MIT
allowed-tools: Bash
---

# .NET Tests

Every feature or fix ships with tests that prove it works and keep it working. Write them in one fixed pattern, run them with fixed commands, and do not call the work done until the quality gate passes.

## When to Use

- Implementing or changing behaviour: add or update tests in the same change.
- Fixing a bug: write the failing regression test first.
- Asked to add, review or improve tests, check coverage or kill surviving mutants.
- Before reporting a change as done. The `dotnet-tests` Stop hook enforces this.

## When Not to Use

- Benchmarks (BenchmarkDotNet projects are not tests).
- Lint, style and analyzer findings: use `dotnet-lint`, and run it after the tests pass.

## Prerequisites

- .NET SDK on `PATH`, run from the repository root.
- `<tests>` below is `dotnet run <skill-dir>/scripts/tests.cs --` (`<skill-dir>` is the directory containing this file).
- Integration tests need Docker only when they use Testcontainers; mutation needs `dotnet tool install -g dotnet-stryker`; coverage reports need `dotnet tool install -g dotnet-reportgenerator-globaltool`. The script checks each and says what is missing.
- xunit v3 runs on Microsoft.Testing.Platform: the repo's `global.json` needs `"test": { "runner": "Microsoft.Testing.Platform" }` (tested with SDK 11.0.100) or `dotnet test` fails. Details in [references/unit-tests.md](references/unit-tests.md).
- If the repo has no test project, create one from [examples/TestProject.csproj.example](examples/TestProject.csproj.example) before writing tests.

## Core Rules

1. **AAA.** Every test has `// Arrange`, `// Act`, `// Assert` sections, in that order, one Act per test.
2. **Names:** `Given_<state>_When_<action>_Then_<outcome>`. One behaviour per test.
3. **Assertions:** AwesomeAssertions (`.Should()`) only. A test without an assertion is a bug.
4. **Deterministic.** No real clock, randomness, network or shared state without a fake. Tests run in any order and in parallel.
5. **Test the contract, not the implementation.** Mock only boundaries (I/O, clock, external services) with NSubstitute; never mock the type under test or plain value objects.
6. **Prove it can fail.** Each test must go red if the behaviour breaks. Confirm with mutation testing.
7. Follow `.editorconfig` and the repo's `AGENTS.md`/`CLAUDE.md` style.

### NEVER

- **NEVER** lower the thresholds in `quality-gate.json` or edit it without the user's approval.
- **NEVER** use `[ExcludeFromCodeCoverage]`, coverage excludes, `[Fact(Skip=...)]`, `#if` or test deletion to pass the gate.
- **NEVER** weaken or delete an assertion to make a test pass; fix the code or the expectation with a reason.
- **NEVER** retry flaky tests blindly: fix the cause, or tell the user.
- **NEVER** add a package or tool beyond the ones named here without asking (see [references/suggested-tools.md](references/suggested-tools.md)).
- **NEVER** commit or push.

## Workflow

Follow in order.

1. **Locate** the test project (`Microsoft.NET.Test.Sdk` or `xunit` reference) and the existing conventions; mirror them. Create the project if missing.
2. **List behaviours** for the feature: happy path, boundaries, invalid input (null, empty, out of range), failure paths, ordering/concurrency when relevant. Each becomes a test. See [references/test-pattern.md](references/test-pattern.md).
3. **Unit tests first** (red, then green). See [references/unit-tests.md](references/unit-tests.md) and [examples/UnitTest.cs.example](examples/UnitTest.cs.example).
4. **Narrow integration tests** only where a unit test cannot prove the behaviour (database, HTTP pipeline, serialization, messaging). Tag them `[Trait("Category", "Integration")]`. See [references/integration-tests.md](references/integration-tests.md).
5. **Run** the deterministic commands:

   | Goal | Command |
   |------|---------|
   | Unit tests | `<tests> unit` |
   | Integration tests | `<tests> integration` |
   | Coverage gate | `<tests> coverage` |
   | Mutation (changed files) | `<tests> mutation` |
   | Unit + coverage (what the hook runs) | `<tests> all` |

   Outputs go to `artifacts/dotnet-tests/`. Exit codes: `0` pass, `1` gate or test failure, `2` tooling error (fix the environment, not the gate).
6. **Coverage gate** ([references/coverage.md](references/coverage.md)): changed code must reach the thresholds in [quality-gate.json](quality-gate.json). Add tests for the uncovered lines and branches listed in the output.
7. **Mutation** ([references/mutation-tests.md](references/mutation-tests.md)): run on changed files; kill every survivor you can by strengthening assertions or adding cases. List the ones you cannot kill (equivalent mutants) in the report.
8. **Review** against [references/quality-gate.md](references/quality-gate.md) (definition of done and test smells), then run `dotnet-lint`.
9. **Report** (always last): `<tests> --report` writes and prints `artifacts/dotnet-tests/report.md`: tests added, results, coverage vs gate, mutation score, surviving mutants, decisions needed.

## Arguments

| Invocation | Meaning |
|------------|---------|
| `/dotnet-tests` or `all` | Steps 1 to 9 for the current change |
| `unit` / `integration` | Run only that category |
| `coverage` | Run the coverage gate |
| `mutation` | Run Stryker on files changed vs the default branch |

## Hook enforcement

A `Stop` hook (`.claude/settings.json`) runs `<tests> hook stop` when the agent tries to end its turn:

- No-op when there are no changed `.cs` files, no test project, or `DOTNET_TESTS_HOOK=off`.
- Otherwise it runs the unit tests and the coverage gate on changed code. A failing test or coverage below the gate blocks the stop and hands the list of failures and uncovered lines back; continue from workflow step 3.
- Mutation is never run by the hook (too slow); run it on demand.
- After 3 consecutive blocks it lets the agent stop with a warning, so it cannot loop forever.

## References (load on demand)

- [references/test-pattern.md](references/test-pattern.md): AAA, naming, layout, builders, data-driven tests
- [references/unit-tests.md](references/unit-tests.md): xunit, AwesomeAssertions, NSubstitute test doubles
- [references/integration-tests.md](references/integration-tests.md): when and how, Testcontainers, HTTP, isolation
- [references/mutation-tests.md](references/mutation-tests.md): Stryker config, reading and killing survivors
- [references/coverage.md](references/coverage.md): coverlet, ReportGenerator, what may be excluded
- [references/quality-gate.md](references/quality-gate.md): thresholds, definition of done, smells, flaky policy, CI
- [references/suggested-tools.md](references/suggested-tools.md): extra tools to propose to the user (ask first)
