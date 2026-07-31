using RealEstatesWatcher.AdsPortals.BidliCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class BidliCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <a class="item" href="/listing/3"><span class="kategorie">Prodej bytu 3+kk 75 m²</span><span class="cena">6 000 000 Kč</span><span class="adresa">Brno</span><span class="img" style="background-image:url(/images/three.jpg)"></span></a>
            """;

        var post = Parse(new BidliCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "Bidli.cz", "Prodej bytu 3+kk 75 m²", 6_000_000m, 75m, Layout.ThreePlusKk, "https://example.test/listing/3");
        Assert.Equal(new Uri("https://example.test/images/three.jpg"), post.ImageUrl);
    }
}
