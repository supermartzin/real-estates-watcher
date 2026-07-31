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

    [Fact]
    public void MissingOptionalValuesUseDefaults()
    {
        const string html = """
            <article><span class="propertyCardLabel">Studio</span><span class="propertyCardAddress">Brno</span><div class="propertyCardContent"><p>Description</p></div><h2 class="propertyCardHeadline"><a href="https://bezrealitky.test/fallback">Detail</a></h2><strong class="product__value">Neuvedeno</strong><ul><li class="featuresListItem">Ateliér</li></ul><span class="image"><img srcset="not-an-image-url"></span></article>
            """;

        var post = Parse(new BezrealitkyCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "Bezrealitky.cz", "Studio Brno", decimal.Zero, decimal.Zero, Layout.NotSpecified, "https://bezrealitky.test/fallback");
        Assert.Equal(decimal.Zero, post.AdditionalFees);
        Assert.Null(post.ImageUrl);
    }
}
