using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Scrapers;

public class LocalNodejsConsoleWebScraper(string pathToScript) : IWebScraper
{
    private static readonly Encoding DefaultPageEncoding = Encoding.UTF8;

    public async Task<string> GetFullWebPageContentAsync(string url, Encoding? pageEncoding = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        return await GetFullWebPageContentAsync(new Uri(url), pageEncoding, cancellationToken);
    }

    public async Task<string> GetFullWebPageContentAsync(Uri uri, Encoding? pageEncoding = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        // decide which console to use
        string runner;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            runner = "cmd.exe";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            runner = "/bin/bash";
        else throw new WebScraperException("Unknown operating system for running the script.");

        try
        {
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

            // execute external Node.js script
            await process.StandardInput
                         .WriteLineAsync($"node {pathToScript} \"{uri.AbsoluteUri}\"")
                         .ConfigureAwait(false);

            await process.StandardInput
                         .FlushAsync(cancellationToken)
                         .ConfigureAwait(false);

            process.StandardInput.Close();
            process.WaitForExit(10000);

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