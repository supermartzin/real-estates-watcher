using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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

            var posts = new List<RealEstateAdPost>();
            
            try
            {
                var pageContent = await webHtml.LoadFromWebAsync(_adsUrl).ConfigureAwait(false);

                foreach (var adNode in pageContent.DocumentNode.SelectNodes("//span[@class=\"vypis\"]"))
                {
                    var innerNode = adNode.SelectSingleNode(".//tr[1]");
                    var titleElement = innerNode.SelectSingleNode(".//span[@class=\"nadpis\"]").FirstChild;
                    var title = titleElement.InnerText;
                    var link = titleElement.GetAttributeValue("href", string.Empty);
                    var publishDateText = innerNode.SelectSingleNode(".//span[@class=\"velikost10\"]").InnerText;
                    var text = innerNode.SelectSingleNode(".//div[@class=\"popis\"]").InnerText;
                    var imageUrl = innerNode.SelectSingleNode(".//img[@class=\"obrazek\"]")
                        ?.GetAttributeValue("src", string.Empty);
                    var priceText = innerNode.SelectSingleNode(".//span[@class=\"cena\"]").InnerText;
                    var address = innerNode.SelectSingleNode("./td[3]").InnerHtml.Replace("<br>", " ");

                    var price = Regex.IsMatch(priceText, "([0-9\\s]+)")
                        ? decimal.Parse(Regex.Match(priceText, "([0-9\\s]+)").Groups[1].Value.Replace(" ", ""))
                        : decimal.Zero;
                    var publishDate = Regex.IsMatch(publishDateText, "\\[([0-9.\\s]+)\\]")
                        ? DateTime.ParseExact(
                            Regex.Match(publishDateText, "\\[([0-9.\\s]+)\\]").Groups[1].Value.Replace(" ", ""),
                            "d.M.yyyy", CultureInfo.InvariantCulture)
                        : default;
                    var floorArea = Regex.IsMatch(title, "([0-9]+)\\s?m2|([0-9]+)\\s?m²")
                        ? decimal.Parse(Regex.Match(title, "([0-9]+)\\s?m2|([0-9]+)\\s?m²").Groups.Where(group => group.Success).ToArray()[1].Value)
                        : decimal.Zero;

                    posts.Add(new RealEstateAdPost(title, text, price, Currency.CZK, address, _rootHost + link,
                        floorArea, imageUrl, publishDate));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error parsing latest Ad posts from Bazos.cz: {ex.Message}");
            }

            return posts;
        }


        private string ParseRootHost()
        {
            var uri = new Uri(_adsUrl);

            return $"https://{uri.Host}";
        }
    }
}
