using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.BravisCz
{
    public class BravisCzAdsPortal : RealEstateAdsPortalBase
    {
        public override string Name => "Bravis.cz";

        public BravisCzAdsPortal(string adsUrl,
                                 ILogger<BravisCzAdsPortal>? logger = default) : base(adsUrl, logger)
        {
        }

        protected override string GetPathToAdsElements() => "//ul[@class=\"itemslist\"]/li[not(@class)]";

        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                                        ParseTitle(node),
                                                                                        string.Empty,
                                                                                        ParsePrice(node),
                                                                                        Currency.CZK,
                                                                                        ParseLayout(node),
                                                                                        ParseAddress(node),
                                                                                        ParseWebUrl(node, RootHost),
                                                                                        ParseAdditionalFees(node),
                                                                                        ParseFloorArea(node),
                                                                                        ParsePriceComment(node),
                                                                                        ParseImageUrl(node, RootHost));
        
        private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//h1").InnerText.Trim();

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//strong[@class='price']")?.FirstChild?.InnerText;
            if (value == null)
                return decimal.Zero;

            value = Regex.Replace(value, RegexPatterns.AllNonNumberValues, "");

            return decimal.TryParse(value, out var price)
                ? price
                : decimal.Zero;
        }

        private static decimal ParseAdditionalFees(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//strong[@class='price']/small")?.InnerText;
            if (value == null)
                return decimal.Zero;

            var subValues = value.Split('+');
            var totalFees = decimal.Zero;
            foreach (var subValue in subValues)
            {
                var feeValue = subValue;

                var index = subValue.IndexOf(",-", StringComparison.InvariantCulture);
                if (index > -1)
                    feeValue = subValue[..index];

                feeValue = Regex.Replace(feeValue, RegexPatterns.AllNonNumberValues, "");

                if (decimal.TryParse(feeValue, out var fee))
                    totalFees += fee;
            }

            return totalFees;
        }

        private static Layout ParseLayout(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//ul[@class='params']/li[contains(text(),\"Typ\")]").InnerText;

            var result = Regex.Match(value, RegexPatterns.Layout);
            if (!result.Success)
                return Layout.NotSpecified;

            var layoutValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;
            layoutValue = Regex.Replace(layoutValue, RegexPatterns.AllWhitespaceValues, "");

            return LayoutExtensions.ToLayout(layoutValue);
        }

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//em[@class=\"location\"]").InnerText;

        private static Uri ParseWebUrl(HtmlNode node, string rootHost)
        {
            var relativePath = node.SelectSingleNode(".//a[@class=\"main\"]").GetAttributeValue("href", null);

            return new Uri(rootHost +  "/" + relativePath);
        }

        private static decimal ParseFloorArea(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//ul[@class='params']/li[contains(text(),\"Plocha\")]").InnerText;

            var result = Regex.Match(value, RegexPatterns.FloorArea);
            if (!result.Success)
                return decimal.Zero;

            var floorAreaValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;

            return decimal.TryParse(floorAreaValue, NumberStyles.Number, new NumberFormatInfo { NumberDecimalSeparator = "," }, out var floorArea)
                ? floorArea
                : decimal.Zero;
        }

        private static string? ParsePriceComment(HtmlNode node) => node.SelectSingleNode(".//string[@class='price']/small")?.InnerText?.Trim('(', ')');

        private static Uri? ParseImageUrl(HtmlNode node, string rootHost)
        {
            var relativePath = node.SelectSingleNode(".//a[@class=\"img\"]/img")?.GetAttributeValue("src", null);

            return relativePath is not null
                ? new Uri(rootHost + relativePath)
                : default;
        }
    }
}
