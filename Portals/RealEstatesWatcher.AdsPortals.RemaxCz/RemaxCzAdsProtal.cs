using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RemaxCz;

public class RemaxCzAdsProtal : RealEstateAdsPortalBase
{
    public override string Name => "RE/MAX CZ";

    public RemaxCzAdsProtal(string watchedUrl,
                            ILogger<RemaxCzAdsProtal>? logger = null) : base(watchedUrl, logger)
    {
    }

    protected override string GetPathToAdsElements() => "//div[@class='pl-items__item']";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Text = string.Empty,
        Price = ParsePrice(node),
        Currency = ParsePrice(node) is not decimal.Zero ? Currency.CZK : Currency.Other,
        Layout = ParseLayout(node),
        Address = ParseAddress(node),
        WebUrl = ParseWebUrl(node, RootHost),
        FloorArea = ParseFloorArea(node),
        PriceComment = ParsePriceComment(node),
        ImageUrl = ParseImageUrl(node)
    };

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//div[contains(@class,\"item-price\")]/strong")?.FirstChild?.InnerText;
        if (value is null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static string? ParsePriceComment(HtmlNode node) => ParsePrice(node) is decimal.Zero
        ? node.SelectSingleNode(".//div[contains(@class,'item-price')]/strong")?.InnerText?.Trim()
        : null;

    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//h2/strong").InnerText;

    private static string ParseAddress(HtmlNode node)
    {
        var address = node.SelectSingleNode(".//div[contains(@class,'item-info')]//p").InnerText?.Trim();

        if (address is null)
            return string.Empty;

        address = HttpUtility.HtmlDecode(address);
        address = RegexMatchers.AllWhitespaceCharacters().Replace(address, " ");
        address = address.TrimEnd(',', '.');

        return address;
    }

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.SelectSingleNode("./a").GetAttributeValue("href", string.Empty);

        return new Uri(rootHost + relativePath);
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(".//div[@class='pl-items__images']//img")?.GetAttributeValue("data-src", null);

        return path is not null
            ? new Uri(path)
            : null;
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

    private static Layout ParseLayout(HtmlNode node)
    {
        var value = ParseTitle(node);

        var result = RegexMatchers.Layout().Match(value);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }
}