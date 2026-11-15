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
//   tests.cs unit                 run the unit tests (everything not tagged Category=Integration)
//   tests.cs integration          run the integration tests (Category=Integration); checks Docker first
//   tests.cs coverage             run the unit tests with coverage and evaluate the gate on the changed files
//   tests.cs mutation             run Stryker.NET on the files changed versus the default branch
//   tests.cs all                  unit + coverage (what the Stop hook runs)
//   tests.cs --report             write and print artifacts/dotnet-tests/report.md
//   tests.cs hook stop            Stop hook: reads the hook JSON from stdin, writes the decision JSON to stdout
//
// Options: --base <branch> (default branch auto-detected; the hook always uses HEAD).
// Exit codes: 0 = pass, 1 = tests or gate failed, 2 = tooling/usage error.

return TestRunner.Run(args);

sealed class ToolException(string message) : Exception(message);

sealed record Gate(double LineCoverage, double BranchCoverage, double MutationScore, string IntegrationTrait);

sealed record FileCoverage(string File, int Lines, int LinesCovered, int Branches, int BranchesCovered, List<int> Uncovered)
{
    public double LineRate => Lines == 0 ? 100 : 100.0 * LinesCovered / Lines;

    public double BranchRate => Branches == 0 ? 100 : 100.0 * BranchesCovered / Branches;
}

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
                "unit" => RunTests(integration: false),
                "integration" => RunTests(integration: true),
                "coverage" => RunCoverage(gate, baseRef),
                "mutation" => RunMutation(gate, baseRef),
                "all" => Combine(RunTests(integration: false), () => RunCoverage(gate, baseRef)),
                "--report" => WriteReport(),
                _ => throw new ToolException("usage: unit | integration | coverage | mutation | all | --report | hook stop"),
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
        return new Gate(r.GetProperty("lineCoverage").GetDouble(), r.GetProperty("branchCoverage").GetDouble(), r.GetProperty("mutationScore").GetDouble(), r.GetProperty("integrationTrait").GetString() ?? "Integration");
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

    static List<string> TestProjects() =>
        ProjectFiles().Where(p => Regex.IsMatch(File.ReadAllText(p), "Microsoft\\.NET\\.Test\\.Sdk|xunit", RegexOptions.IgnoreCase)).ToList();

    // Changed .cs files (committed since <baseRef>, staged, unstaged, untracked), minus files that live in a test project.
    static List<string> ChangedSourceFiles(string baseRef, List<string> testProjects)
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

        var testDirs = testProjects.Select(p => Path.GetDirectoryName(p)! + Path.DirectorySeparatorChar).ToList();
        return names
            .Where(n => n.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(n => Path.GetFullPath(n))
            .Where(File.Exists)
            .Where(n => !testDirs.Any(d => n.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
            .Where(n => !Regex.IsMatch(n, "[\\\\/](bin|obj|Migrations)[\\\\/]|\\.g\\.cs$|\\.Designer\\.cs$"))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
        var projects = TestProjects();
        if (projects.Count == 0)
        {
            throw new ToolException("no test project found (a .csproj referencing Microsoft.NET.Test.Sdk or xunit). Create one from examples/TestProject.csproj.example.");
        }

        var trait = LoadGate().IntegrationTrait;
        if (integration)
        {
            var docker = TryExec("docker", "info");
            if (docker != 0)
            {
                throw new ToolException("Docker is not available (`docker info` failed); integration tests that use Testcontainers need it.");
            }
        }

        var filter = integration ? "--filter-trait" : "--filter-not-trait";
        var exitCode = 0;
        var failures = new List<string>();
        foreach (var project in projects)
        {
            var (exit, output) = ExecLogged(integration ? "integration.log" : "unit.log", "dotnet", ["test", "--project", project, filter, $"Category={trait}"]);
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

    static (bool Passed, List<FileCoverage> Failing, List<string> Messages) EvaluateCoverage(Gate gate, string baseRef, string resultsDir)
    {
        var projects = TestProjects();
        if (projects.Count == 0)
        {
            throw new ToolException("no test project found; cannot measure coverage.");
        }

        if (Directory.Exists(resultsDir))
        {
            Directory.Delete(resultsDir, recursive: true);
        }

        foreach (var project in projects)
        {
            List<string> arguments =
            [
                "test", "--project", project, "--filter-not-trait", $"Category={gate.IntegrationTrait}",
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
                return (false, [], ["tests failed before coverage could be evaluated"]);
            }
        }

        var reports = Directory.Exists(resultsDir) ? Directory.GetFiles(resultsDir, "*.cobertura*.xml", SearchOption.AllDirectories) : [];
        if (reports.Length == 0)
        {
            throw new ToolException("no Cobertura report was produced; add the coverlet.MTP package to the test project and opt in to Microsoft.Testing.Platform (see references/coverage.md).");
        }

        var coverage = ParseCobertura(reports);
        var changed = ChangedSourceFiles(baseRef, projects);
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
        return (failing.Count == 0, failing, messages);
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

    static int RunMutation(Gate gate, string baseRef)
    {
        var projects = TestProjects();
        if (projects.Count == 0)
        {
            throw new ToolException("no test project found; cannot run mutation tests.");
        }

        if (TryExec("dotnet", "stryker", "--version") != 0)
        {
            throw new ToolException("dotnet-stryker is not installed (dotnet tool install -g dotnet-stryker).");
        }

        if (ChangedSourceFiles(baseRef, projects).Count == 0)
        {
            Out.WriteLine("No changed source files to mutate.");
            SaveSection("mutation", new { passed = true, note = "nothing to mutate" });
            return 0;
        }

        var exitCode = 0;
        foreach (var project in projects)
        {
            var dir = Path.GetDirectoryName(project)!;
            var arguments = new List<string> { "stryker", "--since:" + baseRef, "--threshold-break", gate.MutationScore.ToString("F0"), "--output", Path.GetFullPath($"{StateDir}/mutation") };
            if (File.Exists(Path.Combine(dir, "stryker-config.json")))
            {
                arguments.AddRange(["--config-file", "stryker-config.json"]);
            }

            var (exit, output) = ExecLogged("mutation.log", "dotnet", arguments, dir);
            var score = Regex.Match(output, "mutation score[^\\d]*(\\d+(\\.\\d+)?)", RegexOptions.IgnoreCase);
            Out.WriteLine($"{(exit == 0 ? "PASS" : "FAIL")} {Path.GetFileName(project)} mutation score {(score.Success ? score.Groups[1].Value + "%" : "n/a")} (gate {gate.MutationScore}%)");
            SaveSection("mutation", new { passed = exit == 0, project = Path.GetFileName(project), score = score.Success ? score.Groups[1].Value : null, gate = gate.MutationScore });
            exitCode |= exit == 0 ? 0 : 1;
        }

        return exitCode;
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
        foreach (var name in new[] { "unit", "integration", "coverage", "mutation" })
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
            var projects = TestProjects();
            var changed = projects.Count == 0 ? [] : ChangedSourceFiles("HEAD", projects);
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
                var unitExit = RunTests(integration: false);
                if (unitExit == 0)
                {
                    var result = EvaluateCoverage(LoadGate(), "HEAD", $"{StateDir}/hook/results");
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
            "dotnet-tests: the changed code is not verified. The turn cannot end until the unit tests pass and each changed file meets the coverage gate.",
            "Follow the dotnet-tests skill (.claude/skills/dotnet-tests/SKILL.md) from workflow step 3: fix failing tests, then add tests for the uncovered lines and branches below.",
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
