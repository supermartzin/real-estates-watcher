using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.SrealityCz
{
    public class SrealityCzAdsPortal : IRealEstateAdsPortal
    {
        private readonly ILogger<SrealityCzAdsPortal>? _logger;

        private readonly IWebScraper _webScraper;
        private readonly string _adsUrl;
        private readonly string _rootHost;

        public string Name => "Sreality.cz";

        public SrealityCzAdsPortal(string adsUrl,
                                   IWebScraper webScraper,
                                   ILogger<SrealityCzAdsPortal>? logger = default)
        {
            _adsUrl = adsUrl ?? throw new ArgumentNullException(nameof(adsUrl));
            _webScraper = webScraper ?? throw new ArgumentNullException(nameof(webScraper));
            _rootHost = ParseRootHost(adsUrl);
            _logger = logger;
        }
        
        public async Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync()
        {
            var htmlDoc = new HtmlDocument();

            try
            {
                // get page content
                var pageContent = await _webScraper.GetFullWebPageContentAsync(_adsUrl)
                                                          .ConfigureAwait(false);
                if (pageContent == null)
                    throw new Exception("Page content has not been correctly downloaded.");

                htmlDoc.LoadHtml(pageContent);

                _logger?.LogDebug($"({Name}): Downloaded page with ads.");

                foreach (var adNode in htmlDoc.DocumentNode.SelectNodes("//div[@class=\"dir-property-list\"]/div"))
                {
                    var firstNode = adNode.FirstChild;
                    var link = firstNode.GetAttributeValue("href", null);
                    var imageUrl = firstNode.SelectSingleNode(".//img[@class=\"img\"]").GetAttributeValue("src", null);

                    var lastNode = adNode.LastChild;
                    var title = lastNode.SelectSingleNode(".//a[@class=\"title\"]").InnerText;
                    var address = lastNode.SelectSingleNode(".//span[contains(@class, \"locality\")]").InnerText;
                    var price = lastNode.SelectSingleNode(".//span[contains(@class, \"norm-price\")]").InnerText;

                }

                return new List<RealEstateAdPost>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"({Name}): Error getting latest ads: {ex.Message}");

                return new List<RealEstateAdPost>();
            }
        }
        

        private static string ParseRootHost(string url) => $"https://{new Uri(url).Host}";
    }
}
