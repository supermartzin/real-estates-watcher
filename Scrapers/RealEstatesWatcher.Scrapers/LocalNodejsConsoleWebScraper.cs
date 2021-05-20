using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Scrapers
{
    public class LocalNodejsConsoleWebScraper : IWebScraper
    {
        private readonly string _pathToScript;

        public LocalNodejsConsoleWebScraper(string pathToScript)
        {
            _pathToScript = pathToScript ?? throw new ArgumentNullException(nameof(pathToScript));
        }

        public async Task<string> GetFullWebPageContentAsync(string url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            return await GetFullWebPageContentAsync(new Uri(url));
        }

        public async Task<string> GetFullWebPageContentAsync(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

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
                             .WriteLineAsync($"node {_pathToScript} \"{uri.AbsoluteUri}\"")
                             .ConfigureAwait(false);

                await process.StandardInput
                             .FlushAsync()
                             .ConfigureAwait(false);

                process.StandardInput.Close();
                process.WaitForExit(10000);

                var output = await process.StandardOutput
                                          .ReadToEndAsync()
                                          .ConfigureAwait(false);
                var errorOutput = await process.StandardError
                                               .ReadToEndAsync()
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
}
