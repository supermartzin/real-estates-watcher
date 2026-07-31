using RealEstatesWatcher.Scrapers;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Tests;

public class LocalNodejsConsoleWebScraperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task StringOverload_RejectsMissingUrl(string? url)
    {
        var scraper = CreateScraper();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => scraper.GetFullWebPageContentAsync(url!));
    }

    [Fact]
    public async Task UriOverload_RejectsNullUri()
    {
        var scraper = CreateScraper();

        await Assert.ThrowsAsync<ArgumentNullException>(() => scraper.GetFullWebPageContentAsync((Uri)null!));
    }

    [Fact]
    public async Task NegativeTimeout_IsRejectedBeforeStartingAProcess()
    {
        var scraper = CreateScraper(timeout: -1);

        var exception = await Assert.ThrowsAsync<WebScraperException>(
            () => scraper.GetFullWebPageContentAsync(new Uri("https://example.test")));

        Assert.Contains("invalid value", exception.Message);
    }

    private static LocalNodejsConsoleWebScraper CreateScraper(int timeout = 1) => new(new LocalNodejsConsoleWebScraperSettings
    {
        PathToScript = "unused.js",
        PageScrapingTimeoutSeconds = timeout
    });
}
