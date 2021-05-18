﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RemaxCz
{
    public class RemaxCzAdsProtal : RealEstateAdsPortalBase
    {
        public override string Name => "Remax.cz";

        public RemaxCzAdsProtal(string adsUrl,
                                ILogger<RemaxCzAdsProtal>? logger = default) : base(adsUrl, logger)
        {
        }
        
        protected override string GetPathToAdsElements() => "//a[@class=\"pl-items__link\"]";

        protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node)
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
                                        ParseLayout(node),
                                        ParseAddress(node),
                                        ParseWebUrl(node, RootHost),
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
            address = Regex.Replace(address, RegexPatterns.AllWhitespaceValues, " ");
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
            var value = ParseTitle(node);

            var result = Regex.Match(value, RegexPatterns.FloorArea);
            if (!result.Success)
                return decimal.Zero;

            var floorAreaValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;

            return decimal.TryParse(floorAreaValue, out var floorArea)
                ? floorArea
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
    }
}
