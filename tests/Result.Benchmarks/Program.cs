using System.Linq;
using BenchmarkDotNet.Running;

var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args).ToArray();

// Help, version and list commands run no benchmarks, so an empty result is valid only for them.
var informational = args.Any(static arg => arg is "--help" or "-h" or "--version" or "--list" or "-l");
if(summaries.Length == 0)
{
    return informational ? 0 : 1;
}

return summaries.Any(static summary => summary.HasCriticalValidationErrors || summary.Reports.Any(static report => !report.Success))
    ? 1
    : 0;
