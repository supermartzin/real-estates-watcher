﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.BazosCz
{
    public class BazosCzAdsPortal : IRealEstateAdsPortal
    {
        private readonly ILogger<BazosCzAdsPortal>? _logger;

        private readonly string _adsUrl;
        private readonly string _rootHost;

        public string Name => "Bazoš.cz";

        public BazosCzAdsPortal(string adsUrl,
                                ILogger<BazosCzAdsPortal>? logger = default)
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

                // parse posts
                var posts = pageContent.DocumentNode
                                       .SelectNodes("//span[@class=\"vypis\"]")
                                       .Select(adNode => adNode.SelectSingleNode(".//tr[1]"))
                                       .Select(ParseRealEstateAdPost)
                                       .ToList();

                _logger?.LogDebug($"({Name}): Successfully parsed {posts.Count} ads from page.");

                return posts;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"({Name}): Error getting latest ads: {ex.Message}");

                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
            }
        }


        private RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                             ParseTitle(node),
                                                                             ParseAdText(node),
                                                                             ParsePrice(node),
                                                                             Currency.CZK,
                                                                             ParseLayout(node),
                                                                             ParseAddress(node),
                                                                             ParseWebUrl(node, _rootHost), 
                                                                             ParseFloorArea(node),
                                                                             imageUrl: ParseImageUrl(node), 
                                                                             publishTime: ParsePublishDate(node));

        private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//span[@class=\"nadpis\"]").FirstChild.InnerText;

        private static string ParseAdText(HtmlNode node) => node.SelectSingleNode(".//div[@class=\"popis\"]").InnerText;

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode("./td[3]").InnerHtml.Replace("<br>", " ");

        private static Uri ParseWebUrl(HtmlNode node, string rootHost)
        {
            var relativePath = node.SelectSingleNode(".//span[@class=\"nadpis\"]").FirstChild.GetAttributeValue("href", string.Empty);

            return new Uri(rootHost + relativePath);
        }

        private static Uri? ParseImageUrl(HtmlNode node)
        {
            var path = node.SelectSingleNode(".//img[@class=\"obrazek\"]")?.GetAttributeValue("src", null);

            return path is not null
                ? new Uri(path)
                : default;
        }

        private static DateTime? ParsePublishDate(HtmlNode node)
        {
            const string dateTimeFormat = "d.M.yyyy";
            const string dateTimeParseRegex = "\\[([0-9.\\s]+)\\]";

            var value = node.SelectSingleNode(".//span[@class=\"velikost10\"]")?.InnerText;
            if (value == null)
                return default;

            var result = Regex.Match(value, dateTimeParseRegex);
            if (!result.Success)
                return default;

            var dateTimeValue = result.Groups[1].Value.Replace(" ", "");

            return DateTime.TryParseExact(dateTimeValue, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var publishTime)
                ? publishTime
                : default;
        }

        private static decimal ParsePrice(HtmlNode node)
        {
            const string priceRegex = "([0-9\\s]+)";

            var value = node.SelectSingleNode(".//span[@class=\"cena\"]")?.InnerText;
            if (value == null)
                return default;

            var result = Regex.Match(value, priceRegex);
            if (!result.Success)
                return decimal.Zero;

            var priceValue = result.Groups[1].Value.Replace(" ", "");

            return decimal.TryParse(priceValue, out var price)
                ? price
                : decimal.Zero;
        }

        private static decimal ParseFloorArea(HtmlNode node)
        {
            const string floorAreaRegex = "([0-9]+)\\s?m2|([0-9]+)\\s?m²";

            var value = node.SelectSingleNode(".//span[@class=\"nadpis\"]")?.FirstChild?.InnerText;
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

        private static Layout ParseLayout(HtmlNode node)
        {
            const string layoutRegex = @"(2\s?\+\s?kk|1\s?\+\s?kk|2\s?\+\s?1|1\s?\+\s?1|3\s?\+\s?1|3\s?\+\s?kk|4\s?\+\s?1|4\s?\+\s?kk|5\s?\+\s?1|5\s?\+\s?kk)";

            var value = node.SelectSingleNode(".//span[@class=\"nadpis\"]").FirstChild.InnerText;

            var result = Regex.Match(value, layoutRegex);
            if (!result.Success)
                value = node.SelectSingleNode(".//div[@class=\"popis\"]").InnerText;

            result = Regex.Match(value, layoutRegex);
            if (!result.Success)
                return Layout.NotSpecified;

            var layoutValue = result.Groups.Where(group => group.Success).ToArray()[1].Value;
            layoutValue = Regex.Replace(layoutValue, @"\s+", "");

            return LayoutExtensions.ToLayout(layoutValue);
        }

        private static string ParseRootHost(string url) => $"https://{new Uri(url).Host}";
    }
}
