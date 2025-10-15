using System.Text;
using System.Text.RegularExpressions;

using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.Base;

public abstract partial class RealEstateAdsPortalBase : IRealEstateAdsPortal
{
    protected static partial class RegexMatchers
    {
        [GeneratedRegex(@"\D+")]
        public static partial Regex AllNonNumberValues();

        [GeneratedRegex(@"\d")]
        public static partial Regex AtLeastOneDigitValue();

        [GeneratedRegex(@"(2\s?\+?\s?kk|1\s?\+?\s?kk|2\s?\+\s?1|1\s?\+\s?1|3\s?\+\s?1|3\s?\+?\s?kk|4\s?\+\s?1|4\s?\+?\s?kk|5\s?\+\s?1|5\s?\+?\s?kk|2\s?\+?\s?KK|1\s?\+?\s?KK|2\s?\+\s?1|1\s?\+\s?1|3\s?\+\s?1|3\s?\+?\s?KK|4\s?\+\s?1|4\s?\+?\s?KK|5\s?\+\s?1|5\s?\+?\s?KK)")]
        public static partial Regex Layout();

        [GeneratedRegex(@"(\s+)")]
        public static partial Regex AllWhitespaceCharacters();

        [GeneratedRegex(@"([\d]+?)\s*?m2|([\d]+?[,. ]?[\d]+?)\s*?m2|([\d]+?)\s*?m²|([\d]+?[,. ]?[\d]+?)\s*?m²|([\d]+?)\s*?m|([\d]+?[,. ]?[\d]+?)\s*?m")]
        public static partial Regex FloorArea();
    }

    protected const char UrlPathSeparator = '/';
    protected const string NonBreakingSpace = "\u00A0";

    protected readonly ILogger<RealEstateAdsPortalBase>? Logger;
    protected readonly IWebScraper? WebScraper;
    protected readonly HtmlWeb? HtmlWeb;
    protected readonly string RootHost;

    protected virtual Encoding PageEncoding { get; init; } = Encoding.UTF8;

    public abstract string Name { get; }

    public string WatchedUrl { get; }

    protected RealEstateAdsPortalBase(string watchedUrl,
        ILogger<RealEstateAdsPortalBase>? logger = null)
    {
        WatchedUrl = watchedUrl ?? throw new ArgumentNullException(nameof(watchedUrl));
        RootHost = ParseRootHost(watchedUrl);
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

    protected RealEstateAdsPortalBase(string watchedUrl,
        IWebScraper webScraper,
        ILogger<RealEstateAdsPortalBase>? logger = null) : this(watchedUrl, logger)
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
            var pageContent = await WebScraper!.GetFullWebPageContentAsync(WatchedUrl, PageEncoding)
                .ConfigureAwait(false);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContent);
            htmlDoc.OptionDefaultStreamEncoding = PageEncoding;

            Logger?.LogDebug("({Name}): Downloaded page with ads.", Name);

            // parse posts
            var posts = htmlDoc.DocumentNode
                               .SelectNodes(GetPathToAdsElements())?
                               .Select(ParseRealEstateAdPost)
                               .ToList() ?? [];


            Logger?.LogDebug("({Name}): Parsed {PostsCount} ads from page.", Name, posts.Count);

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
            var pageContent = await HtmlWeb!.LoadFromWebAsync(WatchedUrl, PageEncoding)
                .ConfigureAwait(false);

            Logger?.LogDebug("({Name}): Downloaded page with ads.", Name);

            var posts = pageContent.DocumentNode
                .SelectNodes(GetPathToAdsElements())?
                .Select(ParseRealEstateAdPost)
                .ToList() ?? [];

            Logger?.LogDebug("({Name}): Parsed {PostsCount} ads from page.", Name, posts.Count);

            return posts;
        }
        catch (Exception ex)
        {
            throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses a price value from a node's inner text by removing all non-numeric characters.
    /// </summary>
    /// <param name="node">The HTML node containing the price information</param>
    /// <param name="xpath">XPath to select the specific node containing the price</param>
    /// <returns>The parsed decimal price, or decimal.Zero if parsing fails</returns>
    protected static decimal ParsePriceFromNode(HtmlNode node, string xpath)
    {
        var value = node.SelectSingleNode(xpath)?.InnerText;
        if (value is null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    /// <summary>
    /// Parses layout information from text using regex pattern matching.
    /// </summary>
    /// <param name="text">The text to parse for layout information</param>
    /// <returns>The parsed Layout enum value, or Layout.NotSpecified if not found</returns>
    protected static Layout ParseLayoutFromText(string text)
    {
        var result = RegexMatchers.Layout().Match(text);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }

    /// <summary>
    /// Parses floor area from text using regex pattern matching.
    /// </summary>
    /// <param name="text">The text to parse for floor area information</param>
    /// <returns>The parsed decimal floor area, or decimal.Zero if not found</returns>
    protected static decimal ParseFloorAreaFromText(string text)
    {
        var result = RegexMatchers.FloorArea().Match(text);
        if (!result.Success)
            return decimal.Zero;

        var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;

        return decimal.TryParse(floorAreaValue, out var floorArea)
            ? floorArea
            : decimal.Zero;
    }

    /// <summary>
    /// Returns the price comment when the price is zero, otherwise returns null.
    /// </summary>
    /// <param name="price">The price value to check</param>
    /// <param name="node">The HTML node containing the price comment</param>
    /// <param name="xpath">XPath to select the specific node containing the price comment</param>
    /// <returns>The price comment text if price is zero, otherwise null</returns>
    protected static string? GetPriceCommentWhenZero(decimal price, HtmlNode node, string xpath)
    {
        return price is decimal.Zero
            ? node.SelectSingleNode(xpath)?.InnerText?.Trim()
            : null;
    }
}