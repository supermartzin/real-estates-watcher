﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RealityIdnesCz
{
    public class RealityIdnesCzAdsPortal : IRealEstateAdsPortal
    {
        private readonly ILogger<RealityIdnesCzAdsPortal>? _logger;

        private readonly string _adsUrl;
        private readonly string _rootHost;

        public string Name => "Reality.idnes.cz";

        public RealityIdnesCzAdsPortal(string adsUrl,
                                       ILogger<RealityIdnesCzAdsPortal>? logger = default)
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

                var posts = pageContent.DocumentNode
                                       .SelectNodes("//div[@class=\"c-products__item\"]")
                                       .Select(ParseRealEstateAdPost)
                                       .ToList();

                _logger?.LogDebug($"({Name}): Successfully parsed {posts.Count} ads from page.");

                return posts;
            }
            catch (Exception ex)
            {
                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
            }
        }

        private RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                             ParseTitle(node),
                                                                             string.Empty,
                                                                             ParsePrice(node),
                                                                             Currency.CZK,
                                                                             ParseAddress(node),
                                                                             ParseWebUrl(node, _rootHost),
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

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//p[@class=\"c-products__info\"]").InnerText.Trim();

        private static Uri ParseWebUrl(HtmlNode node, string rootHost)
        {
            var relativePath = node.SelectSingleNode(".//a[@class=\"c-products__link\"]").GetAttributeValue("href", string.Empty);

            return new Uri(rootHost + relativePath);
        }

        private static decimal ParseFloorArea(HtmlNode node)
        {
            const string floorAreaRegex = @"([0-9]+)\s?m2|([0-9]+)\s?m²";

            var value = node.SelectSingleNode(".//h2[@class=\"c-products__title\"]").InnerText.Trim();
            value = HttpUtility.HtmlDecode(value);

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

        private static string ParseRootHost(string url) => $"https://{new Uri(url).Host}";
    }
}
