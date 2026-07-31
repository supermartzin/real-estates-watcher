using RealEstatesWatcher.AdsPortals.RealityIdnesCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class RealityIdnesCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <div class="c-products__item"><h2 class="c-products__title">prodej bytu 1+kk 42 m²</h2><p class="c-products__price"><strong>5 300 000 Kč</strong></p><p class="c-products__info">Praha 8</p><a class="c-products__link" href="https://reality.idnes.test/listing/9">Detail</a><span class="c-products__img"><img data-src="https://img.example.test/nine.jpg"></span></div>
            """;

        var post = Parse(new RealityIdnesCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "Reality.idnes.cz", "Prodej bytu 1+kk 42 m²", 5_300_000m, 42m, Layout.OnePlusKk, "https://reality.idnes.test/listing/9");
    }
}
