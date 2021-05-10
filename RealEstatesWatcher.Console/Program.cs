using System.Threading.Tasks;
using RealEstatesWatcher.AdsPortals.RemaxCz;

namespace RealEstatesWatcher.Console
{
    class Program
    {
        private const string Url = "https://www.remax-czech.cz/reality/vyhledavani/?area_from=40&floor_from=1&object_types%5B2%5D=on&object_types%5B5%5D=on&object_types%5B7%5D=on&ownerships%5B1%5D=on&price_to=6000000&regions%5B116%5D%5B3702%5D=on&sale=1";

        public static async Task Main(string[] args)
        {
            var portal = new RemaxCzAdsProtal(Url);

            var ads = await portal.GetLatestRealEstateAdsAsync();
        }
    }
}
