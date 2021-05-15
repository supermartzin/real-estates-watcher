using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.BezrealitkyCz
{
    public class BezrealitkyCzAdsPortal : IRealEstateAdsPortal
    {
        private readonly ILogger<BezrealitkyCzAdsPortal>? _logger;

        private readonly IWebScraper _webScraper;
        private readonly string _adsUrl;
        private readonly string _rootHost;

        public string Name => "Bezrealitky.cz";

        public BezrealitkyCzAdsPortal(string adsUrl,
                                      IWebScraper webScraper,
                                      ILogger<BezrealitkyCzAdsPortal>? logger = default)
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
                                   .SelectNodes("//div[contains(@class,\"pb-0\")]/div")
                                   .Select(ParseRealEstateAdPost)
                                   .ToList();

                _logger?.LogDebug($"({Name}): Successfully parsed {posts.Count} ads from page.");

                return posts;
            }
            catch (RealEstateAdsPortalException)
            {
                throw;
            }
            catch (WebScraperException wsEx)
            {
                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {wsEx.Message}", wsEx);
            }
            catch (Exception ex)
            {
                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
            }
        }

        private RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                             ParseTitle(node),
                                                                             string.Empty,
                                                                             ParsePrice(node),
                                                                             Currency.CZK,
                                                                             ParseAddress(node),
                                                                             ParseWebUrl(node),
                                                                             ParseFloorArea(node),
                                                                             imageUrl: ParseImageUrl(node, _rootHost));

        private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//p[@class=\"product__note\"]").InnerText;

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//strong[@class=\"product__value\"]")?.InnerText;
            if (value == null)
                return decimal.Zero;

            value = Regex.Replace(value, @"\D+", "");

            return decimal.TryParse(value, out var price)
                ? price
                : decimal.Zero;
        }

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//a[contains(@class,\"product__link\")]/strong").InnerText;

        private static Uri ParseWebUrl(HtmlNode node) => new(node.SelectSingleNode(".//a[contains(@class,\"product__link\")]").GetAttributeValue("href", string.Empty));

        private static decimal ParseFloorArea(HtmlNode node)
        {
            const string floorAreaRegex = @"([0-9]+)\s?m2|([0-9]+)\s?m²";
            
            var value = node.SelectSingleNode(".//p[@class=\"product__note\"]").InnerText;
            if (value == null)
                return decimal.Zero;

            var result = Regex.Match(value, floorAreaRegex);
            if (!result.Success)
                return decimal.Zero;

            var floorAreaValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;

            return decimal.TryParse(floorAreaValue, out var floorArea)
                ? floorArea
                : decimal.Zero;
        }

        private static Uri? ParseImageUrl(HtmlNode node, string hostUrlPart)
        {
            var path = node.SelectSingleNode(".//div[@class=\"slick-list\"]//img")?.GetAttributeValue("src", null);
            if (path is null)
                return default;
            
            return path.Contains(hostUrlPart)
                ? new Uri(path)
                : new Uri(hostUrlPart + path);
        }

        private static string ParseRootHost(string url) => $"https://{new Uri(url).Host}";
    }
}
