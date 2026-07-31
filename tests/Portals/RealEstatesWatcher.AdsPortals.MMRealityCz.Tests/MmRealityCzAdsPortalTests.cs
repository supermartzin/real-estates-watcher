using RealEstatesWatcher.AdsPortals.MMRealityCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class MmRealityCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <div><span href="/listing/7"></span><a id="target"><article><h4 class="rds-property-title">Prodej bytu 4+1 111 m²</h4><div class="rds-content"><div class="price">9 100 000 Kč</div></div><button class="rds-favorite-icon" data-realty-name="Byt, Praha 5, Smíchov"></button><div class="rds-image-carousel"><img class="rds-image" src="https://img.example.test/seven.jpg"></div></article></a></div>
            """;

        var post = Parse(new MmRealityCzAdsPortal(WatchedUrl, new StubWebScraper("<html/>")), html, "//*[@id='target']");

        AssertPost(post, "M&M Reality", "Prodej bytu 4+1 111 m²", 9_100_000m, 111m, Layout.FourPlusOne, "https://example.test/listing/7");
        Assert.Equal("Praha 5, Smíchov", post.Address);
    }

    [Fact]
    public void MissingOptionalDetailsUseFallbacks()
    {
        const string html = """
            <div><span href="/fallback"></span><a id="target"><article><h4 class="rds-property-title">Rodinný dům</h4><div class="rds-content"><div class="price">Cena na dotaz</div></div></article></a></div>
            """;

        var post = Parse(new MmRealityCzAdsPortal(WatchedUrl, new StubWebScraper("<html/>")), html, "//*[@id='target']");

        AssertPost(post, "M&M Reality", "Rodinný dům", decimal.Zero, decimal.Zero, Layout.NotSpecified, "https://example.test/fallback");
        Assert.Equal(string.Empty, post.Address);
        Assert.Equal("Cena na dotaz", post.PriceComment);
        Assert.Null(post.ImageUrl);
    }
}
