using RealEstatesWatcher.AdsPortals.SrealityCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class SrealityCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public async Task ParsesRepresentativeListing()
    {
        const string html = """
            <html><body><ul>
              <li id="estate-list-item-1"><a href="/detail/123"><ul><li><img src="//img.example.test/one.jpg"></li></ul><p>Prodej bytu 2+kk 55 m²</p><p>Praha 2</p><p>5 500 000 Kč</p></a></li>
            </ul></body></html>
            """;
        var portal = new SrealityCzAdsPortal("https://www.sreality.cz/hledani", new StubWebScraper(html));

        var post = Assert.Single(await portal.GetLatestRealEstateAdsAsync());

        AssertPost(post, "Sreality.cz", "Prodej bytu 2+kk 55 m²", 5_500_000m, 55m, Layout.TwoPlusKk, "https://www.sreality.cz/detail/123");
        Assert.Equal("Praha 2", post.Address);
        Assert.Equal(new Uri("https://img.example.test/one.jpg"), post.ImageUrl);
    }

    [Fact]
    public async Task SparseListingUsesFallbackValues()
    {
        const string html = """
            <html><body><ul><li id="estate-list-item-fallback"><a href="/fallback"><p>Ateliér</p></a></li></ul></body></html>
            """;
        var portal = new SrealityCzAdsPortal("https://www.sreality.cz/hledani", new StubWebScraper(html));

        var post = Assert.Single(await portal.GetLatestRealEstateAdsAsync());

        Assert.Equal("Ateliér", post.Title);
        Assert.Equal(string.Empty, post.Address);
        Assert.Equal(decimal.Zero, post.Price);
        Assert.Equal(decimal.Zero, post.FloorArea);
        Assert.Equal(Layout.NotSpecified, post.Layout);
        Assert.Null(post.PriceComment);
        Assert.Null(post.ImageUrl);
    }
}
