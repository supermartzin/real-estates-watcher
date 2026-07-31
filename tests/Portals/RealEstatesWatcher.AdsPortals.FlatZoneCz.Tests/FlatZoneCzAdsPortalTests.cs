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

    [Fact]
    public void MissingPriceAndImageUseDefaults()
    {
        const string html = """
            <div><span class="js-project">Project</span><span class="js-developer">Developer</span><span class="js-locality">Brno</span><a class="js-project-detail-btn" href="https://flatzone.test/fallback">Detail</a></div>
            """;

        var post = Parse(new FlatZoneCzAdsPortal(WatchedUrl, new StubWebScraper("<html/>")), html);

        AssertPost(post, "FlatZone.cz", "Project | Developer", decimal.Zero, decimal.Zero, Layout.NotSpecified, "https://flatzone.test/fallback");
        Assert.Null(post.ImageUrl);
    }

    [Fact]
    public async Task PublicLoad_RemovesTemplateCardsAndParsesTheListing()
    {
        const string html = """
            <html><body><div class="project-apartment-card"></div><div class="project-apartment-card"><span class="js-project">Live project</span><span class="js-developer">Builder</span><span class="js-price">4 500 000 Kč</span><span class="js-locality">Brno</span><a class="js-project-detail-btn" href="https://flatzone.test/live">Detail</a></div><div class="project-apartment-card"></div></body></html>
            """;
        var portal = new FlatZoneCzAdsPortal(WatchedUrl, new StubWebScraper(html));

        var post = Assert.Single(await portal.GetLatestRealEstateAdsAsync());

        Assert.Equal("Live project | Builder", post.Title);
        Assert.Equal(4_500_000m, post.Price);
    }
}
