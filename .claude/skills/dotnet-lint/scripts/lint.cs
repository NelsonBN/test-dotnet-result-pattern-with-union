#:property PublishAot=false

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

// Deterministic build + format analysis for a .NET repository, and the Stop hook that enforces it.
// Single-file C# app: `dotnet run lint.cs -- <arguments>` (any OS, no dependency besides the .NET SDK the repo already needs).
//
//   lint.cs [--scope <changed|all|error|warning|info|deadcode|CODE>] [--reset] [--solution <path>] [--base <branch>]
//              [--out-dir <dir>] [--top <n>]
//   lint.cs --report [--decisions "<item 1>" "<item 2>"]
//   lint.cs hook stop        (Stop / agentStop hook: reads the hook JSON from stdin, writes the decision JSON to stdout)
//
// Exit codes: 0 = no findings in scope, 1 = findings remain in scope, 2 = tooling/usage error.

return CodeHealth.Run(args);

sealed class ToolException(string message) : Exception(message);

sealed record Finding(string Severity, int Rank, string Category, string Code, string File, int Line, int Col, string Message, string Project);

sealed record Candidate(string Kind, string Symbol);

sealed record RunState(string Scope, string Base, string MergeBase, string Solution, string StartedUtc, string DeadCodeCandidates, string[] UnscannedRules);

sealed record HookState(string Verified, string Waived, int Blocks);

sealed class Options
{
    public string Scope { get; set; } = "changed";
    public string? Solution { get; set; }
    public string? Base { get; set; }
    public string OutDir { get; set; } = CodeHealth.StateDir;
    public int Top { get; set; } = 100;
    public List<string> Decisions { get; } = [];
    public bool Reset { get; set; }
    public bool Report { get; set; }
}

sealed class EditorConfigSection(Regex pattern)
{
    public Regex Pattern { get; } = pattern;
    public Dictionary<string, string> Rules { get; } = [];
}

sealed class EditorConfigFile
{
    public bool Root { get; set; }
    public List<EditorConfigSection> Sections { get; } = [];
}

static class CodeHealth
{
    public const string StateDir = "artifacts/dotnet-lint";
    const int MaxConsecutiveBlocks = 3;

    const string Usage = """
        Usage:
          dotnet run <skill-dir>/scripts/lint.cs -- [--scope <changed|all|error|warning|info|deadcode|CODE>] [--reset]
              [--solution <path>] [--base <branch>] [--out-dir <dir>] [--top <n>]
          dotnet run <skill-dir>/scripts/lint.cs -- --report [--decisions "<item 1>" "<item 2>"]
          dotnet run <skill-dir>/scripts/lint.cs -- hook stop
        """;

    static readonly bool IsWin = OperatingSystem.IsWindows();
    static readonly StringComparer PathComparer = IsWin ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    static readonly StringComparison PathComparison = IsWin ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    static readonly UTF8Encoding Utf8NoBom = new(false);
    static readonly string[] FindingHeader = ["Severity", "Rank", "Category", "Code", "File", "Line", "Col", "Message", "Project"];
    static readonly Dictionary<string, int> SeverityOrder = new() { ["error"] = 0, ["warning"] = 1, ["info"] = 2 };

    // The IDE rules are off or hidden in most repos, so they are enabled for one extra build through generated files under
    // the output directory (never by editing .editorconfig). The CS codes are compiler warnings the normal build reports.
    static readonly string[] DeadCodeIdeCodes = ["IDE0005", "IDE0051", "IDE0052", "IDE0060"];
    static readonly string[] DeadCodeCodes = [.. DeadCodeIdeCodes, "CS0168", "CS0169", "CS0219", "CS0414"];

    // Criticality within a severity: lower rank is fixed first. references/severity-order.md documents this table.
    static readonly (int Rank, string Name, Regex Pattern)[] CategoryRules =
    [
        (0, "build", new Regex(@"^(CS|MSB|NU)\d+$")),
        (1, "security", new Regex(@"^CA(3\d{3}|5\d{3}|2(100|109|119|153|3\d{2}|4\d{2}))$")),
        (2, "reliability", new Regex(@"^CA2\d{3}$")),
        (3, "performance", new Regex(@"^CA18\d{2}$")),
        (4, "maintainability", new Regex(@"^CA\d{4}$")),
        (5, "deadcode", new Regex(@"^(IDE0005|IDE0051|IDE0052|IDE0060)$")),
        (6, "style", new Regex(@"^IDE\d{4}$")),
        (8, "format", new Regex(@"^(WHITESPACE|FINALNEWLINE|ENDOFLINE|CHARSET|IMPORTS)$")),
    ];

    static readonly Regex DiagnosticRegex = new(
        @"^\s*(?<file>.+?)(?:\((?<line>\d+)(?:,(?<col>\d+))?(?:,\d+,\d+)?\))?: (?<sev>error|warning|info) (?<code>[A-Za-z]+\d*): (?<msg>.*?)(?: \[(?<proj>[^\]]+)\])?\s*$");

    // Files whose edits can change the analysis result; the Stop hook only reacts to these.
    static readonly Regex RelevantFile = new(
        @"(\.(cs|vb|csproj|vbproj|props|targets|globalconfig|sln|slnx)$)|((^|/)(\.editorconfig|global\.json|nuget\.config)$)",
        RegexOptions.IgnoreCase);

    static TextWriter Out = Console.Out;
    static string RepoRoot = "";
    static Dictionary<string, EditorConfigFile>? editorConfigs;
    static bool deadCodeBuildIncomplete;

    // ---------------------------------------------------------------- entry point

    public static int Run(string[] args)
    {
        try
        {
            Console.OutputEncoding = Utf8NoBom;
        }
        catch (IOException)
        {
            // Keep the default console encoding.
        }

        try
        {
            if (args.Length > 0 && args[0] is "-h" or "--help" or "help")
            {
                Console.WriteLine(Usage);
                return 0;
            }

            if (args.Length > 0 && args[0] == "hook")
            {
                if (args.Length == 2 && args[1] == "stop")
                {
                    return HookStop();
                }

                throw new ToolException("unknown hook. Supported: hook stop.");
            }

            var options = ParseArgs(args);
            InitRepo();
            return options.Report ? ReportMode(options) : RunMode(options);
        }
        catch (ToolException e)
        {
            Console.Error.WriteLine($"dotnet-lint: {e.Message}");
            return 2;
        }
    }

