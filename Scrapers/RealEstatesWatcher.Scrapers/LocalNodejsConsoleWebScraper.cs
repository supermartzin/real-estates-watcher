using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Scrapers;

public class LocalNodejsConsoleWebScraper : IWebScraper
{
    private const int ProcessExitDelaySeconds = 3;

    private static readonly Encoding DefaultPageEncoding = Encoding.UTF8;
    private readonly ILogger<LocalNodejsConsoleWebScraper>? _logger;
    private readonly string _nodeExecutablePath;
    private readonly LocalNodejsConsoleWebScraperSettings _settings;
    private readonly IProcessRunner _processRunner;

    public LocalNodejsConsoleWebScraper(
        LocalNodejsConsoleWebScraperSettings settings,
        ILogger<LocalNodejsConsoleWebScraper>? logger = null,
        IProcessRunner? processRunner = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _logger = logger;
        _processRunner = processRunner ?? new SystemProcessRunner();
        _nodeExecutablePath = ResolveNodeExecutablePath(settings.PathToNodeExecutable);
    }

    public async Task<string> GetFullWebPageContentAsync(string url, Encoding? pageEncoding = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        return await GetFullWebPageContentAsync(new Uri(url), pageEncoding, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetFullWebPageContentAsync(Uri uri, Encoding? pageEncoding = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (_settings.PageScrapingTimeoutSeconds < 0)
            throw new WebScraperException("Web scraping timeout has invalid value.");

        try
        {
            _logger?.LogDebug("Creating process for scraping the page '{Url}'.", uri.OriginalString);

            var startInfo = new ProcessStartInfo { FileName = _nodeExecutablePath };
            startInfo.ArgumentList.Add(_settings.PathToScript);
            startInfo.ArgumentList.Add(_settings.PageScrapingTimeoutSeconds.ToString(CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add(uri.AbsoluteUri);
            if (!string.IsNullOrEmpty(_settings.PathToCookiesFile))
                startInfo.ArgumentList.Add(_settings.PathToCookiesFile);

            var startTime = Stopwatch.GetTimestamp();
            _logger?.LogDebug("Scraping started...");

            var result = await _processRunner.RunAsync(
                startInfo,
                pageEncoding ?? DefaultPageEncoding,
                TimeSpan.FromSeconds(_settings.PageScrapingTimeoutSeconds + ProcessExitDelaySeconds),
                cancellationToken).ConfigureAwait(false);

            _logger?.LogDebug("Scraping finished in {Seconds} s.", Stopwatch.GetElapsedTime(startTime).TotalSeconds);

            if (result.ExitCode is not 0 || !string.IsNullOrWhiteSpace(result.StandardError))
            {
                var details = string.IsNullOrWhiteSpace(result.StandardError)
                    ? $"Node.js process exited with code {result.ExitCode}."
                    : result.StandardError;
                throw new WebScraperException($"Error scraping web page: {details}");
            }

            var startIndex = result.StandardOutput.IndexOf("<html", StringComparison.OrdinalIgnoreCase);
            var closingTagIndex = result.StandardOutput.LastIndexOf("</html>", StringComparison.OrdinalIgnoreCase);

            if (startIndex < 0 || closingTagIndex < startIndex)
                throw new WebScraperException("No web page content has been scraped.");

            _logger?.LogDebug("Successfully scraped page content.");

            return result.StandardOutput[startIndex..(closingTagIndex + 7)];
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

    private static string ResolveNodeExecutablePath(string? configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? GetDefaultNodeExecutablePath()
            : configuredPath;

        if (!Path.IsPathFullyQualified(path))
            throw new ArgumentException("The Node.js executable path must be absolute.", nameof(configuredPath));

        return Path.GetFullPath(path);
    }

    private static string GetDefaultNodeExecutablePath() => OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe")
        : "/usr/local/bin/node";
}
