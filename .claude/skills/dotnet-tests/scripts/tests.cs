#:property PublishAot=false

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Deterministic test runner and quality gate for a .NET repository, and the Stop hook that enforces it.
// Single-file C# app: `dotnet run tests.cs -- <command>` (any OS, only the .NET SDK is required).
//
//   tests.cs structure            check that every changed source file has its mirror test project and file
//   tests.cs unit                 run the unit tests (everything not tagged Category=Integration)
//   tests.cs integration          run the integration tests (Category=Integration); checks Docker first
//   tests.cs coverage             run the unit tests with coverage and evaluate the gate on the changed files
//   tests.cs quality              coverage + mutation, correlated per file and per surviving mutant (artifacts/dotnet-tests/quality.md)
//   tests.cs mutation             run Stryker.NET on the files changed versus the default branch
//   tests.cs all                  unit + coverage (what the Stop hook runs)
//   tests.cs --report             write and print artifacts/dotnet-tests/report.md
//   tests.cs hook stop            Stop hook: reads the hook JSON from stdin, writes the decision JSON to stdout
//
// Options: --base <branch> (default branch auto-detected; the hook always uses HEAD).
// Exit codes: 0 = pass, 1 = tests or gate failed, 2 = tooling/usage error.

return TestRunner.Run(args);

sealed class ToolException(string message) : Exception(message);

sealed record Gate(double LineCoverage, double BranchCoverage, double MutationScore, string UnitSuffix, string IntegrationSuffix, string IntegrationTrait, string[] StructureIgnore, string[] MutationIgnore);

sealed record FileCoverage(string File, int Lines, int LinesCovered, int Branches, int BranchesCovered, List<int> Uncovered)
{
    public double LineRate => Lines == 0 ? 100 : 100.0 * LinesCovered / Lines;

    public double BranchRate => Branches == 0 ? 100 : 100.0 * BranchesCovered / Branches;
}

sealed record Mutant(string File, int Line, string Mutator, string Status, string Replacement);

sealed record HookState(string Verified, string Waived, int Blocks);

static class TestRunner
{
    const int MaxConsecutiveBlocks = 3;
    const string StateDir = "artifacts/dotnet-tests";
    const string SelfCommand = "dotnet run .claude/skills/dotnet-tests/scripts/tests.cs --";

    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    static string ScriptDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    static TextWriter Out = Console.Out;

    public static int Run(string[] args)
    {
        try
        {
            if (args.Length >= 2 && args[0] == "hook")
            {
                return args[1] == "stop" ? HookStop() : throw new ToolException("unknown hook. Supported: hook stop.");
            }

            var baseBranch = Option(args, "--base");
            var command = args.FirstOrDefault(a => !a.StartsWith("--") && a != baseBranch)?.ToLowerInvariant() ?? (args.Contains("--report") ? "--report" : "all");
            var root = Git("rev-parse", "--show-toplevel") ?? throw new ToolException("not inside a git repository.");
            Directory.SetCurrentDirectory(root.Trim());
            Directory.CreateDirectory(StateDir);
            var gate = LoadGate();
            var baseRef = baseBranch ?? DefaultBase();

            return command switch
            {
                "structure" => RunStructure(gate, baseRef),
                "unit" => RunTests(integration: false),
                "integration" => RunTests(integration: true),
                "coverage" => RunCoverage(gate, baseRef),
                "mutation" => RunMutation(gate, baseRef),
                "quality" => RunQuality(gate, baseRef),
                "all" => Combine(Combine(RunStructure(gate, baseRef), () => RunTests(integration: false)), () => RunCoverage(gate, baseRef)),
                "--report" => WriteReport(),
                _ => throw new ToolException("usage: structure | unit | integration | coverage | mutation | quality | all | --report | hook stop"),
            };
        }
        catch (ToolException e)
        {
            Console.Error.WriteLine($"dotnet-tests: {e.Message}");
            return 2;
        }
    }

    static int Combine(int first, Func<int> second)
    {
        var next = second();
        return first == 2 || next == 2 ? 2 : first | next;
    }

    // ---------------------------------------------------------------- Discovery

