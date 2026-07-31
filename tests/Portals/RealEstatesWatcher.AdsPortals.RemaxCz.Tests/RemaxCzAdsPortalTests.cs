using RealEstatesWatcher.AdsPortals.RemaxCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class RemaxCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <div class="pl-items__item"><a href="/listing/10">Detail</a><h2><strong>Prodej bytu 5+kk 130 m²</strong></h2><div class="item-price"><strong>12 000 000 Kč</strong></div><div class="item-info"><p>Praha 6,
            Dejvice.</p></div><div class="pl-items__images"><img data-src="https://img.example.test/ten.jpg"></div></div>
            """;

        var post = Parse(new RemaxCzAdsProtal(WatchedUrl), html);

        AssertPost(post, "RE/MAX CZ", "Prodej bytu 5+kk 130 m²", 12_000_000m, 130m, Layout.FivePlusKk, "https://example.test/listing/10");
        Assert.Equal("Praha 6, Dejvice", post.Address);
    }

    [Fact]
    public void TextPriceAndMissingOptionalDetailsUseFallbacks()
    {
        const string html = """
            <div><a href="/fallback">Detail</a><h2><strong>Rodinný dům</strong></h2><div class="item-price"><strong>Cena na dotaz</strong></div><div class="item-info"><p>Brno.</p></div></div>
            """;

        var post = Parse(new RemaxCzAdsProtal(WatchedUrl), html);

        AssertPost(post, "RE/MAX CZ", "Rodinný dům", decimal.Zero, decimal.Zero, Layout.NotSpecified, "https://example.test/fallback");
        Assert.Equal(Currency.Other, post.Currency);
        Assert.Equal("Cena na dotaz", post.PriceComment);
        Assert.Null(post.ImageUrl);
    }
}
