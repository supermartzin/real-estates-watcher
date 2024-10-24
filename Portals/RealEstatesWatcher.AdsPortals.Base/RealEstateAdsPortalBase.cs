﻿using System.Text;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.Base
{
    public abstract class RealEstateAdsPortalBase : IRealEstateAdsPortal
    {
        protected static class RegexPatterns
        {
            public const string Layout = @"(2\s?\+?\s?kk|1\s?\+?\s?kk|2\s?\+\s?1|1\s?\+\s?1|3\s?\+\s?1|3\s?\+?\s?kk|4\s?\+\s?1|4\s?\+?\s?kk|5\s?\+\s?1|5\s?\+?\s?kk)";
            public const string FloorArea = @"([\d,. ]+)\s?m2|([\d,. ]+)\s?m²";
            public const string AllNonNumberValues = @"\D+";
            public const string AtLeastOneDigitValue = @"\d";
            public const string AllWhitespaceValues = @"\s+";
        }

        protected const char UrlPathSeparator = '/';
        protected const string NonBreakingSpace = "\u00A0";

        protected readonly ILogger<RealEstateAdsPortalBase>? Logger;
        protected readonly IWebScraper? WebScraper;
        protected readonly HtmlWeb? HtmlWeb;

        protected readonly string AdsUrl;
        protected readonly string RootHost;

        protected Encoding PageEncoding { get; set; } = Encoding.UTF8;

        public abstract string Name { get; }

        protected RealEstateAdsPortalBase(string adsUrl,
                                          ILogger<RealEstateAdsPortalBase>? logger = default)
        {
            AdsUrl = adsUrl ?? throw new ArgumentNullException(nameof(adsUrl));
            RootHost = ParseRootHost(adsUrl);
            Logger = logger;
            HtmlWeb = new HtmlWeb
            {
                PreRequest = request =>
                {
                    request.Timeout = 30000;
                    return true;
                }
            };
        }

        protected RealEstateAdsPortalBase(string adsUrl,
                                          IWebScraper webScraper,
                                          ILogger<RealEstateAdsPortalBase>? logger = default) : this(adsUrl, logger)
        {
            WebScraper = webScraper ?? throw new ArgumentNullException(nameof(webScraper));
        }

        public virtual async Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync() => WebScraper is not null
            ? await GetAdsWithWebScraperAsync().ConfigureAwait(false)
            : await GetAdsDirectlyAsync().ConfigureAwait(false);

        protected abstract string GetPathToAdsElements();

        protected abstract RealEstateAdPost ParseRealEstateAdPost(HtmlNode node);

        private static string ParseRootHost(string url) => $"https://{new Uri(url).Host}";

        private async Task<IList<RealEstateAdPost>> GetAdsWithWebScraperAsync()
        {
            try
            {
                // get page content
                var pageContent = await WebScraper!.GetFullWebPageContentAsync(AdsUrl)
                                                   .ConfigureAwait(false);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageContent);
                htmlDoc.OptionDefaultStreamEncoding = PageEncoding;

                Logger?.LogDebug($"({Name}): Downloaded page with ads.");

                // parse posts
                var posts = htmlDoc.DocumentNode
                                   .SelectNodes(GetPathToAdsElements())?
                                   .Select(ParseRealEstateAdPost)
                                   .ToList() ?? [];

                Logger?.LogDebug($"({Name}): Parsed {posts.Count} ads from page.");

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

        private async Task<IList<RealEstateAdPost>> GetAdsDirectlyAsync()
        {
            try
            {
                // get page content
                var pageContent = await HtmlWeb!.LoadFromWebAsync(AdsUrl, PageEncoding)
                                                .ConfigureAwait(false);

                Logger?.LogDebug($"({Name}): Downloaded page with ads.");

                var posts = pageContent.DocumentNode
                                       .SelectNodes(GetPathToAdsElements())?
                                       .Select(ParseRealEstateAdPost)
                                       .ToList() ?? [];

                Logger?.LogDebug($"({Name}): Parsed {posts.Count} ads from page.");

                return posts;
            }
            catch (Exception ex)
            {
                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
            }
        }
    }
}
