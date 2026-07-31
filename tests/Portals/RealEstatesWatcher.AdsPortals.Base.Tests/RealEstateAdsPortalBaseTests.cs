using System.Text;
using HtmlAgilityPack;
using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Tests;

public class RealEstateAdsPortalBaseTests
{
    [Fact]
    public async Task UsesScraperAndParsesSelectedNodes()
    {
        var scraper = new StubWebScraper("<html><body><article data-id='one'></article><article data-id='two'></article></body></html>");
        var portal = new TestPortal("https://example.test/listings", scraper);

        var posts = await portal.GetLatestRealEstateAdsAsync();

        Assert.Equal("https://example.test/listings", portal.WatchedUrl);
        Assert.Equal(Encoding.UTF8, scraper.RequestedEncoding);
        Assert.Equal(new[] { "one", "two" }, posts.Select(post => post.Title));
    }

    [Fact]
    public async Task WrapsScraperExceptions()
    {
        var expected = new WebScraperException("scraping failed");
        var portal = new TestPortal("https://example.test/listings", new StubWebScraper(expected));

        var exception = await Assert.ThrowsAsync<RealEstateAdsPortalException>(() => portal.GetLatestRealEstateAdsAsync());

        Assert.Same(expected, exception.InnerException);
        Assert.Contains("scraping failed", exception.Message);
    }

    [Fact]
    public void RejectsInvalidUrls() =>
        Assert.Throws<UriFormatException>(() => new TestPortal("not a URL", new StubWebScraper("<html/>")));

    [Fact]
    public void RejectsNullConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new TestPortal(null!, new StubWebScraper("<html/>")));
        Assert.Throws<ArgumentNullException>(() => new TestPortal("https://example.test", null!));
    }

    [Fact]
    public async Task ReturnsEmptyCollectionWhenPageHasNoMatchingNodes()
    {
        var portal = new TestPortal("https://example.test/listings", new StubWebScraper("<html><body><p>Empty</p></body></html>"));

        Assert.Empty(await portal.GetLatestRealEstateAdsAsync());
    }

    [Fact]
    public async Task WrapsUnexpectedParserExceptions()
    {
        var portal = new ThrowingPortal("https://example.test/listings", new StubWebScraper("<html><body><article></article></body></html>"));

        var exception = await Assert.ThrowsAsync<RealEstateAdsPortalException>(() => portal.GetLatestRealEstateAdsAsync());

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains("parse failed", exception.Message);
    }

    private sealed class TestPortal(string watchedUrl, IWebScraper scraper) : RealEstateAdsPortalBase(watchedUrl, scraper)
    {
        public override string Name => "Test";
        protected override string GetPathToAdsElements() => "//article";

        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
        {
            AdsPortalName = Name,
            Title = node.GetAttributeValue("data-id", string.Empty),
            Text = string.Empty,
            Price = decimal.Zero,
            Address = string.Empty,
            WebUrl = new Uri($"https://example.test/{node.GetAttributeValue("data-id", string.Empty)}"),
            Currency = Currency.Other,
            Layout = Layout.NotSpecified
        };
    }

    private sealed class ThrowingPortal(string watchedUrl, IWebScraper scraper) : RealEstateAdsPortalBase(watchedUrl, scraper)
    {
        public override string Name => "Throwing";
        protected override string GetPathToAdsElements() => "//article";
        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => throw new InvalidOperationException("parse failed");
    }
}

public class RealEstateAdsPortalExceptionTests
{
    [Fact]
    public void Constructors_PreserveMessagesAndInnerExceptions()
    {
        var inner = new InvalidOperationException("inner");

        Assert.Null(new RealEstateAdsPortalException().InnerException);
        Assert.Equal("message", new RealEstateAdsPortalException("message").Message);
        Assert.Same(inner, new RealEstateAdsPortalException("message", inner).InnerException);
    }
}
