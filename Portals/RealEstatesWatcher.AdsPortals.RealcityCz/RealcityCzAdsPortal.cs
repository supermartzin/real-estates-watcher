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
                                                                                        decimal.Zero,
                                                                                        ParseFloorArea(node),
                                                                                        imageUrl: ParseImageUrl(node));

        private static string ParseTitle(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"title\"]").InnerText);

        private static string ParseText(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"description\"]").InnerText).Trim();

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//div[@class=\"price\"]/span")?.InnerText;
            if (value is null)
                return decimal.Zero;

            value = Regex.Replace(value, RegexPatterns.AllNonNumberValues, "");

            return decimal.TryParse(value, out var price)
                ? price
                : decimal.Zero;
        }

        private static Layout ParseLayout(HtmlNode node)
        {
            var value = ParseTitle(node);

            var result = Regex.Match(value, RegexPatterns.Layout);
            if (!result.Success)
                return Layout.NotSpecified;

            var layoutValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;
            layoutValue = Regex.Replace(layoutValue, RegexPatterns.AllWhitespaceValues, "");

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
            var value = ParseTitle(node);

            var result = Regex.Match(value, RegexPatterns.FloorArea);
            if (!result.Success)
            {
                value = ParseText(node);
                result = Regex.Match(value, RegexPatterns.FloorArea);
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

            return path is not null
                ? new Uri($"https://{path[2..]}")   // skip leading '//' characters
                : default;
        }
    }
}
