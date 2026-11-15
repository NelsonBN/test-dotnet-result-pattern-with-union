# Test pattern

One pattern for every test, so any test reads the same way.

## Arrange-Act-Assert

```csharp
[Fact]
public void Given_ValidAmount_When_Withdraw_Then_BalanceDecreases()
{
    // Arrange
    var account = new AccountBuilder().WithBalance(100m).Build();

    // Act
    account.Withdraw(30m);

    // Assert
    account.Balance.Should().Be(70m);
}
```

- Exactly one `// Arrange`, one `// Act`, one `// Assert` (merge `// Act & Assert` only for exception assertions).
- One Act (one call on the unit under test) per test.
- No `if`, loops or computed expectations in a test; write the literal expected value.

## Naming: Given_When_Then

`Given_<precondition>_When_<action>_Then_<observable outcome>`, PascalCase parts joined by underscores.

| Good | Bad |
|------|-----|
| `Given_EmptyCart_When_Checkout_Then_ThrowsInvalidOperation` | `CheckoutTest1` |
| `Given_ExpiredToken_When_Validate_Then_ReturnsFailure` | `Validate_Works` |

The name states the behaviour; if you need "and" in the name, split the test.

## Layout

- Project and file mirror structure: see [project-structure.md](project-structure.md) (`src/<P>/<Folder>/<Name>.cs` is tested by `tests/<P>.Tests/<Folder>/<Name>Tests.cs`).
- One test class per unit under test: `OrderService` is tested by `OrderServiceTests`, in the namespace `<P>.Tests.<Folder>`.
- Group by method with nested classes only when a class exceeds roughly 20 tests.
- Shared helpers live in `Builders/`, `Fakes/` and `Fixtures/` inside the test project.

## Test data

- **Builders** (`AccountBuilder`) give a valid default object; each test overrides only what matters to it. Never copy-paste 15-line object setups.
- Use literal, readable values; avoid random data unless the data generator is seeded.
- Data-driven: `[Theory]` + `[InlineData]` for simple values, `[MemberData]`/`TheoryData<>` for objects. One theory per behaviour, not a bundle of unrelated cases.

## Exceptions

```csharp
// Act
var act = () => account.Withdraw(500m);

// Assert
act.Should().Throw<InvalidOperationException>().WithMessage("*insufficient*");
```

## Async

Return `Task`, `await` the Act, use `await act.Should().ThrowAsync<T>()`. Never `.Result` or `.Wait()`. Pass `TestContext.Current.CancellationToken` (xunit v3) to async calls.

## Time, randomness, IO

Inject `TimeProvider` (use `FakeTimeProvider` when approved, see [suggested-tools.md](suggested-tools.md)), `IFileSystem`, `Random` seeds. Never `DateTime.Now` in a test or in code that a test covers without a seam.

## What to cover per feature

Happy path, each boundary (min, max, off-by-one), null/empty/whitespace, invalid state, each failure path and its message or error type, idempotency, and (when relevant) concurrency and cancellation. Bug fix: a test that fails on the old code, written and seen failing before the fix ([tdd-bug-workflow.md](tdd-bug-workflow.md)), named after the bug's behaviour.

## Isolation

No static mutable state, no test order dependence, no shared files; unique names for anything created on disk or in a database. xunit runs classes in parallel: keep fixtures read-only or per-class.
