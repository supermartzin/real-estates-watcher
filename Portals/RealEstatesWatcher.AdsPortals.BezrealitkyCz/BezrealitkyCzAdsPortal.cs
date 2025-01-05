using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.BezrealitkyCz;

public class BezrealitkyCzAdsPortal(string watchedUrl,
                                    IWebScraper webScraper,
                                    ILogger<BezrealitkyCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, webScraper, logger)
{
    public override string Name => "Bezrealitky.cz";

    protected override string GetPathToAdsElements() => "//article[contains(@class,\"product\")]";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Text = string.Empty,
        Price = ParsePrice(node),
        Currency = Currency.CZK,
        Layout = ParseLayout(node),
        Address = ParseAddress(node),
        WebUrl = ParseWebUrl(node),
        AdditionalFees = ParseAdditionalFees(node),
        FloorArea = ParseFloorArea(node),
        ImageUrl = ParseImageUrl(node, RootHost)
    };

    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//p[@class=\"product__note\"]").InnerText;

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//strong[@class=\"product__value\"]")?.InnerText;
        if (value == null)
            return decimal.Zero;

        if (value.Contains('+'))
            value = value.Split('+')[0];    // get first value as primary

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static decimal ParseAdditionalFees(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//strong[@class=\"product__value\"]")?.InnerText;
        if (value == null)
            return decimal.Zero;

        if (!value.Contains('+'))
            return decimal.Zero;

        // get second value as additional
        value = value.Split('+')[1];

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static Layout ParseLayout(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//p[@class=\"product__note\"]").InnerText;

        var result = RegexMatchers.Layout().Match(value);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//a[contains(@class,\"product__link\")]/strong").InnerText;

    private static Uri ParseWebUrl(HtmlNode node) => new(node.SelectSingleNode(".//a[contains(@class,\"product__link\")]").GetAttributeValue("href", string.Empty));

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var value = ParseTitle(node);

        var result = RegexMatchers.FloorArea().Match(value);
        if (!result.Success)
            return decimal.Zero;

        var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;

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