# Quality gate

## Thresholds

Stored in [../quality-gate.json](../quality-gate.json); never lowered without the user's approval.

| Metric | Default | Checked by |
|--------|---------|------------|
| Mirror project and file structure | required | Stop hook, `structure`, `all` |
| All tests pass | required | Stop hook, `all` |
| Line coverage (changed files) | 90% | Stop hook, `coverage` |
| Branch coverage (changed files) | 85% | Stop hook, `coverage` |
| Mutation score (changed files) | 80% | `mutation`, `quality` (on demand) |
| Coverage and mutation agree (no file passes one but fails the other) | required | `quality` (on demand) |

## Definition of done

- [ ] Mirror structure holds (`tests.cs structure` passes): a `<P>.Tests` project per source project, a `<Name>Tests.cs` per source file, `<P>.IntegrationTests` for web/API projects.
- [ ] Every new or changed behaviour has a test; bug fixes have a regression test that failed before the fix.
- [ ] Tests follow AAA and Given_When_Then, one behaviour each, AwesomeAssertions only.
- [ ] Unit tests are fast and deterministic; integration tests only where needed and tagged.
- [ ] `all` passes (tests + coverage gate).
- [ ] `quality` passes: every changed file meets line, branch and mutation gates; every remaining survivor is explained (equivalent mutant) in the report.
- [ ] No skipped tests, no coverage excludes added, no thresholds changed.
- [ ] `dotnet-lint` is clean on the test code too.
- [ ] Report printed.

## Test smells (fix when found)

| Smell | Fix |
|-------|-----|
| No assertion, or only `NotBeNull` | Assert the concrete outcome |
| Several Acts / several behaviours | Split |
| Logic (`if`, loops) or recomputed expectations | Literal expected values, `[Theory]` |
| `Arg.Any` on the argument that matters | `Arg.Is` |
| Mocks of own types or value objects | Use the real thing |
| `Thread.Sleep`, real clock, real network | Fake time/IO, await signals |
| Order dependence, shared static state | Isolate per test |
| Giant arrange | Builder with defaults |
| Test mirrors the implementation | Test the observable contract |
| Name says nothing | Rename to Given_When_Then |

## Flaky tests

Never retry blindly or add sleeps. Reproduce (loop the test), find the shared state, race or time dependency, fix it. If it cannot be fixed now, tell the user and propose a quarantine (`[Trait("Category", "Quarantine")]` plus an issue); never silently skip.

## Parallelism

xunit runs test classes in parallel. Keep per-test state local, fixtures read-only, and give integration tests unique data. Use a `[Collection]` only as a last resort for a truly shared resource.

## CI

```yaml
- uses: actions/setup-dotnet@v4
  with: { global-json-file: global.json }
- run: dotnet tool install -g dotnet-reportgenerator-globaltool
- run: dotnet run .claude/skills/dotnet-tests/scripts/tests.cs -- all
- run: dotnet tool restore
- run: dotnet run .claude/skills/dotnet-tests/scripts/tests.cs -- quality
- uses: actions/upload-artifact@v4
  with: { name: test-reports, path: artifacts/dotnet-tests }
```

For `mutation` in CI, fetch full history (`fetch-depth: 0`) so `--since` can diff against the base branch.

## Report

`tests.cs --report` prints `artifacts/dotnet-tests/report.md`: tests added or changed, run results, coverage versus gate per changed file, mutation score and survivors, and "Decisions needed" (equivalent mutants, approved excludes, missing tooling).
