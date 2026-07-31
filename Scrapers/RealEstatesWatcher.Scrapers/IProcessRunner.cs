using System.Diagnostics;
using System.Text;

namespace RealEstatesWatcher.Scrapers;

public interface IProcessRunner
{
    Task<ProcessExecutionResult> RunAsync(
        ProcessStartInfo startInfo,
        Encoding outputEncoding,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed record ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError);
