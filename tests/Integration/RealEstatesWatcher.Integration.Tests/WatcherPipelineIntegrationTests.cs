using System.Diagnostics;
using System.Globalization;
using System.Text;
using RealEstatesWatcher.AdPostsFilters.BasicFilter;
using RealEstatesWatcher.AdPostsHandlers.File;
using RealEstatesWatcher.AdsPortals.SrealityCz;
using RealEstatesWatcher.Core;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Integration.Tests;

public sealed class WatcherPipelineIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"real-estates-watcher-integration-{Guid.NewGuid():N}");

    public WatcherPipelineIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    [Trait("Category", "Integration")]
    public async Task StartupCheck_ParsesFiltersAndWritesListingsThroughRealPipeline()
    {
        const string html = """
            <html><body><ul>
              <li id="estate-list-item-affordable"><a href="/detail/affordable"><ul><li><img src="//img.example.test/affordable.jpg"></li></ul><p>Prodej bytu 2+kk 55 m²</p><p>Praha 2</p><p>5 500 000 Kč</p></a></li>
              <li id="estate-list-item-expensive"><a href="/detail/expensive"><ul><li><img src="//img.example.test/expensive.jpg"></li></ul><p>Prodej bytu 4+1 120 m²</p><p>Praha 1</p><p>12 000 000 Kč</p></a></li>
            </ul></body></html>
            """;
        var scraper = new StubWebScraper(html);
        var portal = new SrealityCzAdsPortal("https://www.sreality.cz/hledani", scraper);
        var filter = new BasicParametersAdPostsFilter(new BasicParametersAdPostsFilterSettings
        {
            MaxPrice = 6_000_000m,
            Layouts = new HashSet<Layout> { Layout.TwoPlusKk }
        });
        var outputPath = Path.Combine(_directory, "watched-listings.html");
        var handler = new LocalFileAdPostsHandler(
            new LocalFileAdPostsHandlerSettings
            {
                Enabled = true,
                MainFilePath = outputPath,
                PrintFormat = PrintFormat.Html
            },
            new NumberFormatInfo { NumberDecimalDigits = 0, NumberGroupSeparator = " " });
        var engine = new RealEstatesWatchEngine(new WatchEngineSettings
        {
            PerformCheckOnStartup = true,
            CheckIntervalMinutes = 1
        });
        engine.RegisterAdsPortal(portal);
        engine.RegisterAdPostsFilter(filter);
        engine.RegisterAdPostsHandler(handler);

        await engine.StartAsync();
        await engine.StopAsync();

        var output = await File.ReadAllTextAsync(outputPath);
        Assert.Equal(1, scraper.CallCount);
        Assert.Contains("Prodej bytu 2+kk 55 m²", output);
        Assert.Contains("https://www.sreality.cz/detail/affordable", output);
        Assert.Contains("Sreality.cz", output);
        Assert.DoesNotContain("Prodej bytu 4+1 120 m²", output);
        Assert.DoesNotContain("/detail/expensive", output);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private sealed class StubWebScraper(string html) : IWebScraper
    {
        public int CallCount { get; private set; }

        public Task<string> GetFullWebPageContentAsync(
            string url,
            Encoding? pageEncoding = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(html);
        }

        public Task<string> GetFullWebPageContentAsync(
            Uri uri,
            Encoding? pageEncoding = null,
            CancellationToken cancellationToken = default) =>
            GetFullWebPageContentAsync(uri.AbsoluteUri, pageEncoding, cancellationToken);
    }
}

public class SystemProcessRunnerIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Run_CapturesOutputFromARealChildProcess()
    {
        var runner = new SystemProcessRunner();
        var startInfo = new ProcessStartInfo { FileName = GetFixtureExecutablePath() };
        startInfo.ArgumentList.Add("write");
        startInfo.ArgumentList.Add("fixture output");

        var result = await runner.RunAsync(
            startInfo,
            Encoding.UTF8,
            TimeSpan.FromSeconds(10));

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("fixture output", result.StandardOutput);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Run_KillsAChildProcessThatExceedsItsTimeout()
    {
        var runner = new SystemProcessRunner();
        var startInfo = CreateDelayProcess(TimeSpan.FromSeconds(30));

        var exception = await Assert.ThrowsAsync<TimeoutException>(() => runner.RunAsync(
            startInfo,
            Encoding.UTF8,
            TimeSpan.FromMilliseconds(100)));

        Assert.Contains("exceeded the timeout", exception.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Run_KillsAChildProcessWhenCancelled()
    {
        var runner = new SystemProcessRunner();
        var startInfo = CreateDelayProcess(TimeSpan.FromSeconds(30));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.RunAsync(
            startInfo,
            Encoding.UTF8,
            TimeSpan.FromSeconds(10),
            cancellation.Token));
    }

    private static ProcessStartInfo CreateDelayProcess(TimeSpan delay)
    {
        var startInfo = new ProcessStartInfo { FileName = GetFixtureExecutablePath() };
        startInfo.ArgumentList.Add("delay");
        startInfo.ArgumentList.Add(((int)delay.TotalMilliseconds).ToString(CultureInfo.InvariantCulture));
        return startInfo;
    }

    private static string GetFixtureExecutablePath() => Path.Combine(
        AppContext.BaseDirectory,
        OperatingSystem.IsWindows()
            ? "RealEstatesWatcher.Integration.ProcessFixture.exe"
            : "RealEstatesWatcher.Integration.ProcessFixture");
}
