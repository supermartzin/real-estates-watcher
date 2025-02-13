using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.CeskeRealityCz;

public partial class CeskeRealityCzAdsPortal : RealEstateAdsPortalBase
{
    [GeneratedRegex(@"m²(.*)")]
    private static partial Regex AddressRegex();

    public CeskeRealityCzAdsPortal(string watchedUrl,
                                   ILogger<CeskeRealityCzAdsPortal>? logger = null) : base(watchedUrl, logger)
    {
    }

    public override string Name => "České reality.cz";

    protected override string GetPathToAdsElements() => "//div[@class=\"g-estates\"]/article";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Text = ParseText(node),
        Price = ParsePrice(node),
        Currency = Currency.CZK,
        Layout = ParseLayout(node),
        Address = ParseAddress(node),
        WebUrl = ParseWebUrl(node, RootHost),
        FloorArea = ParseFloorArea(node),
        ImageUrl = ParseImageUrl(node)
    };
        
    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//h2[@class=\"i-estate__header-title\"]").InnerText.Trim();

    private static string ParseText(HtmlNode node) => node.SelectSingleNode(".//p[@class=\"i-estate__description-text\"]").InnerText.Trim();

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//h3[@class=\"i-estate__footer-price-value\"]").InnerText;

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
        {
            value = ParseText(node);
            result = RegexMatchers.Layout().Match(value);
            if (!result.Success)
                return Layout.NotSpecified;
        }

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }

    private static string ParseAddress(HtmlNode node)
    {
        var title = ParseTitle(node);

        var result = AddressRegex().Match(title);
        return result.Success 
            ? result.Groups.Skip<Group>(1).First(group => group.Success).Value.Trim()
            : string.Empty;
    }

    private static Uri ParseWebUrl(HtmlNode node, string rootUri)
    {
        var relativePath = node.SelectSingleNode(".//h2[@class=\"i-estate__header-title\"]/a")?.GetAttributeValue("href", null);

        return relativePath is not null 
            ? new Uri($"{rootUri}{relativePath}") 
            : new Uri(rootUri);
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
        var path = node.SelectSingleNode(".//div[contains(@class, \"img\")]/picture/source[@type=\"image/jpeg\"]")?.GetAttributeValue("srcset", null);

        return path is not null
            ? new Uri(path) 
            : null;
    }
}