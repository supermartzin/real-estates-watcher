using RealEstatesWatcher.AdsPortals.RealcityCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class RealcityCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <div class="media advertise item"><div class="title"><a href="/listing/8">Prodej bytu 2+1 70 m²</a></div><div class="description">Close to the park</div><div class="price"><span>6 400 000 Kč</span></div><div class="address">Ostrava</div><div class="image"><img src="//img.example.test/eight.jpg"></div></div>
            """;

        var post = Parse(new RealcityCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "Realcity.cz", "Prodej bytu 2+1 70 m²", 6_400_000m, 70m, Layout.TwoPlusOne, "https://example.test/listing/8");
    }
}
