using RealEstatesWatcher.AdsPortals.CeskeRealityCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class CeskeRealityCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <article><h2 class="i-estate__header-title"><a href="/listing/5">Prodej bytu 3+1 80 m² Praha 4</a></h2><p class="i-estate__description-text">Spacious apartment</p><h3 class="i-estate__footer-price-value">7 500 000 Kč</h3><div class="photo img"><picture><source type="image/jpeg" srcset="https://img.example.test/five.jpg"></picture></div></article>
            """;

        var post = Parse(new CeskeRealityCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "České reality.cz", "Prodej bytu 3+1 80 m² Praha 4", 7_500_000m, 80m, Layout.ThreePlusOne, "https://example.test/listing/5");
        Assert.Equal("Praha 4", post.Address);
    }

    [Fact]
    public void DescriptionAndMissingFieldsDriveFallbackValues()
    {
        const string html = """
            <article><h2 class="i-estate__header-title">Pronájem bytu</h2><p class="i-estate__description-text">Dispozice 1+1</p><h3 class="i-estate__footer-price-value">Cena na dotaz</h3></article>
            """;

        var post = Parse(new CeskeRealityCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "České reality.cz", "Pronájem bytu", decimal.Zero, decimal.Zero, Layout.OnePlusOne, "https://example.test/");
        Assert.Equal(string.Empty, post.Address);
        Assert.Equal("Cena na dotaz", post.PriceComment);
        Assert.Null(post.ImageUrl);
    }
}
