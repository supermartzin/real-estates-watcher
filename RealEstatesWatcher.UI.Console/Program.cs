using System.Threading.Tasks;
using RealEstatesWatcher.AdsPortals.FlatZoneCz;
using RealEstatesWatcher.Scrapers;

namespace RealEstatesWatcher.UI.Console
{
    class Program
    {
        private const string Url = "https://www.flatzone.cz/novostavby/prodej/byty/jihomoravsky-kraj-brno-mesto/2-1_2-kk_3-kk/?query=Česko~Jihomoravský%20kraj~okres%20Brno-město~Brno";

        public static async Task Main(string[] args)
        {
            var portal = new FlatZoneCzAdsPortal(Url, new LocalNodejsConsoleWebScraper("./scraper/index.js"));

            var ads = await portal.GetLatestRealEstateAdsAsync();
        }
    }
}
