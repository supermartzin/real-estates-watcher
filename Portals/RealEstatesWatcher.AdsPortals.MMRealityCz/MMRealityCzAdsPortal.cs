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
                                    ILogger<MMRealityCzAdsPortal>? logger = default) : base(adsUrl, logger)
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
                                                                                        decimal.Zero,
                                                                                        ParseFloorArea(node),
                                                                                        imageUrl: ParseImageUrl(node));

        private static string ParseTitle(HtmlNode node) => node.SelectSingleNode("./p[1]").LastChild.InnerText.Trim();

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode("./strong[contains(@class,\"text-secondary\")]")?.InnerText;
            if (value == null)
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

            var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
            layoutValue = Regex.Replace(layoutValue, RegexPatterns.AllWhitespaceValues, "");

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
            var value = ParseTitle(node);

            var result = Regex.Match(value, RegexPatterns.FloorArea);
            if (!result.Success)
                return decimal.Zero;

            var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;

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
