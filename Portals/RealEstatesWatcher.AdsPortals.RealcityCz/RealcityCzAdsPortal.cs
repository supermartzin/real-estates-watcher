using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RealcityCz
{
    public class RealcityCzAdsPortal : RealEstateAdsPortalBase
    {
        public override string Name => "Realcity.cz";

        public RealcityCzAdsPortal(string adsUrl,
                                   ILogger<RealEstateAdsPortalBase>? logger = default) : base(adsUrl, logger)
        {
        }

        protected override string GetPathToAdsElements() => "//div[@class=\"media advertise item\"]";

        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                                        ParseTitle(node),
                                                                                        ParseText(node),
                                                                                        ParsePrice(node),
                                                                                        Currency.CZK,
                                                                                        ParseLayout(node),
                                                                                        ParseAddress(node),
                                                                                        ParseWebUrl(node, RootHost),
                                                                                        ParseFloorArea(node),
                                                                                        imageUrl: ParseImageUrl(node));

        private static string ParseTitle(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"title\"]").InnerText);

        private static string ParseText(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"description\"]").InnerText).Trim();

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//div[@class=\"price\"]/span")?.InnerText;
            if (value is null)
                return decimal.Zero;

            value = Regex.Replace(value, @"\D+", "");

            return decimal.TryParse(value, out var price)
                ? price
                : decimal.Zero;
        }

        private static Layout ParseLayout(HtmlNode node)
        {
            const string layoutRegex = @"(2\s?\+\s?kk|1\s?\+\s?kk|2\s?\+\s?1|1\s?\+\s?1|3\s?\+\s?1|3\s?\+\s?kk|4\s?\+\s?1|4\s?\+\s?kk|5\s?\+\s?1|5\s?\+\s?kk)";

            var value = node.SelectSingleNode(".//div[@class=\"title\"]").InnerText;

            var result = Regex.Match(value, layoutRegex);
            if (!result.Success)
                return Layout.NotSpecified;

            var layoutValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;
            layoutValue = Regex.Replace(layoutValue, @"\s+", "");

            return LayoutExtensions.ToLayout(layoutValue);
        }

        private static string ParseAddress(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"address\"]").InnerText).Trim();

        private static Uri ParseWebUrl(HtmlNode node, string rootHost)
        {
            var relativePath = node.SelectSingleNode(".//div[@class=\"title\"]/a").GetAttributeValue("href", string.Empty);

            return new Uri(rootHost + relativePath);
        }
        
        private static decimal ParseFloorArea(HtmlNode node)
        {
            const string floorAreaRegex = @"([0-9]+)\s?m2|([0-9]+)\s?m²";

            var value = ParseTitle(node);

            var result = Regex.Match(value, floorAreaRegex);
            if (!result.Success)
            {
                value = ParseText(node);
                result = Regex.Match(value, floorAreaRegex);
                if (!result.Success)
                    return decimal.Zero;
            }

            var floorAreaValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;

            return decimal.TryParse(floorAreaValue, out var floorArea)
                ? floorArea
                : decimal.Zero;
        }

        private static Uri? ParseImageUrl(HtmlNode node)
        {
            var path = node.SelectSingleNode(".//div[contains(@class,\"image\")]//img")?.GetAttributeValue("src", null);
            if (path is null)
                return default;

            return new Uri($"https://{path[2..]}");  // skip leading '//' characters
        }
    }
}
