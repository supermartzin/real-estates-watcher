using RealEstatesWatcher.AdsPortals.FlatZoneCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class FlatZoneCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <div class="project-apartment-card"><span class="js-project">River &amp; Park</span><span class="js-developer">Builder</span><span class="js-price">8 900 000 Kč</span><span class="js-locality">Prague</span><a class="js-project-detail-btn" href="https://flatzone.test/listing/6">Detail</a><amp-img src="https://img.example.test/six.jpg"></amp-img></div>
            """;

        var post = Parse(new FlatZoneCzAdsPortal(WatchedUrl, new StubWebScraper("<html/>")), html);

        AssertPost(post, "FlatZone.cz", "River & Park | Builder", 8_900_000m, decimal.Zero, Layout.NotSpecified, "https://flatzone.test/listing/6");
    }
}
