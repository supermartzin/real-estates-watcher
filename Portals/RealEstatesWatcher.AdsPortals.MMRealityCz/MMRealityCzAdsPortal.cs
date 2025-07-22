using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.MMRealityCz;

public partial class MmRealityCzAdsPortal(string watchedUrl,
                                  IWebScraper webScraper,
                                  ILogger<MmRealityCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, webScraper, logger)
{
    [GeneratedRegex(@".*,\s(.+,\s.+)")]
    private static partial Regex AddressRegex();

    public override string Name => "M&M Reality";

    protected override string GetPathToAdsElements() => "//div[@id='offers-list']/a[./article]";
    
    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Text = string.Empty,
        Price = ParsePrice(node),
        PriceComment = ParsePriceComment(node),
        Currency = Currency.CZK,
        Layout = ParseLayout(node),
        Address = ParseAddress(node),
        WebUrl = ParseWebUrl(node),
        FloorArea = ParseFloorArea(node),
        ImageUrl = ParseImageUrl(node)
    };

    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//h4[contains(@class, 'rds-property-title')]").InnerText;

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//div[@class='rds-content']//div[contains(@class, 'price')]")?.InnerText;
        if (value is null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static string? ParsePriceComment(HtmlNode node) => node.SelectSingleNode(".//div[@class='rds-content']//div[contains(@class, 'price')]")?.InnerText;

    private static Layout ParseLayout(HtmlNode node)
    {
        var title = ParseTitle(node);

        var result = RegexMatchers.Layout().Match(title);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }

    private static string ParseAddress(HtmlNode node)
    {
        var title = node.SelectSingleNode(".//button[contains(@class, 'rds-favorite-icon')]")?.GetAttributeValue<string?>("data-realty-name", null);
        if (title is null)
            return string.Empty;

        var result = AddressRegex().Match(title);

        return result.Success
            ? result.Groups.Skip<Group>(1).First(group => group.Success).Value.Trim()
            : string.Empty;
    }

    private Uri ParseWebUrl(HtmlNode node) => new($"{RootHost}{node.PreviousSibling.GetAttributeValue("href", string.Empty)}");

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var title = ParseTitle(node);

        // workaround for the case like "Prodej bytu 4+1 111 m²" when it parses to "1111 m²"
        var layout = RegexMatchers.Layout().Match(title);
        if (layout.Success)
            title = title.Replace(layout.Groups[1].Value, string.Empty);

        var result = RegexMatchers.FloorArea().Match(title);
        if (!result.Success)
            return decimal.Zero;

        var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        floorAreaValue = RegexMatchers.AllNonNumberValues().Replace(floorAreaValue, string.Empty);

        return decimal.TryParse(floorAreaValue, out var floorArea)
            ? floorArea
            : decimal.Zero;
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(".//div[@class='rds-image-carousel']//img[@class='rds-image']")?.GetAttributeValue<string?>("src", null);

        return path is not null
            ? new Uri(path)
            : null;
    }
}