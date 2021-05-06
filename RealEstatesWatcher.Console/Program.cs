using System.Threading.Tasks;

using RealEstatesWatcher.AdsPortals.SrealityCz;
using RealEstatesWatcher.Scrapers;

namespace RealEstatesWatcher.Console
{
    class Program
    {
        public const string Url = "https://www.sreality.cz/hledani/prodej/byty/brno?navic=balkon,lodzie,terasa&velikost=2%2Bkk,3%2Bkk,3%2B1,2%2B1&vlastnictvi=osobni&stav=po-rekonstrukci,pred-rekonstrukci,velmi-dobry-stav,dobry-stav,ve-vystavbe,developerske-projekty,novostavby&stari=tyden&patro-od=1&patro-do=100&plocha-od=40&plocha-do=10000000000&cena-od=0&cena-do=5500000";

        public static async Task Main(string[] args)
        {
            var portal = new SrealityCzAdsPortal(Url, new LocalNodejsConsoleWebScraper());

            var ads = await portal.GetLatestRealEstateAdsAsync();
        }
    }
}
