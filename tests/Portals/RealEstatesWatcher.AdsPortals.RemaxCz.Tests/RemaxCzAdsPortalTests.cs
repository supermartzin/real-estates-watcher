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
}
