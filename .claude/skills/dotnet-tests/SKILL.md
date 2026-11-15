---
name: dotnet-tests
description: 'Write, run and gate tests for .NET code: unit tests (xunit, AwesomeAssertions, NSubstitute), narrow integration tests (Testcontainers, WebApplicationFactory, Assertions.Web) and mutation tests (Stryker.NET), all in one fixed Arrange-Act-Assert / Given_When_Then pattern, with a coverage gate (coverlet.MTP + ReportGenerator) and a Stop hook that sends the agent back to finish the tests when they fail or coverage is too low. Test-first (TDD: red, fix, green) is mandatory for every fix or change made by an AI. Use when fixing a bug, a ticket, an issue, a review comment, a debug finding or any instruction (reproduce with a failing test first), implementing or changing a feature, adding or reviewing tests, checking coverage, killing surviving mutants, or before declaring a change done. Command form: /dotnet-tests [unit|integration|coverage|mutation|quality|all].'
argument-hint: '[unit|integration|coverage|mutation|quality|all]'
license: MIT
allowed-tools: Bash(dotnet:*), Bash(reportgenerator:*), Bash(docker:*), Bash(git:*), PowerShell(dotnet:*), PowerShell(reportgenerator:*), PowerShell(docker:*), PowerShell(git:*), Read, Edit, Write, Grep, Glob
---

# .NET Tests

Every feature or fix ships with tests that prove it works and keep it working. Write them in one fixed pattern, run them with fixed commands, and do not call the work done until the quality gate passes.

> ## HARD RULE: no fix and no change without a failing test first (TDD)
>
> For **every** issue, whatever its source (ticket, review comment, prompt instruction, debug session, failing build, anything else) and for new behaviour:
>
> 1. **Write the test first** (unit, or integration when the bug needs a real boundary) that reproduces the problem.
> 2. **Red:** run only that test (`dotnet test --project <P>.Tests.csproj --filter-display-name "*<test>*"`) and watch it fail on an assertion that states the reported symptom. A test that passes, does not compile, or matches nothing is not a red. Quote the failure message in your reply.
> 3. **Fix** with the smallest production change. Never edit the test to make it pass.
> 4. **Green:** run the same test again and watch it pass. Say so in your reply.
> 5. **Refactor** only while green, then run the gates below.
>
> Never fix first and write the test afterwards. If the issue cannot be reproduced with a test, stop and ask the user. Only pure refactors already covered by passing tests, docs, config and formatting may skip it, and you must tell the user that you skipped it and why. Details: [references/tdd-bug-workflow.md](references/tdd-bug-workflow.md).

## When to Use

- Fixing anything (bug, ticket, review comment, debug finding, instruction): write the failing reproduction first, see the hard rule above.
- Implementing or changing behaviour: the failing test comes first, then the code.
- Asked to add, review or improve tests, check coverage or kill surviving mutants.
- Before reporting a change as done. The `dotnet-tests` Stop hook enforces this.

## When Not to Use

- Benchmarks (BenchmarkDotNet projects are not tests).
- Lint, style and analyzer findings: use `dotnet-lint`, and run it after the tests pass.

## Prerequisites

- .NET SDK on `PATH`, run from the repository root.
- `<tests>` below is `dotnet run <skill-dir>/scripts/tests.cs --` (`<skill-dir>` is the directory containing this file).
- Integration tests need Docker only when they use Testcontainers; mutation uses Stryker as a **local tool** from `.config/dotnet-tools.json` (`dotnet tool restore`; never a global install) plus `stryker-config.json`, the `mutation-tests.sh` and `mutation-tests.bat` scripts (copied from `examples/` to the repository root) and a solution file at the root, see [references/mutation-tests.md](references/mutation-tests.md); coverage reports need `dotnet tool install -g dotnet-reportgenerator-globaltool`. The script checks each and says what is missing.
- xunit v3 runs on Microsoft.Testing.Platform: the repo's `global.json` needs `"test": { "runner": "Microsoft.Testing.Platform" }` (tested with SDK 11.0.100) or `dotnet test` fails. Details in [references/unit-tests.md](references/unit-tests.md).
- If the repo has no test project, create one from [examples/TestProject.csproj.example](examples/TestProject.csproj.example) before writing tests.

