using RealEstatesWatcher.AdsPortals.BazosCz;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class BazosCzAdsPortalTests : PortalParserTestBase
{
    [Fact]
    public void ParsesRepresentativeListing()
    {
        const string html = """
            <div class="inzeraty inzeratyflex"><div class="nadpis"><a href="/listing/1">Pronájem bytu 2+kk</a></div><div class="popis">Plocha 55 m²</div><div class="inzeratylok">Praha<br>Vinohrady</div><div class="inzeratycena">15 000 Kč</div><span class="velikost10">[31.7.2026]</span><img class="obrazek" src="https://img.example.test/one.jpg"></div>
            """;

        var post = Parse(new BazosCzAdsPortal(WatchedUrl), html);

        AssertPost(post, "Bazoš.cz", "Pronájem bytu 2+kk", 15_000m, 55m, Layout.TwoPlusKk, "https://example.test/listing/1");
        Assert.Equal("Praha Vinohrady", post.Address);
        Assert.Equal(new DateTime(2026, 7, 31), post.PublishTime);
    }
}
