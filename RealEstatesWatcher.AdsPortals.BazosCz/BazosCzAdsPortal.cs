using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.BazosCz
{
    public class BazosCzAdsPortal : IRealEstateAdsPortal
    {
        private readonly ILogger<BazosCzAdsPortal>? _logger;

        private readonly string _adsUrl;
        private readonly string _rootHost;

        public BazosCzAdsPortal(string adsUrl,
                                ILogger<BazosCzAdsPortal>? logger = default)
        {
            _adsUrl = adsUrl ?? throw new ArgumentNullException(nameof(adsUrl));
            _rootHost = ParseRootHost();
            _logger = logger;
        }
        
        public async Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync()
        {
            var webHtml = new HtmlWeb();
            
            var pageContent = await webHtml.LoadFromWebAsync(_adsUrl).ConfigureAwait(false);
            
            foreach (var adNode in pageContent.DocumentNode.SelectNodes("//span[@class=\"vypis\"]"))
            {
                var adPost = new RealEstateAdPost();
                var innerNode = adNode.SelectSingleNode(".//tr[1]");
                var titleElement = innerNode.SelectSingleNode(".//span[@class=\"nadpis\"]").FirstChild;
            }

            return new List<RealEstateAdPost>();
        }


        private string ParseRootHost()
        {
            var uri = new Uri(_adsUrl);

            return $"https://{uri.Host}";
        }
    }
}
