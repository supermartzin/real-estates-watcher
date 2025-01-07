using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Scrapers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.SrealityCz;

public class SrealityCzAdsPortal(string watchedUrl,
                                 IWebScraper webScraper, 
                                 ILogger<SrealityCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, webScraper, logger)
{
    public override string Name => "Sreality.cz";

    protected override string GetPathToAdsElements() => "//div[@class=\"dir-property-list\"]/div[contains(@class,\"property\")]";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Text = string.Empty,
        Price = ParsePrice(node),
        Currency = Currency.CZK,
        Layout = ParseLayout(node),
        Address = ParseAddress(node),
        WebUrl = ParseWebUrl(node, RootHost),
        FloorArea = ParseFloorArea(node),
        ImageUrl = ParseImageUrl(node),
        PriceComment = ParsePriceComment(node)
    };

    private static string ParseTitle(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode("./div//a[@class=\"title\"]").InnerText.Trim());

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode("./div//span[contains(@class,\"locality\")]").InnerText;

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.FirstChild.GetAttributeValue("href", string.Empty);

        return new Uri(rootHost + relativePath);
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.FirstChild?.SelectSingleNode(".//img[@class=\"img\"]")?.GetAttributeValue("src", null);

        return path is not null
            ? new Uri(path)
            : null;
    }

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//span[contains(@class,\"norm-price\")]")?.InnerText;
        if (value is null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static Layout ParseLayout(HtmlNode node)
    {
        var value = node.SelectSingleNode("./div//a[@class=\"title\"]").InnerText.Trim();
        value = HttpUtility.HtmlDecode(value);

        var result = RegexMatchers.Layout().Match(value);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.Layout().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }

    private static string? ParsePriceComment(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//span[contains(@class,\"norm-price\")]")?.InnerText;
        if (value is null)
            return null;

        value = HttpUtility.HtmlDecode(value);
        var result = RegexMatchers.AtLeastOneDigitValue().Match(value);

        return !result.Success && value.Length > 0 ? value : null;
    }

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
}