﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.BezrealitkyCz
{
    public class BezrealitkyCzAdsPortal : RealEstateAdsPortalBase
    {
        public override string Name => "Bezrealitky.cz";

        public BezrealitkyCzAdsPortal(string adsUrl,
                                      IWebScraper webScraper,
                                      ILogger<BezrealitkyCzAdsPortal>? logger = default) : base(adsUrl, webScraper, logger)
        {
        }

        protected override string GetPathToAdsElements() => "//article[contains(@class,\"product\")]";

        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                                        ParseTitle(node),
                                                                                        string.Empty,
                                                                                        ParsePrice(node),
                                                                                        Currency.CZK,
                                                                                        ParseLayout(node),
                                                                                        ParseAddress(node),
                                                                                        ParseWebUrl(node),
                                                                                        ParseFloorArea(node),
                                                                                        imageUrl: ParseImageUrl(node, RootHost));

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

        private static Layout ParseLayout(HtmlNode node)
        {
            const string layoutRegex = @"(2\s?\+\s?kk|1\s?\+\s?kk|2\s?\+\s?1|1\s?\+\s?1|3\s?\+\s?1|3\s?\+\s?kk|4\s?\+\s?1|4\s?\+\s?kk|5\s?\+\s?1|5\s?\+\s?kk)";

            var value = node.SelectSingleNode(".//p[@class=\"product__note\"]").InnerText;

            var result = Regex.Match(value, layoutRegex);
            if (!result.Success)
                return Layout.NotSpecified;

            var layoutValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;
            layoutValue = Regex.Replace(layoutValue, @"\s+", "");

            return LayoutExtensions.ToLayout(layoutValue);
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
    }
}