## Core Rules

1. **Test first (TDD).** Red, fix, green for every issue and change, see the hard rule above.
2. **AAA.** Every test has `// Arrange`, `// Act`, `// Assert` sections, in that order, one Act per test.
3. **Structure:** one `*.Tests.csproj` per source project and one `*Tests.cs` per source file, same relative folder; `*.IntegrationTests.csproj` for web/API projects. The mapping is by name and is enforced.
4. **Names:** `Given_<state>_When_<action>_Then_<outcome>`. One behaviour per test.
5. **Assertions:** AwesomeAssertions (`.Should()`) only. A test without an assertion is a bug.
6. **Deterministic.** No real clock, randomness, network or shared state without a fake. Tests run in any order and in parallel.
7. **Test the contract, not the implementation.** Mock only boundaries (I/O, clock, external services) with NSubstitute; never mock the type under test or plain value objects.
8. **Prove it can fail.** Each test must go red if the behaviour breaks. Confirm with mutation testing.
9. Follow `.editorconfig` and the repo's `AGENTS.md`/`CLAUDE.md` style.
10. **Keep test setup simple.** Plain declarative csproj (e.g. unconditional `<Using Include="Xunit" />`); no MSBuild conditions, custom targets or clever property logic unless a real problem requires them.

### NEVER

