# Unit tests

## Stack

| Package | Role |
|---------|------|
| `xunit.v3` (latest stable) | Test framework. Check the current version with `dotnet package search xunit.v3` and use it |
| `xunit.runner.visualstudio` | Runner for `dotnet test` / IDEs |
| `Microsoft.NET.Test.Sdk` | Test host for `dotnet test` |
| `AwesomeAssertions` | Assertions (`.Should()`), the maintained FluentAssertions fork |
| `NSubstitute` | Test doubles |
| `coverlet.MTP` | Coverage on Microsoft.Testing.Platform (see [coverage.md](coverage.md)) |

xunit v3 runs on Microsoft.Testing.Platform (MTP). With the .NET 10+ SDK, `dotnet test` must opt in through `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`), otherwise it fails with "Testing with VSTest target is no longer supported". On MTP, pass the project with `--project` and filter by trait with `--filter-trait` / `--filter-not-trait`.

Full project file: [../examples/TestProject.csproj.example](../examples/TestProject.csproj.example).

## What a unit test is

Fast (milliseconds), in-process, no network, disk, database or clock. Tests one unit's observable behaviour: return value, state change, or an interaction at a boundary.

## xunit essentials

- `[Fact]` single case, `[Theory]` + data for variations.
- A new class instance per test: use the constructor for setup, `IDisposable`/`IAsyncLifetime` for cleanup. Use `IClassFixture<T>` only for expensive, read-only shared state.
- `[Trait("Category", "Integration")]` marks integration tests; unit tests carry no trait.
- Use `Assert` only for what AwesomeAssertions cannot express; do not mix styles inside one test.

## AwesomeAssertions

```csharp
result.IsSuccess.Should().BeTrue();
result.Errors.Should().ContainSingle().Which.Code.Should().Be("not_found");
list.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
act.Should().Throw<ArgumentException>().WithParameterName("name");
```

- Assert the specific outcome, never just `NotBeNull` or `NotThrow` when a value is available.
- Give a `because` only when the reason is not obvious from the test name.

## Test doubles with NSubstitute

Pick the lightest double that works:

| Double | Use when | NSubstitute |
|--------|----------|-------------|
| **Dummy** | A parameter is required but unused | `Substitute.For<ILogger>()` with no setup |
| **Stub** | The unit needs canned answers from a dependency | `repo.Find(id).Returns(order)` |
| **Spy** | You need to check afterwards what happened | call the unit, then `logger.Received(1).Log(...)` |
| **Mock** | The interaction is the behaviour (message sent, event published) | `bus.Received(1).Publish(Arg.Is<OrderPlaced>(e => e.Id == id))` |
| **Fake** | A working lightweight implementation is clearer (in-memory repository, `FakeTimeProvider`) | hand-written class in `Fakes/` |

Rules:

- Mock boundaries only. Never mock the unit under test, value objects, DTOs or collections; use real instances.
- Prefer state verification (assert the outcome) over interaction verification. Use `Received` only when the interaction is the contract.
- Avoid `Arg.Any<T>()` for the argument that matters; match it (`Arg.Is`), otherwise the test passes for any input.
- If a test needs more than about three doubles, the unit has too many dependencies: simplify the design or use a Fake.
- `DidNotReceive()` is a valid, strong assertion for "must not happen" paths.

## Anti-patterns

Testing private members (test through the public API), asserting on mock call counts for pure logic, one giant test with many Acts, reusing mutable state between tests, `Thread.Sleep`, catching exceptions in tests, tests that duplicate production logic to compute the expectation.
