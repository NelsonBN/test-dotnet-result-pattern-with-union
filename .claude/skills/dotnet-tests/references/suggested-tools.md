# Suggested tools (ask the user first)

The skill's base stack is fixed (xunit, AwesomeAssertions, NSubstitute, Testcontainers, Microsoft.NET.Test.Sdk, Assertions.Web, Stryker.NET, coverlet, ReportGenerator). Everything below is an optional addition: **propose it with the reason, and add it only after the user agrees.**

| Tool | Use for | Propose when |
|------|---------|--------------|
| `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`) | Deterministic time | Code uses clocks, delays or expiry |
| `Bogus` or `AutoFixture` | Realistic or generated test data | Builders become long, or property-style variation helps |
| `Verify` (snapshot testing) | Large outputs: JSON, HTML, generated code | Asserting a big object field by field is noisy |
| `Respawn` | Fast database reset between integration tests | Integration tests share a Testcontainers database |
| `WireMock.Net` | Fake external HTTP services | Integration tests call third-party APIs |
| `FsCheck` | Property-based tests | Pure logic with invariants (parsers, mappers, money math) |
| `PublicApiGenerator` / `Verify` on the API surface | Detect accidental public API breaks | Libraries |
| `ArchUnitNET` or `NetArchTest.Rules` | Enforce architecture rules (layering, naming) | Layered or modular solutions |
| `xunit.analyzers` (included with xunit.v3) and `Meziantou.Analyzer` | Test-quality analyzer rules | Raise lint coverage of test code |
| `Microsoft.Extensions.Diagnostics.Testing` | Assert logs and metrics | Logging or metrics are part of the contract |
| `ReportGenerator` report type `MarkdownSummaryGithub` | Coverage summary on pull requests | The repo uses GitHub Actions |
| `Microsoft.Testing.Extensions.CodeCoverage` | Alternative collector (Microsoft's) | coverlet cannot instrument the target (note: in a trial run it produced an empty Cobertura report here, so verify first) |
| `Testably.Abstractions` | In-memory file system | Code touches files |

Ask in one short message listing only the tools that match the current change, with a one-line reason each.