- **NEVER** lower the thresholds in `quality-gate.json` or edit it without the user's approval.
- **NEVER** use `[ExcludeFromCodeCoverage]`, coverage excludes, `[Fact(Skip=...)]`, `#if` or test deletion to pass the gate.
- **NEVER** weaken or delete an assertion to make a test pass; fix the code or the expectation with a reason.
- **NEVER** retry flaky tests blindly: fix the cause, or tell the user.
- **NEVER** use a tool from the [tools to never use](references/suggested-tools.md#tools-to-never-use) list.
- **NEVER** add a tool beyond the base ones in the [tools to use](references/suggested-tools.md#tools-to-use) table without asking.
- **NEVER** install Stryker globally or run it without the repo's `.config/dotnet-tools.json`, `stryker-config.json` and root `mutation-tests.*` scripts.
- **NEVER** fix an issue, ticket, comment, instruction or debug finding before a failing test that reproduces it has been seen failing.
- **NEVER** write the test after the fix, write a test to pass, or weaken, skip or delete the reproducing test to get to green.
- **NEVER** fix blind: if the problem cannot be reproduced by a test, stop and ask the user.
- **NEVER** commit or push.

## Workflow

Follow in order.

1. **Reproduce first (mandatory)** ([references/tdd-bug-workflow.md](references/tdd-bug-workflow.md)): turn the issue or the new behaviour into a test in the mirror test file, named after the symptom. Run only that test (`dotnet test --project <P>.Tests.csproj --filter-display-name "*<test>*"`) and confirm it fails for the reported reason. For new API, add the signature first (returning a default value) so the test compiles and fails on the assertion.
2. **Fix:** the smallest production change that satisfies the test. Run the same test again and confirm it passes (green). Refactor only while green.
3. **Locate or create the mirror structure** ([references/project-structure.md](references/project-structure.md)): `src/<P>/<P>.csproj` has `tests/<P>.Tests/<P>.Tests.csproj` (plus `tests/<P>.IntegrationTests/<P>.IntegrationTests.csproj` for web/API projects), and `src/<P>/<Folder>/<Name>.cs` has `tests/<P>.Tests/<Folder>/<Name>Tests.cs`. Never one shared test project. Check with `<tests> structure`. (Create the project and file before step 1 when they do not exist yet.)
4. **List the remaining behaviours** for the feature: happy path, boundaries, invalid input (null, empty, out of range), failure paths, ordering/concurrency when relevant. Each becomes a test, written test-first as above. See [references/test-pattern.md](references/test-pattern.md), [references/unit-tests.md](references/unit-tests.md) and [examples/UnitTest.cs.example](examples/UnitTest.cs.example).
5. **Narrow integration tests** only where a unit test cannot prove the behaviour (database, HTTP pipeline, serialization, messaging). Put them in the `<P>.IntegrationTests` project, tagged `[Trait("Category", "Integration")]`. See [references/integration-tests.md](references/integration-tests.md).
6. **Run** the deterministic commands:

   | Goal | Command |
   |------|---------|
   | Red, then green: run only the reproducing test | `dotnet test --project <P>.Tests.csproj --filter-display-name "*<test>*"` |
   | Mirror structure check | `<tests> structure` |
   | Unit tests | `<tests> unit` |
   | Integration tests | `<tests> integration` |
   | Coverage gate | `<tests> coverage` |
   | Mutation (changed files) | `<tests> mutation` |
   | Coverage x mutation, correlated | `<tests> quality` |
   | Structure + unit + coverage (what the hook runs) | `<tests> all` |

   Outputs go to `artifacts/dotnet-tests/`. Exit codes: `0` pass, `1` gate or test failure, `2` tooling error (fix the environment, not the gate).
7. **Coverage gate** ([references/coverage.md](references/coverage.md)): changed code must reach the thresholds in [quality-gate.json](quality-gate.json). Add tests for the uncovered lines and branches listed in the output.
8. **Quality** ([references/mutation-tests.md](references/mutation-tests.md)): run `<tests> quality`. It joins coverage and mutation per file and per surviving mutant (`artifacts/dotnet-tests/quality.md`): *not covered* survivors need a test that reaches the line, *covered but not verified* survivors need a stronger assertion (fix those first). Repeat until every changed file meets the line, branch and mutation gates; list the equivalent mutants you cannot kill in the report.
9. **Review** against [references/quality-gate.md](references/quality-gate.md) (definition of done and test smells), then run `dotnet-lint`.
10. **Report** (always last): `<tests> --report` writes and prints `artifacts/dotnet-tests/report.md`: tests added, results, coverage vs gate, mutation score, surviving mutants, decisions needed.

## Arguments

| Invocation | Meaning |
|------------|---------|
| `/dotnet-tests` or `all` | Steps 1 to 10 for the current change |
| `structure` | Check the mirror project and file convention only |
| `unit` / `integration` | Run only that category |
| `coverage` | Run the coverage gate |
| `mutation` | Run Stryker on files changed vs the default branch |
| `quality` | Coverage + mutation correlated into one report and verdict |

## Hook enforcement

A `Stop` hook (`.claude/settings.json`) runs `<tests> hook stop` when the agent tries to end its turn:

- No-op when there are no changed `.cs` files, no test project, or `DOTNET_TESTS_HOOK=off`.
- Otherwise it checks the mirror structure, then runs the unit tests and the coverage gate on changed code. A missing test project or file (with its exact expected path), a failing test, or coverage below the gate blocks the stop and hands the list back; continue from workflow step 1.
- The hook cannot see whether the test was written first. The test-first rule is the agent's own obligation: follow the hard rule above and say what you observed.
- Mutation is never run by the hook (too slow); run it on demand.
- After 3 consecutive blocks it lets the agent stop with a warning, so it cannot loop forever.

## References (load on demand)

- [references/tdd-bug-workflow.md](references/tdd-bug-workflow.md): the mandatory red, fix, green flow for every issue and change, red quality, when a waiver is acceptable
- [references/project-structure.md](references/project-structure.md): the mirror project and file convention, what is exempt, how it is enforced
- [references/test-pattern.md](references/test-pattern.md): AAA, naming, layout, builders, data-driven tests
- [references/unit-tests.md](references/unit-tests.md): xunit, AwesomeAssertions, NSubstitute test doubles
- [references/integration-tests.md](references/integration-tests.md): when and how, Testcontainers, HTTP, isolation
- [references/mutation-tests.md](references/mutation-tests.md): Stryker setup (tool manifest, config, `mutation-tests.*` scripts), the coverage x mutation `quality` report, killing survivors
- [references/coverage.md](references/coverage.md): coverlet, ReportGenerator, what may be excluded
- [references/quality-gate.md](references/quality-gate.md): thresholds, definition of done, smells, flaky policy, CI
- [references/suggested-tools.md](references/suggested-tools.md): every tool (always vs specific cases), optional extras to propose (ask first) and tools never to use
