using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.Base
{
    public abstract class RealEstateAdsPortalBase : IRealEstateAdsPortal
    {
        protected readonly ILogger<RealEstateAdsPortalBase>? Logger;
        protected readonly IWebScraper? WebScraper;

        protected readonly string AdsUrl;
        protected readonly string RootHost;

        public abstract string Name { get; }

        protected RealEstateAdsPortalBase(string adsUrl,
                                          ILogger<RealEstateAdsPortalBase>? logger = default)
        {
            AdsUrl = adsUrl ?? throw new ArgumentNullException(nameof(adsUrl));
            RootHost = ParseRootHost(adsUrl);
            Logger = logger;
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
                if (pageContent == null)
                    throw new RealEstateAdsPortalException("Page content has not been correctly downloaded.");

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageContent);

                Logger?.LogDebug($"({Name}): Downloaded page with ads.");

                // parse posts
                var posts = htmlDoc.DocumentNode
                                   .SelectNodes(GetPathToAdsElements())
                                   .Select(ParseRealEstateAdPost)
                                   .ToList();

                Logger?.LogDebug($"({Name}): Successfully parsed {posts.Count} ads from page.");

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
                var webHtml = new HtmlWeb();

                // get page content
                var pageContent = await webHtml.LoadFromWebAsync(AdsUrl)
                                               .ConfigureAwait(false);

                Logger?.LogDebug($"({Name}): Downloaded page with ads.");

                var posts = pageContent.DocumentNode
                                       .SelectNodes(GetPathToAdsElements())
                                       .Select(ParseRealEstateAdPost)
                                       .ToList();

                Logger?.LogDebug($"({Name}): Successfully parsed {posts.Count} ads from page.");

                return posts;
            }
            catch (Exception ex)
            {
                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
            }
        }
    }
}
