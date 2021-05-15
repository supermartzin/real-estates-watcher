using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RemaxCz
{
    public class RemaxCzAdsProtal : IRealEstateAdsPortal
    {
        private readonly ILogger<RemaxCzAdsProtal>? _logger;

        private readonly string _adsUrl;
        private readonly string _rootHost;

        public string Name => "Remax.cz";

        public RemaxCzAdsProtal(string adsUrl,
                                ILogger<RemaxCzAdsProtal>? logger = default)
        {
            _adsUrl = adsUrl ?? throw new ArgumentNullException(nameof(adsUrl));
            _rootHost = ParseRootHost(adsUrl);
            _logger = logger;
        }
        
        public async Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync()
        {
            try
            {
                var webHtml = new HtmlWeb();

                // get page content
                var pageContent = await webHtml.LoadFromWebAsync(_adsUrl)
                                               .ConfigureAwait(false);

                _logger?.LogDebug($"({Name}): Downloaded page with ads.");

                var posts = pageContent.DocumentNode
                                       .SelectNodes("//a[@class=\"pl-items__link\"]")
                                       .Select(ParseRealEstateAdPost)
                                       .ToList();

                _logger?.LogDebug($"({Name}): Successfully parsed {posts.Count} ads from page.");

                return posts;
            }
            catch (Exception ex)
            {
                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
            }
        }

        private RealEstateAdPost ParseRealEstateAdPost(HtmlNode node)
        {
            var price = ParsePrice(node);
            var priceCurrency = Currency.CZK;
            var priceComment = string.Empty;
            if (price == decimal.Zero)
            {
                priceCurrency = Currency.Other;
                priceComment = ParsePriceComment(node);
            }

            return new RealEstateAdPost(Name,
                                        ParseTitle(node),
                                        string.Empty,
                                        price,
                                        priceCurrency,
                                        ParseAddress(node),
                                        ParseWebUrl(node, _rootHost),
                                        ParseFloorArea(node),
                                        priceComment,
                                        ParseImageUrl(node));
        }
        
        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//div[contains(@class,\"item-price\")]//span[@data-advert-price]")?
                            .GetAttributeValue("data-advert-price", null);
            if (value == null)
                return decimal.Zero;

            return decimal.TryParse(value, out var price)
                ? price
                : decimal.Zero;
        }

        private static string? ParsePriceComment(HtmlNode node) => node.SelectSingleNode(".//div[contains(@class,\"item-price\")]/strong")?.InnerText;

        private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//h2/strong").InnerText;

        private static string ParseAddress(HtmlNode node)
        {
            var address = node.SelectSingleNode("./div[contains(@class,\"item-info\")]//p").InnerText?.Trim();

            if (address is null)
                return string.Empty;

            address = HttpUtility.HtmlDecode(address);
            address = Regex.Replace(address, @"\s+", " ");
            address = address.TrimEnd(',', '.');

            return address;
        }

        private static Uri ParseWebUrl(HtmlNode node, string rootHost)
        {
            var relativePath = node.GetAttributeValue("href", string.Empty);

            return new Uri(rootHost + relativePath);
        }

        private static Uri? ParseImageUrl(HtmlNode node)
        {
            var path = node.SelectSingleNode("./div[@class=\"pl-items__images\"]//img")?.GetAttributeValue("data-src", null);

            return path is not null
                ? new Uri(path)
                : default;
        }

        private static decimal ParseFloorArea(HtmlNode node)
        {
            const string floorAreaRegex = "([0-9]+)\\s?m2|([0-9]+)\\s?m²";

            var value = node.SelectSingleNode(".//h2/strong").InnerText;
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

        private static string ParseRootHost(string url) => $"https://{new Uri(url).Host}";
    }
}
