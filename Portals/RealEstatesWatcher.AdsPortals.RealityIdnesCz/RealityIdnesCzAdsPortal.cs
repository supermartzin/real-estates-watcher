using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RealityIdnesCz
{
    public class RealityIdnesCzAdsPortal : RealEstateAdsPortalBase
    {
        public override string Name => "Reality.idnes.cz";

        public RealityIdnesCzAdsPortal(string adsUrl,
                                       ILogger<RealityIdnesCzAdsPortal>? logger = default) : base(adsUrl, logger)
        {
        }
        
        protected override string GetPathToAdsElements() => "//div[@class=\"c-products__item\"]";

        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                                        ParseTitle(node),
                                                                                        string.Empty,
                                                                                        ParsePrice(node),
                                                                                        Currency.CZK,
                                                                                        ParseLayout(node),
                                                                                        ParseAddress(node),
                                                                                        ParseWebUrl(node, RootHost),
                                                                                        ParseFloorArea(node),
                                                                                        imageUrl: ParseImageUrl(node));

        private static string ParseTitle(HtmlNode node)
        {
            var title = node.SelectSingleNode(".//h2[@class=\"c-products__title\"]").InnerText;

            title = title.Replace("\n", " ").Trim();
            title = HttpUtility.HtmlDecode(title);
            title = title[0].ToString().ToUpper() + title[1..];

            return title;
        }

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//p[@class=\"c-products__price\"]")?.InnerText;
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

            var value = node.SelectSingleNode(".//h2[@class=\"c-products__title\"]").InnerText;

            var result = Regex.Match(value, layoutRegex);
            if (!result.Success)
                return Layout.NotSpecified;

            var layoutValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;
            layoutValue = Regex.Replace(layoutValue, @"\s+", "");

            return LayoutExtensions.ToLayout(layoutValue);
        }

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//p[@class=\"c-products__info\"]").InnerText.Trim();

        private static Uri ParseWebUrl(HtmlNode node, string rootHost)
        {
            var relativePath = node.SelectSingleNode(".//a[@class=\"c-products__link\"]").GetAttributeValue("href", string.Empty);

            return new Uri(rootHost + relativePath);
        }

        private static decimal ParseFloorArea(HtmlNode node)
        {
            const string floorAreaRegex = @"([0-9]+)\s?m2|([0-9]+)\s?m²";

            var value = HttpUtility.HtmlDecode(ParseTitle(node)).Trim();

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
            var path = node.SelectSingleNode(".//span[@class=\"c-products__img\"]/img")?.GetAttributeValue("data-src", null);

            return path is not null
                ? new Uri(path)
                : default;
        }
    }
}
