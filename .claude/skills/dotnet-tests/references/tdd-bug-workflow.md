# Test-first (TDD) for every fix and change

**Mandatory for AI-driven coding**: vibe coding, agentic development, AI automation and software-factory automation, whenever the work is debugging, solving an issue or changing behaviour. A fix that was not first reproduced by a failing test is not accepted.

## The cycle

```
issue detected -> test (unit or integration) -> RED -> fix -> GREEN -> (refactor) -> gates
```

| Step | What you do | How | Accepted only if |
|------|-------------|-----|------------------|
| 1. Detect | An issue arrives from anywhere: ticket, review comment, prompt instruction, debug session, failing CI, a stack trace, an idea | | |
| 2. Test | Write the test that reproduces it, in the mirror test file, named after the symptom | | |
| 3. **Red** | Run only that test and read the failure | `dotnet test --project <P>.Tests.csproj --filter-display-name "*<test>*"` | It fails on an assertion that states the reported symptom |
| 4. Fix | Smallest production change; do not touch the test | | |
| 5. **Green** | Run the same test again | the same command | It passes, and production code (not the test) changed since the red |
| 6. Refactor | Clean up while the tests stay green (optional) | `<tests> unit` | Still green |
| 7. Gates | Structure, unit, coverage, mutation | `<tests> all`, `<tests> quality` | See [quality-gate.md](quality-gate.md) |

`<tests>` is `dotnet run .claude/skills/dotnet-tests/scripts/tests.cs --`. `<P>.Tests.csproj` is the mirror test project; the filter matches the test's display name (`Namespace.Class.Method`), so part of the method name is enough. For an integration test use the `<P>.IntegrationTests.csproj` project instead.

## Sources of issues (all follow the same cycle)

| Source | The test comes from |
|--------|---------------------|
| Ticket / bug report | The steps to reproduce and the expected result |
| Review comment | The behaviour the reviewer says is wrong or missing |
| Prompt instruction ("make X do Y") | The new or changed behaviour, written as an example |
| Debug session / stack trace | The input that triggers the failure, minimized |
| Failing CI or test | The failing case captured as a stable test (fix the code, not the test, unless the test is wrong, and then say so) |
| New feature | The first behaviour, then the next, one red-green cycle each |

## Unit or integration

Prefer a unit test. Use an integration test only when the bug lives in a boundary a unit test cannot reproduce (SQL, mapping, serialization, HTTP pipeline, messaging); see [integration-tests.md](integration-tests.md). Either way the cycle is identical.

## What a good red looks like

- **It fails, on an assertion.** `Expected r to be 90M, but found 100M` is a red. A compile error, a missing type, "No tests ran" (exit code 8) or a crash in setup is not a red: fix the test until it fails on the assertion.
- **It fails for the reported reason.** Read the message: it must describe the symptom from the issue, not an unrelated problem.
- **It is minimal.** One behaviour, the smallest input that shows it, literal expected values.
- **It would catch the regression later.** Name it after the behaviour: `Given_PriceOf100_When_Discount_Then_TenPercentOff`.
- **New API:** the test cannot compile before the code exists. Add the signature first with a placeholder body that returns a default value, then record the red.

## Worked example

Ticket: "A price of exactly 100 gets no discount."

1. Test in `tests/Lib.Tests/PriceTests.cs`:
   ```csharp
   [Fact]
   public void Given_PriceOf100_When_Discount_Then_TenPercentOff()
   {
       // Act
       var result = Price.Discount(100m);

       // Assert
       result.Should().Be(90m);
   }
   ```
2. Red: `dotnet test --project tests/Lib.Tests/Lib.Tests.csproj --filter-display-name "*Given_PriceOf100*"` fails with `Expected result to be 90M, but found 100M`. Quote that line in the reply.
3. Fix `price > 100` to `price >= 100`.
4. Green: run the same command; it passes. Say so in the reply.
5. `all` and `quality`: the boundary is now covered and the mutant `>=` -> `>` is killed.

## Anti-patterns (never)

| Anti-pattern | Why it fails | Instead |
|--------------|--------------|---------|
| Fix first, write the test after | The test was shaped by the fix and never proved it can fail | Test, red, then fix |
| A test written to pass | It proves nothing | Make it fail on the symptom first |
| Editing, skipping or deleting the test to reach green | Hides the bug | Fix the code |
| Fixing "blind" because the issue cannot be reproduced | No proof, no regression guard | Stop and ask the user for the missing information |
| One huge test for several bugs | Unclear which fix made it pass | One cycle per behaviour |
| Weakening an existing test that now fails | The change broke a contract | Decide with the user whether the contract changed |

## Exemptions: the waiver

Only these may skip the failing test, and you must tell the user that you skipped it and why:

- Pure refactor already covered by passing tests (no behaviour change).
- Docs, comments, formatting, config, build files.

A waiver is never valid for a bug, ticket, review comment or any behaviour change. If unsure, do the cycle.

## How it is followed

This rule is guidance for the agent; no script enforces it, and the Stop hook cannot see the order in which things were written. The gates catch the consequences: the mirror structure and coverage gate fail when the test is missing, and mutation testing (`quality`) exposes tests that were written to pass. Keep the evidence in your reply: the red failure message, the fix, and the green result.
