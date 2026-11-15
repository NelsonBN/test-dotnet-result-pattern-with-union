# Project and file structure

Every implementation project has a sibling test project, and every implementation file has a sibling test file. The mapping is by name, so the agent, the script and the Stop hook can all find it without guessing.

## The three rules

1. **One unit test project per source project**, named `<Project>.Tests`, found by the file suffix `*.Tests.csproj`.
2. **One integration test project per web/API source project** (SDK `Microsoft.NET.Sdk.Web` or an ASP.NET Core reference), named `<Project>.IntegrationTests`, found by the suffix `*.IntegrationTests.csproj`. Other projects get one only when they own a real boundary (database, messaging) worth testing against the real thing.
3. **One unit test file per source file**, same relative folder, class name plus `Tests`, found by the suffix `*Tests.cs`.

```
src/
  Domain/
    Domain.csproj
    Clients/
      Client.cs
      IClientRepository.cs        interface only: no test file required
    Orders/
      Order.cs
  Api/
    Api.csproj                    web project
    Program.cs                    exempt
    Endpoints/
      OrdersEndpoint.cs
tests/
  Domain.Tests/
    Domain.Tests.csproj
    Clients/
      ClientTests.cs              mirrors src/Domain/Clients/Client.cs
    Orders/
      OrderTests.cs
    Builders/                     helpers: exempt
      ClientBuilder.cs
  Api.Tests/
    Api.Tests.csproj
    Endpoints/
      OrdersEndpointTests.cs
  Api.IntegrationTests/
    Api.IntegrationTests.csproj   required: Api is a web project
    Endpoints/
      OrdersEndpointIntegrationTests.cs
```

## Mapping table

| Source | Mirror |
|--------|--------|
| `src/<P>/<P>.csproj` | `tests/<P>.Tests/<P>.Tests.csproj` |
| `src/<P>/<P>.csproj` (web/API) | `tests/<P>.IntegrationTests/<P>.IntegrationTests.csproj` |
| `src/<P>/<Folder>/<Name>.cs` | `tests/<P>.Tests/<Folder>/<Name>Tests.cs` |
| `src/<P>/<Folder>/<Name>.cs` (when a real boundary matters) | `tests/<P>.IntegrationTests/<Folder>/<Name>IntegrationTests.cs` |

The test project is named after its own source project: `Api.csproj` pairs with `Api.Tests` and `Api.IntegrationTests`, never with another project's name.

## Details

- The test class inside `ClientTests.cs` is `ClientTests`, in the namespace `<P>.Tests.<Folder>` (the source namespace plus `.Tests`).
- A source file that declares several types is covered by `<FileName>Tests.cs`; do not split the tests per type.
- Nested or partial folders mirror exactly; do not flatten.
- Each test project references only its own source project (the integration project may also reference `Microsoft.AspNetCore.Mvc.Testing`, Testcontainers and so on).
- Helpers (`Builders/`, `Fakes/`, `Fixtures/`) have no source counterpart and are not checked.
- Unit and coverage runs use `*.Tests.csproj` only; the integration run uses `*.IntegrationTests.csproj` only. The `Category=Integration` trait stays available as an extra filter.

## What is exempt from the one-to-one file rule

Configured in [../quality-gate.json](../quality-gate.json) as `structureIgnore`: `Program.cs`, `AssemblyInfo.cs`, `*.g.cs`, `*.Designer.cs`, `Migrations/**`. Files that declare only interfaces, enums or delegates are skipped automatically. Add to the list only with the user's approval; if a file has behaviour worth keeping, test it instead.

## How it is enforced

`tests.cs structure` checks every `.cs` file changed against the default branch (the Stop hook: against `HEAD`). For each one it resolves the owning source project, then reports the exact path of anything missing:

```
missing test project  tests/Domain.Tests/Domain.Tests.csproj
missing test file     tests/Domain.Tests/Clients/ClientTests.cs  (for src/Domain/Clients/Client.cs)
missing integration   tests/Api.IntegrationTests/Api.IntegrationTests.csproj  (Api is a web project)
```

The hook blocks until each gap is filled with a real test, not an empty file: the unit tests and the coverage gate then run on the same change.

## Creating the missing pieces

1. Create the project from [../examples/TestProject.csproj.example](../examples/TestProject.csproj.example) at the mirror path, referencing only its source project, and add it to the solution (`dotnet sln add`).
2. Create the test file at the mirror path with the first Given_When_Then test (see [test-pattern.md](test-pattern.md)).
3. Add the remaining behaviours until the coverage gate passes.

Test files do not write `using Xunit;`, `using AwesomeAssertions;` or `using NSubstitute;`: the csproj declares them as plain `<Using Include="..." />` items in one `ItemGroup`, one line per library the project references. Keep it that simple: no MSBuild conditions or item-order tricks.

## Existing repositories

The hook only checks files you change, so legacy code is not flagged until it is touched. When you edit a legacy file, add its mirror test file in the same change. Do not bulk-generate empty test files.

## Why this structure

Findability (the test for `X` is always at one known path), clear ownership (a test project's coverage and mutation score map to one source project, so Stryker and coverlet run per project), and enforceability (the mapping is purely by name, so a script can verify it).