    static Options ParseArgs(string[] args)
    {
        var options = new Options();
        var i = 0;

        string Next(string flag)
        {
            if (i + 1 >= args.Length)
            {
                throw new ToolException($"missing value for {flag}.\n{Usage}");
            }

            return args[++i];
        }

        for (; i < args.Length; i++)
        {
            var flag = args[i];
            switch (flag.ToLowerInvariant())
            {
                case "--scope":
                    options.Scope = Next(flag);
                    break;
                case "--solution":
                    options.Solution = Next(flag);
                    break;
                case "--base":
                    options.Base = Next(flag);
                    break;
                case "--out-dir":
                    options.OutDir = Next(flag);
                    break;
                case "--top":
                    options.Top = int.TryParse(Next(flag), NumberStyles.None, CultureInfo.InvariantCulture, out var top) && top > 0
                        ? top
                        : throw new ToolException("--top expects a positive integer.");
                    break;
                case "--reset":
                    options.Reset = true;
                    break;
                case "--report":
                    options.Report = true;
                    break;
                case "--decisions":
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        options.Decisions.Add(args[++i]);
                    }

                    break;
                default:
                    throw new ToolException($"unknown argument '{flag}'.\n{Usage}");
            }
        }

        return options;
    }

    static void InitRepo()
    {
        var root = Git("rev-parse", "--show-toplevel")?.FirstOrDefault();
        if (string.IsNullOrEmpty(root))
        {
            throw new ToolException("not inside a git repository.");
        }

        RepoRoot = root;
        Directory.SetCurrentDirectory(RepoRoot);
    }

    // ---------------------------------------------------------------- process helpers

    static (int Exit, List<string> Lines) Exec(string exe, IEnumerable<string> arguments, bool mergeStdErr = true)
    {
        var startInfo = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // The diagnostic regex expects the English MSBuild/dotnet output, without terminal-logger formatting.
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["MSBUILDTERMINALLOGGER"] = "off";

        var lines = new List<string>();
        var gate = new object();

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    lock (gate)
                    {
                        lines.Add(e.Data);
                    }
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (mergeStdErr && e.Data is not null)
                {
                    lock (gate)
                    {
                        lines.Add(e.Data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return (process.ExitCode, lines);
        }
        catch (Win32Exception)
        {
            return (127, lines);
        }
    }

    // Returns the stdout lines, or null when git fails.
    static List<string>? Git(params string[] arguments)
    {
        var (exit, lines) = Exec("git", arguments, mergeStdErr: false);
        return exit == 0 ? lines : null;
    }

    // Runs a native command, stores its combined output as UTF-8 and returns its exit code.
    static int ExecLogged(string logPath, string exe, params string[] arguments)
    {
        var (exit, lines) = Exec(exe, arguments);
        WriteText(logPath, lines);
        return exit;
    }

    static void WriteText(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, string.Join("\n", lines) + "\n", Utf8NoBom);
    }

    static string[] ReadLines(string path) => File.ReadAllLines(path, Utf8NoBom);

    static string Json(Action<Utf8JsonWriter> body, bool indented = false)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            body(writer);
        }

        return Utf8NoBom.GetString(stream.ToArray());
    }

    static string JsonString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    static void Warn(string message) => Out.WriteLine($"WARNING: {message}");

    // ---------------------------------------------------------------- diagnostics

    // Repo-relative with '/' on every OS, so findings compare equal wherever they were produced.
    static string RelativePath(string path)
    {
        var normalized = path.Trim().Replace('\\', '/');
        var root = RepoRoot.Replace('\\', '/').TrimEnd('/') + "/";
        return normalized.StartsWith(root, PathComparison) ? normalized[root.Length..] : normalized;
    }

    static (int Rank, string Name) GetCategory(string code)
    {
        foreach (var rule in CategoryRules)
        {
            if (rule.Pattern.IsMatch(code))
            {
                return (rule.Rank, rule.Name);
            }
        }

        return (7, "other");
    }

    static List<Finding> ReadDiagnostics(string logPath, string? forcedSeverity)
    {
        var items = new List<Finding>();
        foreach (var line in ReadLines(logPath))
        {
            var match = DiagnosticRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var code = match.Groups["code"].Value.ToUpperInvariant();
            var (rank, name) = GetCategory(code);
            items.Add(new Finding(
                forcedSeverity ?? match.Groups["sev"].Value,
                rank,
                name,
                code,
                RelativePath(match.Groups["file"].Value),
                match.Groups["line"].Success ? int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture) : 0,
                match.Groups["col"].Success ? int.Parse(match.Groups["col"].Value, CultureInfo.InvariantCulture) : 0,
                Regex.Replace(match.Groups["msg"].Value, @"[\t\r\n]+", " ").Trim(),
                match.Groups["proj"].Success ? RelativePath(match.Groups["proj"].Value) : ""));
        }

        return items;
    }

    static List<Finding> Unique(IEnumerable<Finding> items)
    {
        var seen = new HashSet<string>(PathComparer);
        var result = new List<Finding>();
        foreach (var item in items)
        {
            if (seen.Add($"{item.File}|{item.Line}|{item.Col}|{item.Code}"))
            {
                result.Add(item);
            }
        }

        return result;
    }

    // Culture-independent so the order is identical on every machine.
    static int CompareText(string a, string b)
    {
        var result = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        return result != 0 ? result : string.CompareOrdinal(a, b);
    }

    static int CompareFinding(Finding a, Finding b)
    {
        var result = SeverityOrder[a.Severity] - SeverityOrder[b.Severity];
        if (result != 0)
        {
            return result;
        }

        result = a.Rank - b.Rank;
        if (result != 0)
        {
            return result;
        }

        result = CompareText(a.Code, b.Code);
        if (result != 0)
        {
            return result;
        }

        result = CompareText(a.File, b.File);
        if (result != 0)
        {
            return result;
        }

        result = a.Line - b.Line;
        if (result != 0)
        {
            return result;
        }

        result = a.Col - b.Col;
        return result != 0 ? result : CompareText(a.Message, b.Message);
    }

    static List<Finding> Sorted(IEnumerable<Finding> items)
    {
        var list = items.ToList();
        list.Sort(CompareFinding);
        return list;
    }

    // Plain tab-separated text, identical on every OS.
    static void WriteTsv(IEnumerable<Finding> items, string path)
    {
        var lines = new List<string> { string.Join('\t', FindingHeader) };
        foreach (var item in items)
        {
            lines.Add(string.Join('\t', item.Severity, item.Rank.ToString(CultureInfo.InvariantCulture), item.Category, item.Code, item.File,
                item.Line.ToString(CultureInfo.InvariantCulture), item.Col.ToString(CultureInfo.InvariantCulture), item.Message, item.Project));
        }

        WriteText(path, lines);
    }

    static List<Finding> ImportFindings(string path)
    {
        var result = new List<Finding>();
        if (!File.Exists(path))
        {
            return result;
        }

        foreach (var line in ReadLines(path).Skip(1))
        {
            if (line.Length == 0)
            {
                continue;
            }

            var fields = line.Split('\t');
            string Field(int index) => index < fields.Length ? fields[index] : "";
            result.Add(new Finding(Field(0), int.Parse(Field(1), CultureInfo.InvariantCulture), Field(2), Field(3), Field(4),
                int.Parse(Field(5), CultureInfo.InvariantCulture), int.Parse(Field(6), CultureInfo.InvariantCulture), Field(7), Field(8)));
        }

        return result;
    }

    static string FindingKey(Finding finding) => $"{finding.File}|{finding.Code}|{finding.Message}";

    // ---------------------------------------------------------------- scope

    static (string Mode, string Text) ParseScope(string scope)
    {
        var text = scope.Trim();
        string mode;
        if (text.Equals("changed", StringComparison.OrdinalIgnoreCase))
        {
            mode = "changed";
        }
        else if (text.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            mode = "all";
        }
        else if (text.Equals("deadcode", StringComparison.OrdinalIgnoreCase))
        {
            mode = "deadcode";
        }
        else if (Regex.IsMatch(text, "^(?i:error|warning|info)$"))
        {
            mode = "severity";
        }
        else if (Regex.IsMatch(text, @"^[A-Za-z]+\d*$"))
        {
            mode = "code";
        }
        else
        {
            throw new ToolException($"invalid scope '{scope}'. Use changed, all, error, warning, info, deadcode or a diagnostic code.");
        }

        return (mode, mode == "code" ? text.ToUpperInvariant() : text.ToLowerInvariant());
    }

    // True when the scope is answered only by the dead-code build, so a failed or skipped build must not read as "no findings".
    static bool ScopeNeedsDeadCodeBuild(string mode, string scope) =>
        mode == "deadcode" || (mode == "code" && DeadCodeIdeCodes.Contains(scope));

    // True when the scope filter drops compiler/build errors (warning, info, or a non-build code): analyzers need a compiling
    // solution, so with a failed build an empty result would read as "clean". changed, all, error and CS/MSB/NU codes keep the errors.
    static bool ScopeHidesBuildErrors(string mode, string scope) => mode switch
    {
        "deadcode" => true,
        "severity" => scope != "error",
        "code" => !Regex.IsMatch(scope, @"^(CS|MSB|NU)\d+$"),
        _ => false,
    };

    static string? DetectBase()
    {
        var head = Git("symbolic-ref", "--short", "refs/remotes/origin/HEAD")?.FirstOrDefault();
        foreach (var candidate in new[] { head, "origin/main", "main", "origin/master", "master" })
        {
            if (!string.IsNullOrEmpty(candidate) && Git("rev-parse", "--verify", "--quiet", candidate) is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------- changed lines

    static Dictionary<string, List<(int Start, int End)>> GetChangedRanges(string mergeBase)
    {
        var map = new Dictionary<string, List<(int Start, int End)>>(PathComparer);
        string? file = null;
        var inHeader = false;
        var hunk = new Regex(@"^@@ -\d+(?:,\d+)? \+(?<s>\d+)(?:,(?<c>\d+))? @@");
        var target = new Regex(@"^\+\+\+ (?:b/)?(.+?)\t?$");

        foreach (var line in Git("-c", "core.quotepath=off", "diff", "--unified=0", "--no-color", "--no-ext-diff", mergeBase) ?? [])
        {
            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
            {
                inHeader = true;
                file = null;
                continue;
            }

            if (inHeader)
            {
                var targetMatch = target.Match(line);
                if (targetMatch.Success)
                {
                    file = targetMatch.Groups[1].Value == "/dev/null" ? null : targetMatch.Groups[1].Value;
                    continue;
                }
            }

            var hunkMatch = hunk.Match(line);
            if (!hunkMatch.Success)
            {
                continue;
            }

            inHeader = false;
            if (file is null)
            {
                continue;
            }

            var count = hunkMatch.Groups["c"].Success ? int.Parse(hunkMatch.Groups["c"].Value, CultureInfo.InvariantCulture) : 1;
            if (count == 0)
            {
                continue;
            }

            var start = int.Parse(hunkMatch.Groups["s"].Value, CultureInfo.InvariantCulture);
            if (!map.TryGetValue(file, out var ranges))
            {
                map[file] = ranges = [];
            }

            ranges.Add((start, start + count - 1));
        }

        foreach (var untracked in Git("-c", "core.quotepath=off", "ls-files", "--others", "--exclude-standard") ?? [])
        {
            if (untracked.Length > 0)
            {
                map[untracked] = [(1, int.MaxValue)];
            }
        }

        return map;
    }

    static bool InChangedRanges(Finding finding, Dictionary<string, List<(int Start, int End)>> ranges)
    {
        if (!ranges.TryGetValue(finding.File, out var fileRanges))
        {
            // A finding with no file on disk (e.g. a compiler-level error) can't be attributed to a diff, so keep it.
            return !File.Exists(finding.File);
        }

        return finding.Line <= 0 || fileRanges.Any(r => finding.Line >= r.Start && finding.Line <= r.End);
    }

    // ---------------------------------------------------------------- .editorconfig resolution

    // Converts an .editorconfig section glob to an anchored regex over a path relative to the config's directory.
    // Supports *, **, ?, [seq], [!seq], {a,b} (nestable), {n..m} (any integer) and \ escapes. A glob without '/' matches at any depth.
    static Regex EditorConfigRegex(string glob)
    {
        var body = EditorConfigRegexBody(glob.TrimStart('/'));
        var prefix = glob.Contains('/') ? "" : "(?:.*/)?";
        return new Regex($"^{prefix}{body}$", IsWin ? RegexOptions.IgnoreCase : RegexOptions.None);
    }

    static string EditorConfigRegexBody(string glob)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < glob.Length)
        {
            var c = glob[i];
            switch (c)
            {
                case '*':
                    if (i + 1 < glob.Length && glob[i + 1] == '*')
                    {
                        sb.Append(".*");
                        i++;
                    }
                    else
                    {
                        sb.Append("[^/]*");
                    }

                    break;
                case '?':
                    sb.Append("[^/]");
                    break;
                case '\\':
                    if (i + 1 < glob.Length)
                    {
                        i++;
                        sb.Append(Regex.Escape(glob[i].ToString()));
                    }
                    else
                    {
                        sb.Append(@"\\");
                    }

                    break;
                case '[':
                    var closeBracket = glob.IndexOf(']', i + 1);
                    if (closeBracket < 0)
                    {
                        sb.Append(@"\[");
                    }
                    else
                    {
                        var set = glob.Substring(i + 1, closeBracket - i - 1);
                        if (set.StartsWith('!'))
                        {
                            set = "^" + set[1..];
                        }

                        sb.Append('[').Append(set.Replace(@"\", @"\\")).Append(']');
                        i = closeBracket;
                    }

                    break;
                case '{':
                    // Find the matching close brace, honouring nesting.
                    var depth = 0;
                    var closeBrace = -1;
                    for (var j = i; j < glob.Length; j++)
                    {
                        if (glob[j] == '{')
                        {
                            depth++;
                        }
                        else if (glob[j] == '}')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                closeBrace = j;
                                break;
                            }
                        }
                    }

                    if (closeBrace < 0)
                    {
                        sb.Append(@"\{");
                        break;
                    }

                    var inner = glob.Substring(i + 1, closeBrace - i - 1);
                    if (Regex.IsMatch(inner, @"^-?\d+\.\.-?\d+$"))
                    {
                        sb.Append(@"-?\d+");
                    }
                    else
                    {
                        // Split on top-level commas only.
                        var parts = new List<string>();
                        depth = 0;
                        var start = 0;
                        for (var j = 0; j < inner.Length; j++)
                        {
                            if (inner[j] == '{')
                            {
                                depth++;
                            }
                            else if (inner[j] == '}')
                            {
                                depth--;
                            }
                            else if (inner[j] == ',' && depth == 0)
                            {
                                parts.Add(inner[start..j]);
                                start = j + 1;
                            }
                        }

                        parts.Add(inner[start..]);
                        if (parts.Count == 1)
                        {
                            sb.Append(@"\{").Append(EditorConfigRegexBody(inner)).Append(@"\}");
                        }
                        else
                        {
                            sb.Append("(?:").Append(string.Join('|', parts.Select(EditorConfigRegexBody))).Append(')');
                        }
                    }

                    i = closeBrace;
                    break;
                default:
                    sb.Append(Regex.Escape(c.ToString()));
                    break;
            }

            i++;
        }

        return sb.ToString();
    }

    static string ParentDir(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0 ? "" : path[..index];
    }

    // Tracked .editorconfig files parsed once: directory (repo-relative, '' = root) -> Root flag and ordered sections, each with
    // its glob regex and the dead-code rule severities it assigns (lowercase, last assignment in the section wins).
    static Dictionary<string, EditorConfigFile> GetEditorConfigs()
    {
        if (editorConfigs is not null)
        {
            return editorConfigs;
        }

        var configs = new Dictionary<string, EditorConfigFile>(PathComparer);
        var header = new Regex(@"^\s*\[(?<glob>.+)\]\s*(?:[#;].*)?$");
        var root = new Regex(@"^\s*root\s*=\s*true\b", RegexOptions.IgnoreCase);
        var rule = new Regex(@"^\s*dotnet_diagnostic\.(?<code>[A-Za-z]+\d+)\.severity\s*=\s*(?<sev>none|silent|suggestion|warning|error)\b", RegexOptions.IgnoreCase);

        foreach (var file in Git("ls-files", "--cached", "--others", "--exclude-standard", "--", ".editorconfig", "*/.editorconfig") ?? [])
        {
            if (file.Length == 0 || !File.Exists(file))
            {
                continue;
            }

            var config = new EditorConfigFile();
            EditorConfigSection? section = null;
            foreach (var line in ReadLines(file))
            {
                var headerMatch = header.Match(line);
                if (headerMatch.Success)
                {
                    section = new EditorConfigSection(EditorConfigRegex(headerMatch.Groups["glob"].Value.Trim()));
                    config.Sections.Add(section);
                    continue;
                }

                if (section is null)
                {
                    if (root.IsMatch(line))
                    {
                        config.Root = true;
                    }

                    continue;
                }

                var ruleMatch = rule.Match(line);
                if (!ruleMatch.Success)
                {
                    continue;
                }

                var code = ruleMatch.Groups["code"].Value.ToUpperInvariant();
                if (DeadCodeIdeCodes.Contains(code))
                {
                    section.Rules[code] = ruleMatch.Groups["sev"].Value.ToLowerInvariant();
                }
            }

            configs[ParentDir(file)] = config;
        }

        editorConfigs = configs;
        return configs;
    }

    // A .globalconfig has lower precedence than .editorconfig, so the severity the repo sets for a dead-code IDE rule wins over the
    // generated one. Resolves it for one file like the compiler does: the closest .editorconfig wins, up to the one with root = true,
    // and inside a file the last matching section wins. Returns none/silent/suggestion/warning/error, or null when not configured.
    static string? EffectiveDeadCodeSeverity(string relativeFile, string code)
    {
        var configs = GetEditorConfigs();
        var dir = ParentDir(relativeFile);
        while (true)
        {
            if (configs.TryGetValue(dir, out var config))
            {
                var target = dir.Length > 0 ? relativeFile[(dir.Length + 1)..] : relativeFile;
                for (var i = config.Sections.Count - 1; i >= 0; i--)
                {
                    var section = config.Sections[i];
                    if (section.Rules.TryGetValue(code, out var severity) && section.Pattern.IsMatch(target))
                    {
                        return severity;
                    }
                }

                if (config.Root)
                {
                    return null;
                }
            }

            if (dir.Length == 0)
            {
                return null;
            }

            dir = ParentDir(dir);
        }
    }

    // Dead-code IDE rules that resolve to none/silent for at least one source file: the extra build cannot turn them back on.
    static List<string> GetUnscannedDeadCodeRules()
    {
        var sources = Git("ls-files", "--cached", "--others", "--exclude-standard", "--", "*.cs", "*.vb") ?? [];
        var unscanned = new List<string>();
        foreach (var code in DeadCodeIdeCodes)
        {
            if (sources.Any(source => EffectiveDeadCodeSeverity(source, code) is "none" or "silent"))
            {
                unscanned.Add(code);
            }
        }

        return unscanned;
    }

    // ---------------------------------------------------------------- dead code

    // One extra build with the dead-code IDE rules enabled through generated files (EditorConfigFiles item), repo files untouched.
    static List<Finding> RunDeadCodeBuild(string solution, string logPath, string dir)
    {
        var configPath = Path.GetFullPath(Path.Combine(dir, "deadcode.globalconfig"));
        var targetsPath = Path.GetFullPath(Path.Combine(dir, "deadcode.targets"));
        WriteText(configPath, new[] { "is_global = true" }.Concat(DeadCodeIdeCodes.Select(code => $"dotnet_diagnostic.{code}.severity = warning")));

        // The path lands in an MSBuild item Include inside an XML attribute: escape MSBuild specials first, then XML.
        var msbuildPath = configPath.Replace("%", "%25").Replace(";", "%3B").Replace("$", "%24").Replace("@", "%40");
        var xmlConfigPath = System.Security.SecurityElement.Escape(msbuildPath);
        WriteText(targetsPath,
        [
            "<Project>",
            "  <Target Name=\"DeadCodeInjectRules\" AfterTargets=\"GenerateMSBuildEditorConfigFile\" BeforeTargets=\"CoreCompile\">",
            "    <ItemGroup>",
            $"      <EditorConfigFiles Include=\"{xmlConfigPath}\" />",
            "    </ItemGroup>",
            "  </Target>",
            "</Project>",
        ]);

        var exit = ExecLogged(logPath, "dotnet", "build", solution, "--no-incremental", "-nologo", "-v:minimal",
            "-p:EnforceCodeStyleInBuild=true", "-p:GenerateDocumentationFile=true", "-p:TreatWarningsAsErrors=false",
            $"-p:CustomAfterMicrosoftCommonTargets={targetsPath}");
        var parsed = ReadDiagnostics(logPath, null);

        // A repo can set a rule to error explicitly, which fails this build with a real finding; only an unparsable failure is a tooling error.
        if (exit != 0 && !parsed.Any(f => f.Severity == "error"))
        {
            PrintTail(logPath);
            throw new ToolException($"dead-code build failed (exit {exit}) without a parsable diagnostic; see {logPath}.");
        }

        // An error outside the dead-code rules (e.g. only hit with documentation/code-style enforcement on) means the compilation
        // did not finish cleanly, so the dead-code diagnostics may be missing. Errors from the dead-code rules themselves are findings.
        deadCodeBuildIncomplete = exit != 0 && parsed.Any(f => f.Severity == "error" && !DeadCodeIdeCodes.Contains(f.Code));

        // The generated rules run as warnings, which only exist to surface them in the log. Where the repo configures the rule as
        // warning or error for the finding's own file that severity is kept; otherwise (not configured, or suggestion) it is reported as info.
        var result = new List<Finding>();
        foreach (var finding in parsed)
        {
            if (finding.Severity == "error")
            {
                result.Add(finding);
            }
            else if (DeadCodeIdeCodes.Contains(finding.Code))
            {
                var repoSeverity = EffectiveDeadCodeSeverity(finding.File, finding.Code);
                result.Add(finding with { Severity = repoSeverity is "warning" or "error" ? "warning" : "info" });
            }
        }

        return result;
    }

    static void PrintTail(string logPath)
    {
        foreach (var line in ReadLines(logPath).TakeLast(20))
        {
            Out.WriteLine(line);
        }
    }

    // Report-only: unused internal/public symbols across the solution (Roslynator). Best effort; returns a status and the rows.
    static (string Status, List<Candidate> Rows) RunDeadCodeCandidates(string solution, string dir)
    {
        var toolDir = Path.GetFullPath(Path.Combine(dir, "tools"));
        var exe = Path.Combine(toolDir, IsWin ? "roslynator.exe" : "roslynator");
        if (!File.Exists(exe))
        {
            ExecLogged(Path.Combine(dir, "roslynator-install.log"), "dotnet", "tool", "install", "roslynator.dotnet.cli", "--tool-path", toolDir);
            if (!File.Exists(exe))
            {
                return ("unavailable", []);
            }
        }

        var log = Path.Combine(dir, "roslynator.log");
        var exit = ExecLogged(log, exe, "find-symbol", solution, "--unused", "--visibility", "public", "internal", "--verbosity", "normal");
        if (exit != 0)
        {
            return ("unavailable", []);
        }

        string[] kinds = ["class", "struct", "interface", "enum", "delegate", "method", "property", "indexer", "field", "const", "event", "enum-field"];
        var rows = new List<Candidate>();
        foreach (var line in ReadLines(log))
        {
            var match = Regex.Match(line, @"^(?<kind>[a-z-]+)\s+(?<symbol>\S.*)$");
            if (!match.Success || !kinds.Contains(match.Groups["kind"].Value))
            {
                continue;
            }

            var symbol = match.Groups["symbol"].Value.Trim();

            // Test projects and constructors are noise: test classes are discovered by the runner and never referenced.
            if (Regex.IsMatch(symbol, @"(?i)\.tests?\.") || Regex.IsMatch(symbol, @"\.(\w+)\.\1\("))
            {
                continue;
            }

            rows.Add(new Candidate(match.Groups["kind"].Value, symbol));
        }

        return ("ok", rows);
    }

    // ---------------------------------------------------------------- state files

    static RunState ReadRunState(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path, Utf8NoBom));
        var root = document.RootElement;
        var unscanned = root.TryGetProperty("unscannedRules", out var rules) && rules.ValueKind == JsonValueKind.Array
            ? rules.EnumerateArray().Select(r => r.GetString() ?? "").Where(r => r.Length > 0).ToArray()
            : [];
        var candidates = JsonString(root, "deadCodeCandidates");
        return new RunState(JsonString(root, "scope"), JsonString(root, "base"), JsonString(root, "mergeBase"), JsonString(root, "solution"),
            JsonString(root, "startedUtc"), candidates.Length > 0 ? candidates : "not-run", unscanned);
    }

    static void WriteRunState(string path, RunState state) => WriteText(path, [Json(w =>
    {
        w.WriteStartObject();
        w.WriteString("scope", state.Scope);
        w.WriteString("base", state.Base);
        w.WriteString("mergeBase", state.MergeBase);
        w.WriteString("solution", state.Solution);
        w.WriteString("startedUtc", state.StartedUtc);
        w.WriteString("deadCodeCandidates", state.DeadCodeCandidates);
        w.WriteStartArray("unscannedRules");
        foreach (var rule in state.UnscannedRules)
        {
            w.WriteStringValue(rule);
        }

        w.WriteEndArray();
        w.WriteEndObject();
    }, indented: true)]);

    static string HookStatePath => Path.Combine(StateDir, "verified.json");

    // Shared by the skill run and the Stop hook: the fingerprint of the code that was last verified clean.
    static HookState LoadHookState()
    {
        if (!File.Exists(HookStatePath))
        {
            return new HookState("", "", 0);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(HookStatePath, Utf8NoBom));
            var root = document.RootElement;
            var blocks = root.TryGetProperty("blocks", out var value) && value.TryGetInt32(out var number) ? number : 0;
            return new HookState(JsonString(root, "verifiedFingerprint"), JsonString(root, "waivedFingerprint"), blocks);
        }
        catch (JsonException)
        {
            return new HookState("", "", 0);
        }
    }

    static void SaveHookState(HookState state) => WriteText(HookStatePath, [Json(w =>
    {
        w.WriteStartObject();
        w.WriteString("verifiedFingerprint", state.Verified);
        w.WriteString("waivedFingerprint", state.Waived);
        w.WriteNumber("blocks", state.Blocks);
        w.WriteString("updatedUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        w.WriteEndObject();
    }, indented: true)]);

    // SHA-256 over the uncommitted and untracked files that can change the analysis; null when there are none.
    static string? ComputeFingerprint()
    {
        var files = new SortedSet<string>(StringComparer.Ordinal);
        var tracked = Git("-c", "core.quotepath=off", "diff", "--name-only", "HEAD")
                      ?? Git("-c", "core.quotepath=off", "ls-files", "--cached")
                      ?? [];
        var untracked = Git("-c", "core.quotepath=off", "ls-files", "--others", "--exclude-standard") ?? [];
        foreach (var file in tracked.Concat(untracked))
        {
            if (file.Length > 0
                && RelevantFile.IsMatch(file)
                && !file.StartsWith(".claude/", StringComparison.Ordinal)
                && !file.StartsWith("artifacts/", StringComparison.Ordinal))
            {
                files.Add(file);
            }
        }

        if (files.Count == 0)
        {
            return null;
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files)
        {
            hash.AppendData(Utf8NoBom.GetBytes(file));
            hash.AppendData([0]);
            hash.AppendData(File.Exists(file) ? SHA256.HashData(File.ReadAllBytes(file)) : []);
            hash.AppendData([0]);
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    // ---------------------------------------------------------------- markdown

    static string MarkdownTable(string[] columns, IEnumerable<string[]> rows)
    {
        var all = rows.ToList();
        if (all.Count == 0)
        {
            return "None.";
        }

        var output = new List<string>
        {
            "| " + string.Join(" | ", columns) + " |",
            "|" + string.Concat(Enumerable.Repeat("---|", columns.Length)),
        };
        output.AddRange(all.Select(row => "| " + string.Join(" | ", row.Select(cell => cell.Replace("|", @"\|"))) + " |"));
        return string.Join("\n", output);
    }

    static string[] FindingRow(Finding f, string[] columns) => columns
        .Select(column => column switch
        {
            "Severity" => f.Severity,
            "Code" => f.Code,
            "Category" => f.Category,
            "Message" => f.Message,
            "Location" => f.File + (f.Line > 0 ? $":{f.Line}" : ""),
            _ => "",
        })
        .ToArray();

    static string FindingsTable(IEnumerable<Finding> findings)
    {
        string[] columns = ["Severity", "Code", "Category", "Location", "Message"];
        return MarkdownTable(columns, findings.Select(f => FindingRow(f, columns)));
    }

    static string ConsoleTable(IReadOnlyList<Finding> items)
    {
        string[] headers = ["Severity", "Category", "Code", "File", "Line", "Message"];
        var rows = items
            .Select(f => new[] { f.Severity, f.Category, f.Code, f.File, f.Line.ToString(CultureInfo.InvariantCulture), f.Message })
            .ToList();
        var widths = headers.Select((header, index) => Math.Max(header.Length, rows.Count == 0 ? 0 : rows.Max(row => row[index].Length))).ToArray();
        string Format(string[] row) => string.Join("  ", row.Select((cell, index) => index == row.Length - 1 ? cell : cell.PadRight(widths[index]))).TrimEnd();

        var lines = new List<string> { Format(headers), Format(widths.Select(width => new string('-', width)).ToArray()) };
        lines.AddRange(rows.Select(Format));
        return string.Join("\n", lines);
    }

    // ---------------------------------------------------------------- report mode

    static int ReportMode(Options options)
    {
        var outDir = options.OutDir;
        var baselinePath = Path.Combine(outDir, "baseline.tsv");
        var findingsPath = Path.Combine(outDir, "findings.tsv");
        var allPath = Path.Combine(outDir, "findings-all.tsv");
        var statePath = Path.Combine(outDir, "state.json");
        var candidatesPath = Path.Combine(outDir, "deadcode-candidates.tsv");
        var reportPath = Path.Combine(outDir, "report.md");

        foreach (var required in new[] { baselinePath, findingsPath, allPath, statePath })
        {
            if (!File.Exists(required))
            {
                throw new ToolException($"missing '{required}'. Run the analysis first.");
            }
        }

        var state = ReadRunState(statePath);
        var baseline = ImportFindings(baselinePath);
        var current = ImportFindings(findingsPath);
        var outside = ImportFindings(allPath).Count - current.Count;
        var branch = Git("rev-parse", "--abbrev-ref", "HEAD")?.FirstOrDefault() ?? "";

        var fixedItems = Subtract(baseline, current);
        var introduced = Subtract(current, baseline);

        var summary = new[] { "error", "warning", "info" }.Select(severity => new[]
        {
            severity,
            baseline.Count(f => f.Severity == severity).ToString(CultureInfo.InvariantCulture),
            fixedItems.Count(f => f.Severity == severity).ToString(CultureInfo.InvariantCulture),
            current.Count(f => f.Severity == severity).ToString(CultureInfo.InvariantCulture),
            introduced.Count(f => f.Severity == severity).ToString(CultureInfo.InvariantCulture),
        });

        var changedFiles = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in (Git("-c", "core.quotepath=off", "diff", "--name-only", state.MergeBase) ?? [])
                     .Concat(Git("-c", "core.quotepath=off", "ls-files", "--others", "--exclude-standard") ?? []))
        {
            if (file.Length > 0)
            {
                changedFiles.Add(file);
            }
        }

        var testPattern = new Regex(@"(?i)(^|/)[^/]*tests?[^/]*/|tests?\.cs$");
        var testFiles = changedFiles.Where(file => testPattern.IsMatch(file)).ToList();
        var stat = string.Join("\n", Git("diff", "--stat", state.MergeBase) ?? []);
        var untrackedList = (Git("-c", "core.quotepath=off", "ls-files", "--others", "--exclude-standard") ?? []).Where(file => file.Length > 0).ToList();

        var unscannedText = state.UnscannedRules.Length > 0
            ? $"Rules disabled in .editorconfig and NOT scanned: {string.Join(", ", state.UnscannedRules)}. A clean result does not cover them."
            : "None.";

        var deadCodeSection = state.DeadCodeCandidates switch
        {
            "ok" => "Unused public/internal symbols found by Roslynator. Never deleted automatically: reflection, DI, serialization, framework entry points and public APIs look unused but are not."
                    + "\n\n"
                    + MarkdownTable(["Kind", "Symbol"], ReadLines(candidatesPath).Skip(1).Where(line => line.Length > 0).Select(line => line.Split('\t'))),
            "unavailable" => "Roslynator was unavailable (offline or install failed); candidates were not collected.",
            _ => "Not run. Use the `deadcode` or `all` scope to collect candidates.",
        };

        var markdown = string.Join("\n",
        [
            "# Lint report",
            "",
            $"- Date (UTC): {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}",
            $"- Branch: {branch}",
            $"- Scope: {state.Scope}",
            $"- Base: {state.Base} (merge-base {state.MergeBase[..Math.Min(8, state.MergeBase.Length)]})",
            $"- Solution: {state.Solution}",
            "",
            "## Summary",
            "",
            MarkdownTable(["Severity", "Initial", "Fixed", "Remaining", "New"], summary),
            "",
            $"Open findings outside the scope: {outside}",
            "",
            "## Rules not scanned",
            "",
            unscannedText,
            "",
            "## Fixed issues",
            "",
            FindingsTable(fixedItems),
            "",
            "## Remaining issues",
            "",
            FindingsTable(current),
            "",
            "## New issues",
            "",
            FindingsTable(introduced),
            "",
            "## Files changed",
            "",
            "```text",
            stat.Length > 0 ? stat : "(no tracked changes)",
            untrackedList.Count > 0 ? "Untracked:\n" + string.Join("\n", untrackedList) : "",
            "```",
            "",
            "## Test files touched",
            "",
            testFiles.Count > 0 ? string.Join("\n", testFiles.Select(file => $"- {file}")) : "None.",
            "",
            "## Dead code candidates (report only)",
            "",
            deadCodeSection,
            "",
            "## Decisions needed",
            "",
            options.Decisions.Count > 0 ? string.Join("\n", options.Decisions.Select(decision => $"- {decision}")) : "None.",
        ]);

        WriteText(reportPath, [markdown]);
        Out.WriteLine($"Report written to {reportPath}");
        Out.WriteLine();
        foreach (var line in ReadLines(reportPath))
        {
            Out.WriteLine(line);
        }

        return current.Count == 0 ? 0 : 1;
    }

    // Multiset difference: the items of `source` that have no remaining counterpart in `other`.
    static List<Finding> Subtract(List<Finding> source, List<Finding> other)
    {
        var budget = new Dictionary<string, int>();
        foreach (var item in other)
        {
            var key = FindingKey(item);
            budget[key] = budget.GetValueOrDefault(key) + 1;
        }

        var result = new List<Finding>();
        foreach (var item in source)
        {
            var key = FindingKey(item);
            if (budget.GetValueOrDefault(key) > 0)
            {
                budget[key]--;
            }
            else
            {
                result.Add(item);
            }
        }

        return result;
    }

    // ---------------------------------------------------------------- run mode

    static int RunMode(Options options)
    {
        var (mode, scopeText) = ParseScope(options.Scope);
        var outDir = options.OutDir;
        Directory.CreateDirectory(outDir);

        var buildLog = Path.Combine(outDir, "build.log");
        var formatLog = Path.Combine(outDir, "format.log");
        var deadLog = Path.Combine(outDir, "deadcode.log");
        var candidatesPath = Path.Combine(outDir, "deadcode-candidates.tsv");
        var findingsPath = Path.Combine(outDir, "findings.tsv");
        var allPath = Path.Combine(outDir, "findings-all.tsv");
        var baselinePath = Path.Combine(outDir, "baseline.tsv");
        var statePath = Path.Combine(outDir, "state.json");

        var baseRef = options.Base ?? DetectBase();
        if (baseRef is null)
        {
            throw new ToolException("could not detect the default branch. Pass --base <branch>.");
        }

        var mergeBase = Git("merge-base", "HEAD", baseRef)?.FirstOrDefault();
        if (string.IsNullOrEmpty(mergeBase))
        {
            throw new ToolException($"no merge-base between HEAD and '{baseRef}'.");
        }

        var solution = options.Solution;
        if (solution is null)
        {
            var rootFiles = Directory.GetFiles(RepoRoot).Select(Path.GetFileName).OfType<string>().ToList();
            var candidates = rootFiles.Where(f => f.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)).ToList();
            if (candidates.Count == 0)
            {
                candidates = rootFiles.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (candidates.Count != 1)
            {
                throw new ToolException($"expected exactly one solution/project at the repo root, found {candidates.Count}. Pass --solution <path>.");
            }

            solution = candidates[0];
        }

        if (!File.Exists(solution))
        {
            throw new ToolException($"solution '{solution}' not found.");
        }

        if (Exec("git", ["check-ignore", "-q", Path.Combine(outDir, ".probe").Replace('\\', '/')], mergeStdErr: false).Exit != 0)
        {
            Warn($"'{outDir}' is not git-ignored; add it to .gitignore.");
        }

        var fingerprint = ComputeFingerprint();
        Out.WriteLine($"Solution: {solution} | Scope: {scopeText} | Base: {baseRef} | Merge-base: {mergeBase[..Math.Min(8, mergeBase.Length)]}");

        var buildExit = ExecLogged(buildLog, "dotnet", "build", solution, "--no-incremental", "-nologo", "-v:minimal");
        var buildFindings = ReadDiagnostics(buildLog, null);

        var formatExit = ExecLogged(formatLog, "dotnet", "format", solution, "--verify-no-changes", "--severity", "info", "--no-restore", "--verbosity", "normal");
        var formatFindings = ReadDiagnostics(formatLog, "info");

        if (buildExit != 0 && !buildFindings.Any(f => f.Severity == "error"))
        {
            PrintTail(buildLog);
            throw new ToolException($"build failed (exit {buildExit}) without a parsable diagnostic; see {buildLog}.");
        }

        if (buildExit == 0 && formatExit is not (0 or 2))
        {
            PrintTail(formatLog);
            throw new ToolException($"dotnet format failed (exit {formatExit}); see {formatLog}.");
        }

        Out.WriteLine($"Build exit: {buildExit} | Format exit: {formatExit}");

        // The dead-code IDE rules only run in the extra build, which needs a clean main build. Those scopes filter out the build errors,
        // so continuing would report "no findings" for a scan that never ran.
        if (buildExit != 0 && ScopeHidesBuildErrors(mode, scopeText))
        {
            throw new ToolException($"analysis incomplete: the build has errors and scope '{scopeText}' does not report them, so an empty result would not mean clean. Fix them and re-run; see {buildLog}.");
        }

        // Format-only findings are the info items the build hides; anything the build already reported keeps its severity.
        var buildKeys = new HashSet<string>(PathComparer);
        foreach (var finding in buildFindings)
        {
            buildKeys.Add($"{finding.File}|{finding.Line}|{finding.Code}");
        }

        var formatOnly = formatFindings.Where(f => !buildKeys.Contains($"{f.File}|{f.Line}|{f.Code}")).ToList();

        // Dead code: the IDE rules need their own build (see RunDeadCodeBuild); skipped when the main build already failed.
        var deadFindings = new List<Finding>();
        var unscannedRules = new List<string>();
        deadCodeBuildIncomplete = false;
        if (buildExit == 0)
        {
            unscannedRules = GetUnscannedDeadCodeRules();
            if (unscannedRules.Count > 0)
            {
                Warn($"Dead-code rules disabled in .editorconfig cannot be re-enabled by the scan and were NOT checked: {string.Join(", ", unscannedRules)}.");
            }

            deadFindings = RunDeadCodeBuild(solution, deadLog, outDir).Where(f => !buildKeys.Contains($"{f.File}|{f.Line}|{f.Code}")).ToList();
            if (deadCodeBuildIncomplete)
            {
                if (ScopeNeedsDeadCodeBuild(mode, scopeText))
                {
                    throw new ToolException($"analysis incomplete: the dead-code build has errors outside the dead-code rules, so scope '{scopeText}' may be missing findings. Fix them and re-run; see {deadLog}.");
                }

                Warn($"The dead-code build has errors outside the dead-code rules; dead-code findings may be incomplete. See {deadLog}.");
            }
        }
        else
        {
            Warn("Dead-code build skipped: the build has errors. Fix them and re-run.");
        }

        var all = Sorted(Unique(buildFindings.Concat(deadFindings).Concat(formatOnly)));

        List<Finding> inScope;
        switch (mode)
        {
            case "changed":
                var ranges = GetChangedRanges(mergeBase);
                inScope = all.Where(f => InChangedRanges(f, ranges)).ToList();
                break;
            case "deadcode":
                inScope = all.Where(f => DeadCodeCodes.Contains(f.Code)).ToList();
                break;
            case "severity":
                inScope = all.Where(f => f.Severity == scopeText).ToList();
                break;
            case "code":
                inScope = all.Where(f => f.Code == scopeText).ToList();
                break;
            default:
                inScope = all;
                break;
        }

        WriteTsv(all, allPath);
        WriteTsv(inScope, findingsPath);

        var startNew = options.Reset || !File.Exists(baselinePath) || !File.Exists(statePath);
        var started = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        var candidatesStatus = "not-run";
        if (!startNew)
        {
            var previous = ReadRunState(statePath);
            if (previous.Scope != scopeText)
            {
                throw new ToolException($"scope changed from '{previous.Scope}' to '{scopeText}' since the baseline; re-run with --reset.");
            }

            started = previous.StartedUtc;

            // The candidate list is report-only and computed once per invocation; re-scans keep it.
            candidatesStatus = previous.DeadCodeCandidates;
        }
        else
        {
            WriteTsv(inScope, baselinePath);
            File.Delete(candidatesPath);
            if (mode is "deadcode" or "all")
            {
                var (status, rows) = RunDeadCodeCandidates(solution, outDir);
                candidatesStatus = status;
                if (status == "ok")
                {
                    WriteText(candidatesPath, new[] { "Kind\tSymbol" }.Concat(rows.Select(row => $"{row.Kind}\t{row.Symbol}")));
                }
                else
                {
                    Warn("Roslynator is unavailable (offline or install failed); dead-code candidates were not collected.");
                }
            }
        }

        WriteRunState(statePath, new RunState(scopeText, baseRef, mergeBase, solution, started, candidatesStatus, [.. unscannedRules]));

        Out.WriteLine();
        Out.WriteLine("Severity  InScope  Total");
        foreach (var severity in new[] { "error", "warning", "info" })
        {
            Out.WriteLine($"{severity,-9} {inScope.Count(f => f.Severity == severity),7}  {all.Count(f => f.Severity == severity),5}");
        }

        Out.WriteLine();
        if (inScope.Count > 0)
        {
            var shown = inScope.Take(options.Top).ToList();
            Out.WriteLine(ConsoleTable(shown));
            if (inScope.Count > shown.Count)
            {
                Out.WriteLine($"... {inScope.Count - shown.Count} more in {findingsPath}");
            }
        }
        else
        {
            Out.WriteLine("No findings in scope.");
        }

        Out.WriteLine();
        if (unscannedRules.Count > 0)
        {
            Out.WriteLine($"Not scanned (disabled in .editorconfig): {string.Join(", ", unscannedRules)}. A clean result does not cover these rules.");
        }

        Out.WriteLine($"Findings: {findingsPath} | All: {allPath} | Baseline: {baselinePath}");

        // A clean run over the changed code (or the whole solution) is what the Stop hook accepts as "verified".
        if (inScope.Count == 0 && mode is "changed" or "all" && fingerprint is not null)
        {
            SaveHookState(new HookState(fingerprint, "", 0));
        }

        return inScope.Count == 0 ? 0 : 1;
    }

    // ---------------------------------------------------------------- Stop hook

    // Claude Code and the Copilot harness share this contract: JSON on stdin, `{"decision":"block","reason":...}` on stdout.
    static int HookStop()
    {
        var active = false;
        string? cwd = null;
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

        if (Environment.GetEnvironmentVariable("DOTNET_LINT_HOOK")?.ToLowerInvariant() is "off" or "0" or "false")
        {
            return 0;
        }

        cwd = !string.IsNullOrEmpty(cwd) && Directory.Exists(cwd) ? cwd : Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR");
        if (!string.IsNullOrEmpty(cwd) && Directory.Exists(cwd))
        {
            Directory.SetCurrentDirectory(cwd);
        }

        // Not a git repository: nothing to verify, never block.
        if (Git("rev-parse", "--show-toplevel") is null)
        {
            return 0;
        }

        InitRepo();
        var fingerprint = ComputeFingerprint();
        if (fingerprint is null)
        {
            return 0;
        }

        var state = LoadHookState();
        var blocks = active ? state.Blocks : 0;
        if (state.Verified == fingerprint || state.Waived == fingerprint)
        {
            return 0;
        }

        // The agent was already forced to continue several times for this turn: let it stop, and stay quiet until the code changes.
        if (active && blocks >= MaxConsecutiveBlocks)
        {
            SaveHookState(state with { Waived = fingerprint, Blocks = 0 });
            Console.Out.Write(Json(w =>
            {
                w.WriteStartObject();
                w.WriteString("systemMessage", $"dotnet-lint: findings remain after {MaxConsecutiveBlocks} attempts; stopping anyway. Details: {StateDir}/hook/findings.tsv");
                w.WriteEndObject();
            }));
            return 0;
        }

        var hookDir = StateDir + "/hook";
        var capture = new StringWriter();
        Out = capture;
        var reason = "";
        try
        {
            // Fixed: the hook only judges the code differing from HEAD (uncommitted + untracked), whatever scopes the script supports.
            var exit = RunMode(new Options { Scope = "changed", Base = "HEAD", OutDir = hookDir, Reset = true, Top = 25 });
            if (exit == 0)
            {
                return 0;
            }

            reason = BuildBlockReason(ImportFindings(Path.Combine(hookDir, "findings.tsv")), hookDir);
        }
        catch (ToolException e)
        {
            reason = $"dotnet-lint could not verify the changed code: {e.Message}\nFix the environment problem (do not edit global.json or .editorconfig to get past it), then re-run:\n{RerunCommand}";
        }
        finally
        {
            WriteText(Path.Combine(hookDir, "console.log"), capture.ToString().Split('\n'));
        }

        SaveHookState(LoadHookState() with { Blocks = blocks + 1 });
        Console.Out.Write(Json(w =>
        {
            w.WriteStartObject();
            w.WriteString("decision", "block");
            w.WriteString("reason", reason);
            w.WriteEndObject();
        }));
        return 0;
    }

    const string RerunCommand = "dotnet run .claude/skills/dotnet-lint/scripts/lint.cs -- --scope changed --reset";

    static string BuildBlockReason(List<Finding> findings, string hookDir)
    {
        var counts = new[] { "error", "warning", "info" }.Select(severity => $"{findings.Count(f => f.Severity == severity)} {severity}");
        var lines = new List<string>
        {
            $"dotnet-lint: {findings.Count} finding(s) in the changed code ({string.Join(", ", counts)}). The turn cannot end until the changed code is clean.",
                        $"Follow the dotnet-lint skill (.claude/skills/dotnet-lint/SKILL.md) from step 3: the scan already ran, full list in {hookDir}/findings.tsv.",
            "Fix the cause; never silence a rule (no #pragma, NoWarn, SuppressMessage or .editorconfig edits).",
            $"Then re-scan until it exits 0: {RerunCommand}",
            "Top findings:",
        };
        lines.AddRange(findings.Take(15).Select(f => $"- {f.Severity} {f.Code} {f.File}{(f.Line > 0 ? $":{f.Line}" : "")} {f.Message}"));
        if (findings.Count > 15)
        {
            lines.Add($"... {findings.Count - 15} more.");
        }

        return string.Join("\n", lines);
    }
}
