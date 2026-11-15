# Narrow integration tests

Write them only when they add proof a unit test cannot give. Narrow means one real boundary per test, everything else faked or in memory.

## When they make sense

- Data access: real SQL, mappings, migrations, constraints, transactions.
- HTTP pipeline: routing, model binding, validation, auth, serialization, status codes and problem details.
- Messaging, caching or file formats where behaviour lives in the integration itself.

When they do not: pure logic, branching and mapping (unit tests), or "everything wired together" scenarios (slow and brittle).

## Conventions

- Separate project `<Project>.IntegrationTests`, or the same project with `[Trait("Category", "Integration")]`; the script filters on the trait named in `quality-gate.json`.
- Same AAA and Given_When_Then pattern as unit tests.
- Each test owns its data and leaves nothing behind. Never depend on another test's data or on order.
- No fixed ports or fixed names; use random ports and unique identifiers.
- Integration tests count toward the coverage gate only if the gate is run with them; the hook gates unit tests, so core logic must be unit-tested.

## Packages (add only the ones the project needs)

| Package | Role |
|---------|------|
| `Microsoft.NET.Test.Sdk` | Test host |
| `Testcontainers` + module (e.g. `Testcontainers.PostgreSql`, `.MsSql`, `.Redis`) | Real dependencies in disposable Docker containers |
| `Microsoft.AspNetCore.Mvc.Testing` | `WebApplicationFactory<TProgram>` in-process API host |
| `Assertions.Web` (adrianiftode) | AwesomeAssertions-style assertions for `HttpResponseMessage` |

## Testcontainers

- One container per test class or collection via `IAsyncLifetime`/`IClassFixture`, not per test (start time).
- Reset state between tests (delete rows, transaction rollback, or a reset helper) instead of restarting the container.
- Pin the image tag. Never `latest`.
- Needs Docker; `tests.cs integration` fails with exit 2 and a clear message when Docker is unavailable.

## HTTP tests

```csharp
// Act
var response = await client.GetAsync("/orders/42", TestContext.Current.CancellationToken);

// Assert
response.Should().Be200Ok().And.BeAs(new { Id = 42, Status = "Placed" });
```

Replace external services with fakes through `WebApplicationFactory.WithWebHostBuilder(... ConfigureTestServices ...)`; keep the real database via Testcontainers when the test is about persistence.

## Full example

[../examples/IntegrationTest.cs.example](../examples/IntegrationTest.cs.example)

## Run

`dotnet test --project <test project> --filter-trait "Category=Integration"` (wrapped by `tests.cs integration`).
