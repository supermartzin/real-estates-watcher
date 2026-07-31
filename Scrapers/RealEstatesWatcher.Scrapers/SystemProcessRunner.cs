using System.Diagnostics;
using System.Text;

namespace RealEstatesWatcher.Scrapers;

public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessExecutionResult> RunAsync(
        ProcessStartInfo startInfo,
        Encoding outputEncoding,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        ArgumentNullException.ThrowIfNull(outputEncoding);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardOutputEncoding = outputEncoding;
        startInfo.StandardErrorEncoding = outputEncoding;
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Could not start process '{startInfo.FileName}'.");

        var outputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            KillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw new TimeoutException($"Process '{startInfo.FileName}' exceeded the timeout of {timeout}.");
        }
        catch
        {
            KillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        return new ProcessExecutionResult(
            process.ExitCode,
            await outputTask.ConfigureAwait(false),
            await errorTask.ConfigureAwait(false));
    }

    private static void KillProcessTree(Process process)
    {
        if (!process.HasExited)
            process.Kill(entireProcessTree: true);
    }
}
