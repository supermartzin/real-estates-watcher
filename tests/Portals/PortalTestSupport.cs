using System.Reflection;
using System.Text;
using HtmlAgilityPack;
using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.Tests;

public abstract class PortalParserTestBase
{
    protected const string WatchedUrl = "https://example.test/listings";

    protected static RealEstateAdPost Parse(RealEstateAdsPortalBase portal, string html, string xpath = "/*")
    {
        Assert.Equal(WatchedUrl, portal.WatchedUrl);

        var document = new HtmlDocument();
        document.LoadHtml(html);
        var node = document.DocumentNode.SelectSingleNode(xpath) ?? throw new InvalidOperationException($"Fixture node '{xpath}' was not found.");
        var parseMethod = portal.GetType().GetMethod("ParseRealEstateAdPost", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Parser method was not found on {portal.GetType().Name}.");

        return (RealEstateAdPost)parseMethod.Invoke(portal, new object[] { node })!;
    }

    protected static void AssertPost(
        RealEstateAdPost post,
        string portalName,
        string title,
        decimal price,
        decimal floorArea,
        Layout layout,
        string webUrl)
    {
        Assert.Equal(portalName, post.AdsPortalName);
        Assert.Equal(title, post.Title);
        Assert.Equal(price, post.Price);
        Assert.Equal(floorArea, post.FloorArea);
        Assert.Equal(layout, post.Layout);
        Assert.Equal(new Uri(webUrl), post.WebUrl);
    }
}

internal sealed class StubWebScraper : IWebScraper
{
    private readonly string? _content;
    private readonly Exception? _exception;

    public StubWebScraper(string content) => _content = content;
    public StubWebScraper(Exception exception) => _exception = exception;

    public Encoding? RequestedEncoding { get; private set; }

    public Task<string> GetFullWebPageContentAsync(string url, Encoding? pageEncoding = null, CancellationToken cancellationToken = default)
    {
        RequestedEncoding = pageEncoding;
        return _exception is not null ? Task.FromException<string>(_exception) : Task.FromResult(_content!);
    }

    public Task<string> GetFullWebPageContentAsync(Uri uri, Encoding? pageEncoding = null, CancellationToken cancellationToken = default) =>
        GetFullWebPageContentAsync(uri.AbsoluteUri, pageEncoding, cancellationToken);
}
