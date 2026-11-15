# Tools

Every tool the skill knows, in one table, and the tools that must never be used.

**Usage** column: **Always** = every test project of that kind uses it; **Specific cases** = use it only when the situation in the last column applies.

Tools marked (base) are the skill's fixed stack and need no approval. Every other tool is an optional addition: **propose it to the user with the reason, and add it only after they agree.** Ask in one short message, listing only the tools that match the current change.

## Tools to use

| Tool | Use for | Usage | When |
|------|---------|-------|------|
| `xunit.v3` (base) | Test framework | Always | Every test project |
| `xunit.runner.visualstudio` (base) | Runner for `dotnet test` and IDEs | Always | Every test project |
| `Microsoft.NET.Test.Sdk` (base) | Test host | Always | Every test project |
| `xunit.analyzers` (base, included with xunit.v3) | Test-quality analyzer rules | Always | Every test project |
| `AwesomeAssertions` (base) | All assertions (`.Should()`) | Always | Every test |
| `coverlet.MTP` (base) | Coverage collection (Cobertura) | Always | Coverage gate, every unit test project |
| `Stryker.NET` (`dotnet-stryker`, base) | Mutation testing | Always | Mutation step on changed files |
| `NSubstitute` (base) | Dummy, stub, spy, mock, fake | Specific cases | The unit depends on a boundary (I/O, clock, external service) |
| `Testcontainers` + module (base) | Real dependencies in disposable containers | Specific cases | Integration tests that need a real database, cache or broker |
| `Microsoft.AspNetCore.Mvc.Testing` (base) | `WebApplicationFactory` in-process API host | Specific cases | Integration tests of web/API projects |
| `Assertions.Web` (base) | Assertions on `HttpResponseMessage` | Specific cases | HTTP integration tests |
| `ReportGenerator` (base) | Merged summary and HTML coverage report | Specific cases | A human-readable or CI report is wanted (the gate works without it) |
| `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`) | Deterministic time | Specific cases | Code uses clocks, delays or expiry |
| `Bogus` | Realistic generated test data | Specific cases | Builders become long, or varied data helps |
| `WireMock.Net.Testcontainers` (`WireMockContainer`) | Fake external HTTP services, run as a WireMock container through Testcontainers (not in-process) | Specific cases | Integration tests call third-party APIs |
| `Microsoft.Extensions.Diagnostics.Testing` | Assert logs and metrics | Specific cases | Logging or metrics are part of the contract |
| `ReportGenerator` report type `MarkdownSummaryGithub` | Coverage summary on pull requests | Specific cases | The repo uses GitHub Actions |

## Tools to never use

Do not add these packages, do not write code against them, and replace them when found in a test project.

| Tool | Never because | Use instead |
|------|---------------|-------------|
| `FluentAssertions` | The skill standardizes on its maintained fork; two assertion libraries in one codebase is noise | `AwesomeAssertions` |
| `FluentAssertions.Web` | Built on `FluentAssertions` | `Assertions.Web` |
| `Moq` | The skill standardizes on one mocking library | `NSubstitute` |
