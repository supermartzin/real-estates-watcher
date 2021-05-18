using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.CeskeRealityCz
{
    public class CeskeRealityCzAdsPortal : RealEstateAdsPortalBase
    {
        public CeskeRealityCzAdsPortal(string adsUrl,
                                       ILogger<CeskeRealityCzAdsPortal>? logger = default) : base(adsUrl, logger)
        {
            PageEncoding = Encoding.GetEncoding("windows-1250");
        }

        public override string Name => "České reality.cz";

        protected override string GetPathToAdsElements() => "//div[@id=\"div_nemovitost_obal\"]/div[contains(@class,\"div_nemovitost\")]";

        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                                        ParseTitle(node),
                                                                                        ParseText(node),
                                                                                        ParsePrice(node),
                                                                                        Currency.CZK,
                                                                                        ParseLayout(node),
                                                                                        ParseAddress(node),
                                                                                        ParseWebUrl(node),
                                                                                        ParseFloorArea(node),
                                                                                        imageUrl: ParseImageUrl(node));
        
        private static string ParseTitle(HtmlNode node) => node.SelectSingleNode("./h2/a").InnerText;

        private static string ParseText(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"nemovitost-popis\"]/p").InnerText);

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//div[@class=\"cena\"]").InnerText;

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
            {
                value = ParseText(node);
                result = Regex.Match(value, RegexPatterns.Layout);
                if (!result.Success)
                    return Layout.NotSpecified;
            }

            var layoutValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;
            layoutValue = Regex.Replace(layoutValue, RegexPatterns.AllWhitespaceValues, "");

            return LayoutExtensions.ToLayout(layoutValue);
        }

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode("./h2/a").LastChild.InnerText[2..];

        private static Uri ParseWebUrl(HtmlNode node) => new(node.SelectSingleNode("./h2/a").GetAttributeValue("href", null));

        private static decimal ParseFloorArea(HtmlNode node)
        {
            var value = ParseTitle(node);

            var result = Regex.Match(value, RegexPatterns.FloorArea);
            if (!result.Success)
                return decimal.Zero;

            var floorAreaValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;

            return decimal.TryParse(floorAreaValue, out var floorArea)
                ? floorArea
                : decimal.Zero;
        }

        private static Uri? ParseImageUrl(HtmlNode node)
        {
            var path = node.SelectSingleNode(".//a/img")?.GetAttributeValue("src", null);

            return path is not null
                ? new Uri($"https://{path[2..]}")
                : default;
        }
    }
}
