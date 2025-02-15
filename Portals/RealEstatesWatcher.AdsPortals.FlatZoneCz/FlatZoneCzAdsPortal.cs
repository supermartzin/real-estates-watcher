﻿using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Scrapers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.FlatZoneCz;

public class FlatZoneCzAdsPortal(string watchedUrl,
                                 IWebScraper webScraper,
                                 ILogger<FlatZoneCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, webScraper, logger)
{
    public override string Name => "FlatZone.cz";

    public override async Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync()
    {
        try
        {
            // get page content
            var pageContent = await WebScraper!.GetFullWebPageContentAsync(WatchedUrl)
                .ConfigureAwait(false);
            if (pageContent == null)
                throw new RealEstateAdsPortalException("Page content has not been correctly downloaded.");

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageContent);
                
            Logger?.LogDebug("({Name}): Downloaded page with ads.", Name);
                
            // get HTML elements
            var elements = htmlDoc.DocumentNode.SelectNodes(GetPathToAdsElements());
                
            // remove first and last elements - templates
            if (elements.Count > 1)
            {
                elements.RemoveAt(0);
                elements.RemoveAt(elements.Count - 1);
            }

            // parse posts
            var posts = elements.Select(ParseRealEstateAdPost).ToList();

            Logger?.LogDebug("({Name}): Successfully parsed {PostsCount} ads from page.", Name, posts.Count);

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

    protected override string GetPathToAdsElements() => "//div[contains(@class,\"project-apartment-card\")]";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Text = string.Empty,
        Price = ParsePrice(node),
        Currency = Currency.CZK,
        Layout = Layout.NotSpecified,
        Address = ParseAddress(node),
        WebUrl = ParseWebUrl(node),
        ImageUrl = ParseImageUrl(node)
    };
        
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

        var numberValue = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

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
            : null;
    }
}