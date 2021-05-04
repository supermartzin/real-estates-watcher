using System.Threading.Tasks;
using RealEstatesWatcher.AdsPortals.BazosCz;

namespace Real_Estates_Watcher
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var bazos = new BazosCzAdsPortal("https://reality.bazos.cz/?hledat=prodej+bytu&rubriky=reality&hlokalita=60200&humkreis=10&cenaod=&cenado=6000000&Submit=Hledat&kitx=ano");

            var ads = await bazos.GetLatestRealEstateAdsAsync();
        }
    }
}
