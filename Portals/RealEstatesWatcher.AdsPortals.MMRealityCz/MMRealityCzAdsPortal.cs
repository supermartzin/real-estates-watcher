using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.MMRealityCz;

public class MmRealityCzAdsPortal(string watchedUrl,
                                  ILogger<MmRealityCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, logger)
{
    public override string Name => "M&M Reality.cz";

    protected override string GetPathToAdsElements() => "//div[contains(@class,\"grid-x\")]//div[contains(@class, \"cell\")]//a[@data-realty-name]/..";

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
        FloorArea = ParseFloorArea(node),
        ImageUrl = ParseImageUrl(node)
    };

    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode("./p[1]").LastChild.InnerText.Trim();

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode("./strong[contains(@class,\"text-secondary\")]")?.InnerText;
        if (value == null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
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

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode("./p[1]").FirstChild.InnerText.Trim();

    private static Uri ParseWebUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode("./a[contains(@class,\"text-underline\")]").GetAttributeValue("href", null);
            
        return new Uri(path);
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

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(".//img[1]")?.GetAttributeValue("src", null);

        return path is not null
            ? new Uri(path)
            : default;
    }
}