    static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    static Gate LoadGate()
    {
        var path = Path.Combine(ScriptDir(), "..", "quality-gate.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var r = document.RootElement;
        return new Gate(
            r.GetProperty("lineCoverage").GetDouble(),
            r.GetProperty("branchCoverage").GetDouble(),
            r.GetProperty("mutationScore").GetDouble(),
            r.GetProperty("unitSuffix").GetString() ?? ".Tests",
            r.GetProperty("integrationSuffix").GetString() ?? ".IntegrationTests",
            r.GetProperty("integrationTrait").GetString() ?? "Integration",
            r.GetProperty("structureIgnore").EnumerateArray().Select(e => e.GetString() ?? "").ToArray(),
            LoadMutationIgnore());
    }

    // Optional per-repository list of source project names (without extension) that mutation testing skips.
    static string[] LoadMutationIgnore()
    {
        const string path = ".config/dotnet-tests.json";
        if (!File.Exists(path))
        {
            return [];
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("mutationIgnore", out var ignored)
            ? ignored.EnumerateArray().Select(e => e.GetString() ?? "").ToArray()
            : [];
    }

    static string DefaultBase()
    {
        foreach (var candidate in new[] { "origin/HEAD", "main", "master" })
        {
            if (Git("rev-parse", "--verify", "--quiet", candidate) is not null)
            {
                var mergeBase = Git("merge-base", "HEAD", candidate)?.Trim();
                if (!string.IsNullOrEmpty(mergeBase))
                {
                    return mergeBase;
                }
            }
        }

        return "HEAD";
    }

    static readonly string[] IgnoredFolders = ["bin", "obj", "node_modules", "artifacts", ".git", ".claude"];

    static IEnumerable<string> ProjectFiles()
    {
        foreach (var file in Directory.EnumerateFiles(".", "*.csproj", SearchOption.AllDirectories))
        {
            var parts = Path.GetRelativePath(".", file).Split(Path.DirectorySeparatorChar);
            if (!parts.Any(p => IgnoredFolders.Contains(p)))
            {
                yield return Path.GetFullPath(file);
            }
        }
    }

    static List<string> UnitProjects() =>
        ProjectFiles().Where(p => Path.GetFileNameWithoutExtension(p).EndsWith(LoadGate().UnitSuffix, StringComparison.OrdinalIgnoreCase)).ToList();

    static List<string> IntegrationProjects() =>
        ProjectFiles().Where(p => Path.GetFileNameWithoutExtension(p).EndsWith(LoadGate().IntegrationSuffix, StringComparison.OrdinalIgnoreCase)).ToList();

    static List<string> AllTestProjects() => UnitProjects().Concat(IntegrationProjects()).ToList();

    // Source projects live under a top-level `src` folder and are not test projects (benchmarks and tools elsewhere are out of scope).
    static List<string> SourceProjects()
    {
        var tests = AllTestProjects();
        var src = Path.GetFullPath("src") + Path.DirectorySeparatorChar;
        return ProjectFiles().Where(p => !tests.Contains(p) && p.StartsWith(src, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    // The source project that owns a file: the nearest ancestor folder holding a source .csproj.
    static string? OwningProject(string file, List<string> sources)
    {
        for (var dir = Path.GetDirectoryName(file); !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
        {
            var match = sources.FirstOrDefault(p => string.Equals(Path.GetDirectoryName(p), dir, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    // Changed .cs files (committed since <baseRef>, staged, unstaged, untracked) that belong to a source project.
    static List<string> ChangedSourceFiles(string baseRef)
    {
        var names = new HashSet<string>();
        foreach (var line in (Git("diff", "--name-only", "--diff-filter=d", baseRef) ?? "").Split('\n'))
        {
            names.Add(line.Trim());
        }

        foreach (var line in (Git("ls-files", "--others", "--exclude-standard") ?? "").Split('\n'))
        {
            names.Add(line.Trim());
        }

        var sources = SourceProjects();
        return names
            .Where(n => n.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(n => Path.GetFullPath(n))
            .Where(File.Exists)
            .Where(n => OwningProject(n, sources) is not null)
            .Where(n => !Regex.IsMatch(n, "[\\\\/](bin|obj|Migrations)[\\\\/]|\\.g\\.cs$|\\.Designer\\.cs$"))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ---------------------------------------------------------------- Structure (mirror projects and files)

    static bool IsIgnored(string relative, string[] patterns)
    {
        var path = relative.Replace('\\', '/');
        foreach (var pattern in patterns)
        {
            var regex = "^" + Regex.Escape(pattern).Replace("\\*\\*", "\u0000").Replace("\\*", "[^/]*").Replace("\u0000", ".*") + "$";
            var target = pattern.Contains('/') || pattern.EndsWith("/**", StringComparison.Ordinal) ? path : Path.GetFileName(path);
            if (Regex.IsMatch(target, regex) || (pattern.EndsWith("/**", StringComparison.Ordinal) && path.StartsWith(pattern[..^2], StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    // Interface, enum and delegate only files carry no behaviour to test.
    static bool DeclaresNoBehaviour(string file)
    {
        var text = Regex.Replace(File.ReadAllText(file), "//.*|/\\*[\\s\\S]*?\\*/", "");
        return !Regex.IsMatch(text, "\\b(class|struct|record)\\s+\\w");
    }

    static List<string> CheckStructure(Gate gate, string baseRef)
    {
        var sources = SourceProjects();
        var units = UnitProjects();
        var integrations = IntegrationProjects();
        var findings = new List<string>();
        var reported = new HashSet<string>();
        foreach (var file in ChangedSourceFiles(baseRef))
        {
            var project = OwningProject(file, sources)!;
            var name = Path.GetFileNameWithoutExtension(project);
            var relative = Path.GetRelativePath(Path.GetDirectoryName(project)!, file);
            if (IsIgnored(relative, gate.StructureIgnore) || DeclaresNoBehaviour(file))
            {
                continue;
            }

            var unit = units.FirstOrDefault(p => string.Equals(Path.GetFileNameWithoutExtension(p), name + gate.UnitSuffix, StringComparison.OrdinalIgnoreCase));
            var unitDir = unit is null ? Path.Combine(Path.GetFullPath("tests"), name + gate.UnitSuffix) : Path.GetDirectoryName(unit)!;
            if (unit is null && reported.Add("unit:" + name))
            {
                findings.Add($"missing test project  {Rel(Path.Combine(unitDir, name + gate.UnitSuffix + ".csproj"))}  (for {Rel(project)})");
            }

            var expected = Path.Combine(unitDir, Path.GetDirectoryName(relative) ?? "", Path.GetFileNameWithoutExtension(file) + "Tests.cs");
            if (!File.Exists(expected))
            {
                findings.Add($"missing test file     {Rel(expected)}  (for {Rel(file)})");
            }

            var text = File.ReadAllText(project);
            var isWeb = text.Contains("Microsoft.NET.Sdk.Web", StringComparison.Ordinal) || text.Contains("Microsoft.AspNetCore", StringComparison.Ordinal);
            var hasIntegration = integrations.Any(p => string.Equals(Path.GetFileNameWithoutExtension(p), name + gate.IntegrationSuffix, StringComparison.OrdinalIgnoreCase));
            if (isWeb && !hasIntegration && reported.Add("integration:" + name))
            {
                findings.Add($"missing integration   {Rel(Path.Combine(Path.GetFullPath("tests"), name + gate.IntegrationSuffix, name + gate.IntegrationSuffix + ".csproj"))}  ({name} is a web project)");
            }
        }

        return findings;
    }

    static string Rel(string path) => Path.GetRelativePath(".", path).Replace('\\', '/');

    static int RunStructure(Gate gate, string baseRef)
    {
        var findings = CheckStructure(gate, baseRef);
        findings.ForEach(Out.WriteLine);
        Out.WriteLine(findings.Count == 0 ? "PASS structure (every changed source file has its mirror test file)" : $"FAIL structure: {findings.Count} gap(s); see references/project-structure.md");
        SaveSection("structure", new { passed = findings.Count == 0, findings });
        return findings.Count == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------- Process helpers

    static string? Git(params string[] arguments)
    {
        try
        {
            var (exit, output) = Exec("git", arguments);
            return exit == 0 ? output : null;
        }
        catch (ToolException)
        {
            return null;
        }
    }

    static (int Exit, string Output) Exec(string exe, IEnumerable<string> arguments, string? workingDirectory = null)
    {
        var info = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (workingDirectory is not null)
        {
            info.WorkingDirectory = workingDirectory;
        }

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        var text = new StringBuilder();
        try
        {
            using var process = new Process { StartInfo = info };
            process.OutputDataReceived += (_, e) => { lock (text) { text.AppendLine(e.Data); } };
            process.ErrorDataReceived += (_, e) => { lock (text) { text.AppendLine(e.Data); } };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return (process.ExitCode, text.ToString());
        }
        catch (System.ComponentModel.Win32Exception)
        {
            throw new ToolException($"'{exe}' was not found on PATH.");
        }
    }

    static (int Exit, string Output) ExecLogged(string logName, string exe, IEnumerable<string> arguments, string? workingDirectory = null)
    {
        var result = Exec(exe, arguments, workingDirectory);
        File.WriteAllText(Path.Combine(StateDir, logName), result.Output, new UTF8Encoding(false));
        return result;
    }

    // ---------------------------------------------------------------- Unit / integration

    static int RunTests(bool integration)
    {
        var gate = LoadGate();
        var projects = integration ? IntegrationProjects() : UnitProjects();
        if (projects.Count == 0)
        {
            throw new ToolException($"no {(integration ? "integration" : "unit")} test project found (expected a *{(integration ? gate.IntegrationSuffix : gate.UnitSuffix)}.csproj). See references/project-structure.md.");
        }

        if (integration)
        {
            var docker = TryExec("docker", "info");
            if (docker != 0)
            {
                throw new ToolException("Docker is not available (`docker info` failed); integration tests that use Testcontainers need it.");
            }
        }

        var exitCode = 0;
        var failures = new List<string>();
        foreach (var project in projects)
        {
            var (exit, output) = ExecLogged(integration ? "integration.log" : "unit.log", "dotnet", ["test", "--project", project]);
            Out.WriteLine($"{(exit == 0 ? "PASS" : "FAIL")} {Path.GetFileName(project)} ({(integration ? "integration" : "unit")})");
            if (exit != 0)
            {
                exitCode = 1;
                failures.AddRange(output.Split('\n').Where(l => Regex.IsMatch(l, "^\\s*(Failed|error)\\b", RegexOptions.IgnoreCase)).Select(l => l.Trim()).Take(20));
            }
        }

        SaveSection(integration ? "integration" : "unit", new { passed = exitCode == 0, failures });
        foreach (var failure in failures)
        {
            Out.WriteLine($"  {failure}");
        }

        return exitCode;
    }

    static int TryExec(string exe, params string[] arguments)
    {
        try
        {
            return Exec(exe, arguments).Exit;
        }
        catch (ToolException)
        {
            return -1;
        }
    }

    // ---------------------------------------------------------------- Coverage

    static int RunCoverage(Gate gate, string baseRef) => EvaluateCoverage(gate, baseRef, $"{StateDir}/results").Passed ? 0 : 1;

    static (bool Passed, List<FileCoverage> Failing, List<string> Messages, List<FileCoverage> All) EvaluateCoverage(Gate gate, string baseRef, string resultsDir)
    {
        var projects = UnitProjects();
        if (projects.Count == 0)
        {
            throw new ToolException($"no unit test project found (expected a *{gate.UnitSuffix}.csproj); cannot measure coverage.");
        }

        if (Directory.Exists(resultsDir))
        {
            Directory.Delete(resultsDir, recursive: true);
        }

        foreach (var project in projects)
        {
            List<string> arguments =
            [
                "test", "--project", project,
                "--coverlet", "--coverlet-output-format", "cobertura", "--coverlet-file-prefix", Path.GetFileNameWithoutExtension(project),
                "--coverlet-exclude-by-file", "**/Migrations/**", "**/*.g.cs", "**/obj/**",
                "--coverlet-exclude-by-attribute", "GeneratedCode", "CompilerGenerated",
                "--results-directory", Path.GetFullPath(resultsDir),
            ];
            var (exit, output) = ExecLogged("coverage.log", "dotnet", arguments);
            if (exit != 0)
            {
                var failed = output.Split('\n').Where(l => Regex.IsMatch(l, "^\\s*(Failed|error)\\b", RegexOptions.IgnoreCase)).Select(l => l.Trim()).Take(20).ToList();
                SaveSection("coverage", new { passed = false, reason = "tests failed", failed });
                failed.ForEach(Out.WriteLine);
                return (false, [], ["tests failed before coverage could be evaluated"], []);
            }
        }

        var reports = Directory.Exists(resultsDir) ? Directory.GetFiles(resultsDir, "*.cobertura*.xml", SearchOption.AllDirectories) : [];
        if (reports.Length == 0)
        {
            throw new ToolException("no Cobertura report was produced; add the coverlet.MTP package to the test project and opt in to Microsoft.Testing.Platform (see references/coverage.md).");
        }

        var coverage = ParseCobertura(reports);
        var changed = ChangedSourceFiles(baseRef);
        var messages = new List<string>();
        var failing = new List<FileCoverage>();
        foreach (var file in changed)
        {
            var match = coverage.FirstOrDefault(c => SamePath(c.File, file));
            if (match is null)
            {
                messages.Add($"no executable code measured in {Path.GetRelativePath(".", file)} (interface, enum or not referenced by a test project)");
                continue;
            }

            var ok = match.LineRate >= gate.LineCoverage && match.BranchRate >= gate.BranchCoverage;
            Out.WriteLine($"{(ok ? "PASS" : "FAIL")} {Path.GetRelativePath(".", file)}  line {match.LineRate:F1}% (gate {gate.LineCoverage}%)  branch {match.BranchRate:F1}% (gate {gate.BranchCoverage}%)");
            if (!ok)
            {
                failing.Add(match);
                Out.WriteLine($"     uncovered lines: {Ranges(match.Uncovered)}");
            }
        }

        messages.ForEach(m => Out.WriteLine($"note: {m}"));
        var total = coverage.Sum(c => c.Lines);
        Out.WriteLine($"solution line coverage (information only): {(total == 0 ? 100 : 100.0 * coverage.Sum(c => c.LinesCovered) / total):F1}%");

        TryReportGenerator(reports);
        SaveSection("coverage", new { passed = failing.Count == 0, gate, files = changed.Count, failing = failing.Select(f => new { file = Path.GetRelativePath(".", f.File), f.LineRate, f.BranchRate, uncovered = Ranges(f.Uncovered) }), messages });
        return (failing.Count == 0, failing, messages, coverage);
    }

    static bool SamePath(string reported, string absolute)
    {
        var a = reported.Replace('\\', '/').TrimStart('.', '/');
        var b = absolute.Replace('\\', '/');
        return b.EndsWith(a, StringComparison.OrdinalIgnoreCase) || a.EndsWith(b, StringComparison.OrdinalIgnoreCase);
    }

    static List<FileCoverage> ParseCobertura(string[] reports)
    {
        // file -> line -> (hits, branchesCovered, branchesTotal); merged across reports by taking the best result.
        var files = new Dictionary<string, Dictionary<int, (int Hits, int Covered, int Total)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var report in reports)
        {
            var document = XDocument.Load(report);
            var sources = document.Descendants("source").Select(s => s.Value).ToList();
            foreach (var cls in document.Descendants("class"))
            {
                var fileName = (string?)cls.Attribute("filename") ?? "";
                var resolved = Path.IsPathRooted(fileName) ? fileName : sources.Select(s => Path.Combine(s, fileName)).FirstOrDefault(File.Exists) ?? fileName;
                if (!files.TryGetValue(resolved, out var lines))
                {
                    files[resolved] = lines = [];
                }

                foreach (var line in cls.Element("lines")?.Elements("line") ?? [])
                {
                    var number = (int)line.Attribute("number")!;
                    var hits = (int)line.Attribute("hits")!;
                    int covered = 0, total = 0;
                    var condition = (string?)line.Attribute("condition-coverage");
                    var m = condition is null ? null : Regex.Match(condition, "\\((\\d+)/(\\d+)\\)");
                    if (m is { Success: true })
                    {
                        covered = int.Parse(m.Groups[1].Value);
                        total = int.Parse(m.Groups[2].Value);
                    }

                    lines[number] = lines.TryGetValue(number, out var old)
                        ? (Math.Max(old.Hits, hits), Math.Max(old.Covered, covered), Math.Max(old.Total, total))
                        : (hits, covered, total);
                }
            }
        }

        return files.Select(f => new FileCoverage(
            f.Key,
            f.Value.Count,
            f.Value.Count(l => l.Value.Hits > 0),
            f.Value.Sum(l => l.Value.Total),
            f.Value.Sum(l => l.Value.Covered),
            f.Value.Where(l => l.Value.Hits == 0).Select(l => l.Key).Order().ToList())).ToList();
    }

    static string Ranges(List<int> lines)
    {
        var parts = new List<string>();
        for (var i = 0; i < lines.Count; i++)
        {
            var start = lines[i];
            while (i + 1 < lines.Count && lines[i + 1] == lines[i] + 1)
            {
                i++;
            }

            parts.Add(start == lines[i] ? $"{start}" : $"{start}-{lines[i]}");
        }

        return string.Join(", ", parts);
    }

    static void TryReportGenerator(string[] reports)
    {
        if (TryExec("reportgenerator", "-help") == -1)
        {
            Out.WriteLine("note: reportgenerator is not installed; skipping the HTML report (dotnet tool install -g dotnet-reportgenerator-globaltool).");
            return;
        }

        ExecLogged("reportgenerator.log", "reportgenerator", [$"-reports:{string.Join(";", reports)}", $"-targetdir:{StateDir}/coverage", "-reporttypes:TextSummary;Html;Cobertura"]);
    }

    // ---------------------------------------------------------------- Mutation

    static int RunMutation(Gate gate, string baseRef) => MutationRun(gate, baseRef).Exit;

    static (int Exit, List<string> Reports) MutationRun(Gate gate, string baseRef)
    {
        var projects = UnitProjects();
        if (projects.Count == 0)
        {
            throw new ToolException($"no unit test project found (expected a *{gate.UnitSuffix}.csproj); cannot run mutation tests.");
        }

        var manifest = new[] { ".config/dotnet-tools.json", "dotnet-tools.json" }.FirstOrDefault(File.Exists);
        if (manifest is null || !File.ReadAllText(manifest).Contains("dotnet-stryker", StringComparison.Ordinal))
        {
            throw new ToolException("dotnet-stryker is not in a local tool manifest. Copy examples/dotnet-tools.json to .config/dotnet-tools.json (see references/mutation-tests.md).");
        }

        if (!File.Exists("stryker-config.json"))
        {
            throw new ToolException("stryker-config.json not found at the repository root. Copy examples/stryker-config.json (see references/mutation-tests.md).");
        }

        var missingScripts = new[] { "mutation-tests.sh", "mutation-tests.bat" }.Where(script => !File.Exists(script)).ToList();
        if (missingScripts.Count > 0)
        {
            throw new ToolException($"{string.Join(" and ", missingScripts)} not found at the repository root. Copy examples/{string.Join(" and examples/", missingScripts)} to the root (see references/mutation-tests.md).");
        }

        // Stryker runs in solution context (it rejects --test-project there), so a solution file must sit at the root.
        if (!Directory.EnumerateFiles(".", "*.sln*", SearchOption.TopDirectoryOnly).Any())
        {
            throw new ToolException("no solution file (.sln or .slnx) at the repository root; Stryker needs it.");
        }

        if (ChangedSourceFiles(baseRef).Count == 0)
        {
            Out.WriteLine("No changed source files to mutate.");
            SaveSection("mutation", new { passed = true, note = "nothing to mutate" });
            return (0, []);
        }

        if (Exec("dotnet", ["tool", "restore"]).Exit != 0)
        {
            throw new ToolException("`dotnet tool restore` failed; check .config/dotnet-tools.json and the NuGet source.");
        }

        // Stryker's --since needs a branch, tag or commit SHA ("HEAD" is rejected).
        var since = Git("rev-parse", baseRef)?.Trim() ?? baseRef;
        var threshold = gate.MutationScore.ToString("F0");
        var high = Math.Max(90, gate.MutationScore).ToString("F0");
        var exitCode = 0;
        var reports = new List<string>();
        foreach (var project in projects)
        {
            var ignored = gate.MutationIgnore.Contains(Path.GetFileNameWithoutExtension(project)[..^gate.UnitSuffix.Length], StringComparer.OrdinalIgnoreCase);
            if (ignored)
            {
                Out.WriteLine($"SKIP {Path.GetFileNameWithoutExtension(project)} (listed in mutationIgnore of .config/dotnet-tests.json)");
                continue;
            }

            var source = Path.GetFileNameWithoutExtension(project)[..^gate.UnitSuffix.Length] + ".csproj";
            var outDir = Path.GetFullPath($"{StateDir}/mutation/{Path.GetFileNameWithoutExtension(source)}");
            reports.Add(Path.Combine(outDir, "reports", "mutation-report.json"));
            List<string> arguments =
            [
                "stryker", "--config-file", "stryker-config.json", "--project", source, $"--since:{since}",
                "--break-at", threshold, "--threshold-low", threshold, "--threshold-high", high,
                "--output", outDir,
            ];
            var (exit, output) = ExecLogged("mutation.log", "dotnet", arguments);
            var score = Regex.Match(output, "final mutation score is (\\d+(\\.\\d+)?)", RegexOptions.IgnoreCase);
            Out.WriteLine($"{(exit == 0 ? "PASS" : "FAIL")} {source} mutation score {(score.Success ? score.Groups[1].Value + "%" : "n/a")} (gate {gate.MutationScore}%)");
            SaveSection("mutation", new { passed = exit == 0, project = source, score = score.Success ? score.Groups[1].Value : null, gate = gate.MutationScore });
            exitCode |= exit == 0 ? 0 : 1;
        }

        return (exitCode, reports);
    }

    // ---------------------------------------------------------------- Quality (coverage x mutation)

    static List<Mutant> LoadMutants(string reportPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
        var mutants = new List<Mutant>();
        foreach (var file in document.RootElement.GetProperty("files").EnumerateObject())
        {
            foreach (var m in file.Value.GetProperty("mutants").EnumerateArray())
            {
                var line = m.GetProperty("location").GetProperty("start").GetProperty("line").GetInt32();
                mutants.Add(new Mutant(file.Name, line, JsonString(m, "mutatorName"), JsonString(m, "status"), JsonString(m, "replacement")));
            }
        }

        return mutants;
    }

    // Joins the Stryker report with the coverage data, per file and per surviving mutant, so the gaps are visible:
    // a survivor on an uncovered line needs a test that reaches it; a survivor on a covered line needs a stronger assertion.
    static int RunQuality(Gate gate, string baseRef)
    {
        var coverage = EvaluateCoverage(gate, baseRef, $"{StateDir}/results");
        var (mutationExit, reports) = MutationRun(gate, baseRef);
        var mutants = reports.Where(File.Exists).SelectMany(LoadMutants).ToList();

        var report = new List<string>
        {
            "# Test quality: coverage x mutation",
            "",
            $"Gates: line {gate.LineCoverage}%, branch {gate.BranchCoverage}%, mutation {gate.MutationScore}%.",
            "",
            "| File | Line | Branch | Mutation | Killed | Survived | No coverage | Verdict |",
            "|------|------|--------|----------|--------|----------|-------------|---------|",
        };
        var details = new List<string>();
        var failed = !coverage.Passed || mutationExit != 0;
        var rows = new List<object>();
        foreach (var group in mutants.GroupBy(m => m.File).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var cov = coverage.All.FirstOrDefault(c => SamePath(c.File, group.Key));
            var detected = group.Count(m => m.Status is "Killed" or "Timeout");
            var survived = group.Count(m => m.Status == "Survived");
            var noCoverage = group.Count(m => m.Status == "NoCoverage");
            var total = detected + survived + noCoverage;
            var score = total == 0 ? 100.0 : 100.0 * detected / total;

            var reasons = new List<string>();
            if (cov is not null && cov.LineRate < gate.LineCoverage)
            {
                reasons.Add("line coverage");
            }

            if (cov is not null && cov.BranchRate < gate.BranchCoverage)
            {
                reasons.Add("branch coverage");
            }

            if (score < gate.MutationScore)
            {
                reasons.Add(cov is not null && cov.LineRate >= gate.LineCoverage ? "mutation score (covered but not verified)" : "mutation score");
            }

            failed |= reasons.Count > 0;
            var name = Rel(group.Key);
            var verdict = reasons.Count == 0 ? "PASS" : "FAIL: " + string.Join(", ", reasons);
            report.Add($"| {name} | {(cov is null ? "n/a" : $"{cov.LineRate:F1}%")} | {(cov is null ? "n/a" : $"{cov.BranchRate:F1}%")} | {score:F1}% | {detected} | {survived} | {noCoverage} | {verdict} |");
            rows.Add(new { file = name, line = cov?.LineRate, branch = cov?.BranchRate, mutation = score, detected, survived, noCoverage, verdict });

            foreach (var mutant in group.Where(m => m.Status is "Survived" or "NoCoverage").OrderBy(m => m.Line))
            {
                var uncovered = mutant.Status == "NoCoverage" || (cov?.Uncovered.Contains(mutant.Line) ?? false);
                var action = uncovered
                    ? "NOT COVERED: add a test that reaches this line"
                    : "COVERED BUT NOT VERIFIED: tighten the assertion so this change fails a test";
                details.Add($"- {name}:{mutant.Line}  {mutant.Mutator}{(mutant.Replacement.Length > 0 ? $" -> `{mutant.Replacement}`" : "")}  {action}");
            }
        }

        if (mutants.Count == 0)
        {
            report.Add("| (no mutants in the changed code) | | | | | | | |");
        }

        report.AddRange(["", $"## Surviving mutants ({details.Count})", ""]);
        report.AddRange(details.Count == 0 ? ["None."] : details);
        report.AddRange(["", "Reading it: a file with high coverage and a low mutation score runs its code without verifying it; fix those first (strengthen assertions). Survivors on uncovered lines need new tests."]);

        File.WriteAllText($"{StateDir}/quality.md", string.Join("\n", report) + "\n", new UTF8Encoding(false));
        report.ForEach(Out.WriteLine);
        SaveSection("quality", new { passed = !failed, files = rows, survivors = details.Count });
        return failed ? 1 : 0;
    }

    // ---------------------------------------------------------------- Report

    static void SaveSection(string name, object value)
    {
        var path = $"{StateDir}/state.json";
        var sections = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path)) ?? []
            : [];
        sections[name] = JsonSerializer.SerializeToElement(value);
        File.WriteAllText(path, JsonSerializer.Serialize(sections, JsonOptions), new UTF8Encoding(false));
    }

    static int WriteReport()
    {
        var path = $"{StateDir}/state.json";
        if (!File.Exists(path))
        {
            throw new ToolException("nothing to report yet: run unit, coverage or mutation first.");
        }

        var sections = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path)) ?? [];
        var report = new StringBuilder("# dotnet-tests report\n\n| Step | Result |\n|------|--------|\n");
        foreach (var name in new[] { "structure", "unit", "integration", "coverage", "mutation", "quality" })
        {
            if (sections.TryGetValue(name, out var s))
            {
                report.AppendLine($"| {name} | {(s.TryGetProperty("passed", out var p) && p.GetBoolean() ? "pass" : "FAIL")} |");
            }
        }

        report.AppendLine().AppendLine("## Details").AppendLine().AppendLine("```json").AppendLine(JsonSerializer.Serialize(sections, JsonOptions)).AppendLine("```");
        report.AppendLine().AppendLine("## Decisions needed").AppendLine().AppendLine("Add equivalent mutants, approved excludes and missing tooling here (edit this file or pass them in the final message).");
        File.WriteAllText($"{StateDir}/report.md", report.ToString(), new UTF8Encoding(false));
        Out.Write(report.ToString());
        return 0;
    }

    // ---------------------------------------------------------------- Stop hook

    static string JsonString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    static int HookStop()
    {
        var active = false;
        string cwd = "";
        var input = Console.IsInputRedirected ? Console.In.ReadToEnd() : "";
        if (input.Trim().Length > 0)
        {
            try
            {
                using var document = JsonDocument.Parse(input);
                var root = document.RootElement;
                foreach (var name in new[] { "stop_hook_active", "stopHookActive" })
                {
                    if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True)
                    {
                        active = true;
                    }
                }

                cwd = JsonString(root, "cwd");
            }
            catch (JsonException)
            {
                // Not a payload we understand: fall back to the process directory.
            }
        }

        if (Environment.GetEnvironmentVariable("DOTNET_TESTS_HOOK")?.ToLowerInvariant() is "off" or "0" or "false")
        {
            return 0;
        }

        cwd = cwd.Length > 0 && Directory.Exists(cwd) ? cwd : Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR") ?? "";
        if (cwd.Length > 0 && Directory.Exists(cwd))
        {
            Directory.SetCurrentDirectory(cwd);
        }

        try
        {
            var root = Git("rev-parse", "--show-toplevel");
            if (root is null)
            {
                return 0;
            }

            Directory.SetCurrentDirectory(root.Trim());
            var projects = AllTestProjects();
            var changed = projects.Count == 0 ? [] : ChangedSourceFiles("HEAD");
            if (changed.Count == 0)
            {
                return 0;
            }

            Directory.CreateDirectory(StateDir);
            var fingerprint = Fingerprint(changed, projects);
            var state = LoadHookState();
            var blocks = active ? state.Blocks : 0;
            if (state.Verified == fingerprint || state.Waived == fingerprint)
            {
                return 0;
            }

            if (active && blocks >= MaxConsecutiveBlocks)
            {
                SaveHookState(state with { Waived = fingerprint, Blocks = 0 });
                Console.Out.Write(JsonSerializer.Serialize(new { systemMessage = $"dotnet-tests: tests or coverage still failing after {MaxConsecutiveBlocks} attempts; stopping anyway. Details: {StateDir}/hook/" }));
                return 0;
            }

            var capture = new StringWriter();
            Out = capture;
            var reason = "";
            try
            {
                var gate = LoadGate();
                var unitExit = RunStructure(gate, "HEAD") == 0 ? RunTests(integration: false) : 1;
                if (unitExit == 0)
                {
                    var result = EvaluateCoverage(gate, "HEAD", $"{StateDir}/hook/results");
                    if (result.Passed)
                    {
                        SaveHookState(new HookState(fingerprint, "", 0));
                        return 0;
                    }
                }

                reason = BuildBlockReason(capture.ToString());
            }
            catch (ToolException e)
            {
                reason = $"dotnet-tests could not verify the changed code: {e.Message}\nFix the environment problem (never lower quality-gate.json), then re-run: {SelfCommand} all";
            }

            SaveHookState(LoadHookState() with { Blocks = blocks + 1 });
            Console.Out.Write(JsonSerializer.Serialize(new { decision = "block", reason }));
            return 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A hook must never trap the agent over its own I/O problem.
            return 0;
        }
    }

    static string BuildBlockReason(string output)
    {
        var lines = output.Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).Take(30);
        return string.Join("\n",
            "dotnet-tests: the changed code is not verified. The turn cannot end until every changed source file has its mirror test file, the unit tests pass and each changed file meets the coverage gate.",
            "Follow the dotnet-tests skill (.claude/skills/dotnet-tests/SKILL.md) from workflow step 1: create the missing mirror test projects and files (references/project-structure.md), fix failing tests, then add tests for the uncovered lines and branches below.",
            "Never lower quality-gate.json, skip tests or add coverage excludes.",
            $"Then re-run until it exits 0: {SelfCommand} all",
            "Output:",
            string.Join("\n", lines));
    }

    static string Fingerprint(List<string> changed, List<string> projects)
    {
        using var sha = SHA256.Create();
        var builder = new StringBuilder();
        foreach (var file in changed.Concat(Directory.EnumerateFiles(".", "*.cs", SearchOption.AllDirectories).Select(Path.GetFullPath).Where(f => projects.Any(p => f.StartsWith(Path.GetDirectoryName(p)!, StringComparison.OrdinalIgnoreCase)) && !Regex.IsMatch(f, "[\\\\/](bin|obj)[\\\\/]"))).Order())
        {
            builder.Append(file).Append(':').Append(Convert.ToHexString(sha.ComputeHash(File.ReadAllBytes(file)))).Append('\n');
        }

        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    static HookState LoadHookState()
    {
        var path = $"{StateDir}/hook-state.json";
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<HookState>(File.ReadAllText(path)) ?? new HookState("", "", 0) : new HookState("", "", 0);
        }
        catch (JsonException)
        {
            return new HookState("", "", 0);
        }
    }

    static void SaveHookState(HookState state)
    {
        Directory.CreateDirectory(StateDir);
        File.WriteAllText($"{StateDir}/hook-state.json", JsonSerializer.Serialize(state), new UTF8Encoding(false));
    }
}
