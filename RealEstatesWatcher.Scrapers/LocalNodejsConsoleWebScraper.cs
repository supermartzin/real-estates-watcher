using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using RealEstatesWatcher.AdsPortals.Contracts;

namespace RealEstatesWatcher.Scrapers
{
    public class LocalNodejsConsoleWebScraper : IWebScraper
    {
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
                await process.StandardInput.WriteLineAsync($"node ./scraper/index.js {uri.AbsoluteUri}");

                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();
                process.WaitForExit(3000);

                // process downloaded page
                var output = await process.StandardOutput.ReadToEndAsync();
                var startIndex = output.IndexOf("<html", StringComparison.Ordinal);
                var endIndex = output.LastIndexOf("</html>", StringComparison.Ordinal) + 7;

                return output[startIndex..endIndex];
            }
            catch (Exception ex)
            {
                throw new WebScraperException($"Error scraping web page: {ex.Message}", ex);
            }
        }
    }
}
