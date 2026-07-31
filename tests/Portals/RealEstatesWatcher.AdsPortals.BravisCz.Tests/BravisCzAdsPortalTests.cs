using RealEstatesWatcher.AdsPortals.BravisCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class BravisCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <li><h1>Pronájem bytu 2+kk</h1><strong class="price">25 000 Kč<small>+ 3 000,- + 500,-</small></strong><ul class="params"><li>Typ 2+kk</li><li>Plocha 65 m²</li></ul><em class="location">Brno-střed</em><a class="main" href="listing/4">Detail</a><a class="img"><img src="/images/four.jpg"></a></li>
            """;

        var post = Parse(new BravisCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "Bravis.cz", "Pronájem bytu 2+kk", 25_000m, 65m, Layout.TwoPlusKk, "https://example.test/listing/4");
        Assert.Equal(3_500m, post.AdditionalFees);
    }
}
