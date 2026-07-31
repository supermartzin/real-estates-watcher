using RealEstatesWatcher.AdsPortals.BezrealitkyCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class BezrealitkyCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <article><span class="propertyCardLabel">Pronájem 2+kk</span><span class="propertyCardAddress">Praha 3</span><div class="propertyCardContent"><p>Sunny flat</p></div><h2 class="propertyCardHeadline"><a href="https://www.bezrealitky.test/listing/2">Detail</a></h2><span class="propertyPriceAmount">20 000 Kč</span><strong class="product__value">20 000 + 3 000 Kč</strong><ul><li class="featuresListItem">Dispozice 2+kk</li><li class="featuresListItem">Plocha 64 m²</li></ul><span class="image"><img srcset="url=https://img.example.test/two.jpg&amp;w=640"></span></article>
            """;

        var post = Parse(new BezrealitkyCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "Bezrealitky.cz", "Pronájem 2+kk Praha 3", 20_000m, 64m, Layout.TwoPlusKk, "https://www.bezrealitky.test/listing/2");
        Assert.Equal(3_000m, post.AdditionalFees);
    }
}
