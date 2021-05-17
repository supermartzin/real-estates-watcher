using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Scrapers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.FlatZoneCz
{
    public class FlatZoneCzAdsPortal : IRealEstateAdsPortal
    {
        private readonly ILogger<FlatZoneCzAdsPortal>? _logger;

        private readonly IWebScraper _webScraper;
        private readonly string _adsUrl;

        public string Name => "FlatZone.cz";

        public FlatZoneCzAdsPortal(string adsUrl,
                                   IWebScraper webScraper,
                                   ILogger<FlatZoneCzAdsPortal>? logger = default)
        {
            _adsUrl = adsUrl ?? throw new ArgumentNullException(nameof(adsUrl));
            _webScraper = webScraper ?? throw new ArgumentNullException(nameof(webScraper));
            _logger = logger;
        }

        public async Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync()
        {
            try
            {
                // get page content
                var pageContent = await _webScraper.GetFullWebPageContentAsync(_adsUrl)
                                                   .ConfigureAwait(false);
                if (pageContent == null)
                    throw new RealEstateAdsPortalException("Page content has not been correctly downloaded.");

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageContent);

                _logger?.LogDebug($"({Name}): Downloaded page with ads.");
                
                // get HTML elements
                var elements = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class,\"project-apartment-card\")]");
                
                // remove first and last elements - templates
                if (elements.Count > 1)
                {
                    elements.RemoveAt(0);
                    elements.RemoveAt(elements.Count - 1);
                }

                // parse posts
                var posts = elements.Select(ParseRealEstateAdPost).ToList();

                _logger?.LogDebug($"({Name}): Successfully parsed {posts.Count} ads from page.");

                return posts;
            }
            catch (RealEstateAdsPortalException)
            {
                throw;
            }
            catch (WebScraperException wsEx)
            {
                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {wsEx.Message}", wsEx);
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
                                                                             Layout.NotSpecified,
                                                                             ParseAddress(node),
                                                                             ParseWebUrl(node),
                                                                             decimal.Zero,
                                                                             imageUrl: ParseImageUrl(node));
        
        private static string ParseTitle(HtmlNode node)
        {
            var name = node.SelectSingleNode(".//span[contains(@class,\"js-project\")]").InnerText;
            var developer = node.SelectSingleNode(".//span[contains(@class,\"js-developer\")]").InnerText;

            return $"{HttpUtility.HtmlDecode(name)} | {HttpUtility.HtmlDecode(developer)}";
        }

        private static decimal ParsePrice(HtmlNode node)
        {
            var value = node.SelectSingleNode(".//span[contains(@class,\"js-price\")]")?.InnerText;
            if (value == null)
                return decimal.Zero;

            var numberValue = Regex.Replace(value, @"\D+", "");

            return decimal.TryParse(numberValue, out var price)
                ? price
                : decimal.Zero;
        }

        private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//span[contains(@class,\"js-locality\")]").InnerText;

        private static Uri ParseWebUrl(HtmlNode node) => new(node.SelectSingleNode(".//a[contains(@class,\"js-project-detail-btn\")]")
                                                                 .GetAttributeValue("href", null));

        private static Uri? ParseImageUrl(HtmlNode node)
        {
            var path = node.SelectSingleNode(".//amp-img")?.GetAttributeValue("src", null);

            return path is not null
                ? new Uri(path)
                : default;
        }
    }
}
