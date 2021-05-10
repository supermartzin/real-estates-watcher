using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Scrapers.Contracts;
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
            try
            {
                // get page content
                var pageContent = await _webScraper.GetFullWebPageContentAsync(_adsUrl)
                                                          .ConfigureAwait(false);
                if (pageContent == null)
                    throw new RealEstateAdsPortalException("Page content has not been correctly downloaded.");

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageContent);

                _logger?.LogDebug($"({Name}): Downloaded page with ads.");

                // parse posts
                var posts = htmlDoc.DocumentNode
                                                       .SelectNodes("//div[@class=\"dir-property-list\"]/div[contains(@class,\"property\")]")
                                                       .Select(ParseRealEstateAdPost)
                                                       .ToList();

                _logger?.LogDebug($"({Name}): Successfully parsed {posts.Count} ads from page.");

                return posts;
            }
            catch (RealEstateAdsPortalException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"({Name}): Error getting latest ads: {ex.Message}");

                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
            }
        }

        private RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                             ParseTitle(node),
                                                                             string.Empty,
                                                                             ParsePrice(node),
                                                                             Currency.CZK,
                                                                             ParseAddress(node),
                                                                             ParseWebUrl(node, _rootHost),
                                                                             ParseFloorArea(node),
                                                                             imageUrl: ParseImageUrl(node));

        private static string ParseTitle(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode("./div//a[@class=\"title\"]").InnerText.Trim());

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode("./div//span[contains(@class,\"locality\")]").InnerText;

        private static Uri ParseWebUrl(HtmlNode node, string rootHost)
        {
            var relativePath = node.FirstChild.GetAttributeValue("href", string.Empty);

            return new Uri(rootHost + relativePath);
        }

        private static Uri? ParseImageUrl(HtmlNode node)
        {
            var path = node.FirstChild?.SelectSingleNode(".//img[@class=\"img\"]")?.GetAttributeValue("src", null);

            return path is not null
                ? new Uri(path)
                : default;
        }

        private static decimal ParsePrice(HtmlNode node)
        {
            const string priceRegex = @"([0-9\s]+)";

            var value = node.SelectSingleNode("./div//span[contains(@class,\"norm-price\")]")?.InnerText;
            if (value == null)
                return decimal.Zero;

            value = HttpUtility.HtmlDecode(value).Replace(" ", "");
            var result = Regex.Match(value, priceRegex);
            if (!result.Success)
                return decimal.Zero;

            var priceValue = result.Groups[1].Value;

            return decimal.TryParse(priceValue, out var price)
                ? price
                : decimal.Zero;
        }

        private static decimal ParseFloorArea(HtmlNode node)
        {
            const string floorAreaRegex = @"([0-9]+)\s?m2|([0-9]+)\s?m²";

            var value = node.SelectSingleNode("./div//a[@class=\"title\"]")?.InnerText?.Trim();
            if (value == null)
                return decimal.Zero;

            value = HttpUtility.HtmlDecode(value);
            var result = Regex.Match(value, floorAreaRegex);
            if (!result.Success)
                return decimal.Zero;

            var floorAreaValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;

            return decimal.TryParse(floorAreaValue, out var floorArea)
                ? floorArea
                : decimal.Zero;
        }

        private static string ParseRootHost(string url) => $"https://{new Uri(url).Host}";
    }
}
