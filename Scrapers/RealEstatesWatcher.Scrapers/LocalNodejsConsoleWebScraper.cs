using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Scrapers;

public class LocalNodejsConsoleWebScraper(LocalNodejsConsoleWebScraperSettings settings,
                                          ILogger<LocalNodejsConsoleWebScraper>? logger = null) : IWebScraper
{
    private const int ProcessExitDelaySeconds = 3;

    private static readonly Encoding DefaultPageEncoding = Encoding.UTF8;

    public async Task<string> GetFullWebPageContentAsync(string url, Encoding? pageEncoding = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        return await GetFullWebPageContentAsync(new Uri(url), pageEncoding, cancellationToken);
    }

    public async Task<string> GetFullWebPageContentAsync(Uri uri, Encoding? pageEncoding = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (settings.PageScrapingTimeoutSeconds < 0)
            throw new WebScraperException("Web scraping timeout has invalid value.");

        // decide which console to use
        string runner;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            runner = "cmd.exe";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            runner = "/bin/bash";
        else throw new WebScraperException("Unknown operating system for running the script.");

        try
        {
            logger?.LogDebug("Creating process for scraping the page '{Url}'.", uri.OriginalString);

            // create process
            var process = new Process
            {
                StartInfo =
                {
                    FileName = runner,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                }
            };
            process.Start();

            // build command
            var command = $"node {settings.PathToScript} \"{settings.PageScrapingTimeoutSeconds}\" \"{uri.AbsoluteUri}\"";
            if (!string.IsNullOrEmpty(settings.PathToCookiesFile))
                command += $" \"{settings.PathToCookiesFile}\"";

            var startTime = Stopwatch.GetTimestamp();

            logger?.LogDebug("Scraping started...");

            // execute external Node.js script
            await process.StandardInput
                         .WriteLineAsync(command)
                         .ConfigureAwait(false);

            await process.StandardInput
                         .FlushAsync(cancellationToken)
                         .ConfigureAwait(false);

            process.StandardInput.Close();
            process.WaitForExit(TimeSpan.FromSeconds(settings.PageScrapingTimeoutSeconds + ProcessExitDelaySeconds));

            logger?.LogDebug("Scraping finished in {Seconds} s.", Stopwatch.GetElapsedTime(startTime).TotalSeconds);

            using var outputReader = new StreamReader(process.StandardOutput.BaseStream, pageEncoding ?? DefaultPageEncoding);
            using var errorReader = new StreamReader(process.StandardError.BaseStream, pageEncoding ?? DefaultPageEncoding);

            var output = await outputReader.ReadToEndAsync(cancellationToken)
                                           .ConfigureAwait(false);
            var errorOutput = await errorReader.ReadToEndAsync(cancellationToken)
                                               .ConfigureAwait(false);

            process.StandardOutput.Close();
            process.StandardError.Close();

            if (!string.IsNullOrEmpty(errorOutput))
                throw new WebScraperException($"Error scraping web page: {errorOutput}");

            // extract HTML content from whole output
            var startIndex = output.IndexOf("<html", StringComparison.Ordinal);
            var endIndex = output.LastIndexOf("</html>", StringComparison.Ordinal) + 7;

            if (startIndex < 0 || endIndex < 0)
                throw new WebScraperException("No web page content has been scraped.");

            logger?.LogDebug("Successfully scraped page content.");

            return output[startIndex..endIndex];
        }
        catch (WebScraperException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WebScraperException($"Error scraping web page: {ex.Message}", ex);
        }
    }
}