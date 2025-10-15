using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.CeskeRealityCz;

public partial class CeskeRealityCzAdsPortal(string watchedUrl,
                                             ILogger<CeskeRealityCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, logger)
{
    [GeneratedRegex(@"m²(.*)")]
    private static partial Regex AddressRegex();

    public override string Name => "České reality.cz";

    protected override string GetPathToAdsElements() => "//div[@class='g-estates']/article";

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
        ImageUrl = ParseImageUrl(node),
        PriceComment = ParsePriceComment(node)
    };

    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//h2[@class='i-estate__header-title']").InnerText.Trim();

    private static string ParseText(HtmlNode node) => node.SelectSingleNode(".//p[@class='i-estate__description-text']").InnerText.Trim();

    private static decimal ParsePrice(HtmlNode node)
    {
        return ParsePriceFromNode(node, ".//h3[@class='i-estate__footer-price-value']");
    }

    private static string? ParsePriceComment(HtmlNode node)
    {
        return GetPriceCommentWhenZero(ParsePrice(node), node, ".//h3[@class='i-estate__footer-price-value']");
    }

    private static Layout ParseLayout(HtmlNode node)
    {
        var value = ParseTitle(node);

        var layout = ParseLayoutFromText(value);
        if (layout != Layout.NotSpecified)
            return layout;

        value = ParseText(node);
        return ParseLayoutFromText(value);
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
        var relativePath = node.SelectSingleNode(".//h2[@class='i-estate__header-title']/a")?.GetAttributeValue<string?>("href", null);

        return relativePath is not null 
            ? new Uri($"{rootUri}{relativePath}") 
            : new Uri(rootUri);
    }

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var value = ParseTitle(node);
        return ParseFloorAreaFromText(value);
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(".//div[contains(@class, \"img\")]/picture/source[@type=\"image/jpeg\"]")?.GetAttributeValue<string?>("srcset", null);

        return path is not null
            ? new Uri(path) 
            : null;
    }
}