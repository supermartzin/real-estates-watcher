using System;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.MMRealityCz
{
    public class MMRealityCzAdsPortal : RealEstateAdsPortalBase
    {
        public override string Name => "M&M Reality.cz";

        public MMRealityCzAdsPortal(string adsUrl,
                                    ILogger<RealEstateAdsPortalBase>? logger = default) : base(adsUrl, logger)
        {
        }

        protected override string GetPathToAdsElements() => "//div[contains(@class,\"grid-x\")]//div[contains(@class, \"cell\")]//a[@data-realty-name]/..";

        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                                        ParseTitle(node),
                                                                                        string.Empty,
                                                                                        ParsePrice(node),
                                                                                        Currency.CZK,
                                                                                        ParseLayout(node),
                                                                                        ParseAddress(node),
                                                                                        ParseWebUrl(node),
                                                                                        ParseFloorArea(node),
                                                                                        imageUrl: ParseImageUrl(node));

        private static string ParseTitle(HtmlNode node) => node.SelectSingleNode("./p[1]").LastChild.InnerText.Trim();

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode("./strong[contains(@class,\"text-secondary\")]")?.InnerText;
            if (value == null)
                return decimal.Zero;

            value = Regex.Replace(value, @"\D+", "");

            return decimal.TryParse(value, out var price)
                ? price
                : decimal.Zero;
        }

        private static Layout ParseLayout(HtmlNode node)
        {
            const string layoutRegex = @"(2\s?\+\s?kk|1\s?\+\s?kk|2\s?\+\s?1|1\s?\+\s?1|3\s?\+\s?1|3\s?\+\s?kk|4\s?\+\s?1|4\s?\+\s?kk|5\s?\+\s?1|5\s?\+\s?kk)";

            var value = ParseTitle(node);

            var result = Regex.Match(value, layoutRegex);
            if (!result.Success)
                return Layout.NotSpecified;

            var layoutValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;
            layoutValue = Regex.Replace(layoutValue, @"\s+", "");

            return LayoutExtensions.ToLayout(layoutValue);
        }

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode("./p[1]").FirstChild.InnerText.Trim();

        private static Uri ParseWebUrl(HtmlNode node)
        {
            var path = node.SelectSingleNode("./a[contains(@class,\"text-underline\")]").GetAttributeValue("href", null);
            
            return new Uri(path);
        }

        private static decimal ParseFloorArea(HtmlNode node)
        {
            const string floorAreaRegex = @"([0-9]+)\s?m2|([0-9]+)\s?m²";

            var value = ParseTitle(node);

            var result = Regex.Match(value, floorAreaRegex);
            if (!result.Success)
                return decimal.Zero;

            var floorAreaValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;

            return decimal.TryParse(floorAreaValue, out var floorArea)
                ? floorArea
                : decimal.Zero;
        }

        private static Uri? ParseImageUrl(HtmlNode node)
        {
            var path = node.SelectSingleNode(".//img[1]")?.GetAttributeValue("src", null);

            return path is not null
                ? new Uri(path)
                : default;
        }
    }
}
