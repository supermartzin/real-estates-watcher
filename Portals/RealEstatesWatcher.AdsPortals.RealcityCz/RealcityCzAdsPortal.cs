using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RealcityCz;

public class RealcityCzAdsPortal(string watchedUrl,
                                 ILogger<RealEstateAdsPortalBase>? logger = null) : RealEstateAdsPortalBase(watchedUrl, logger)
{
    public override string Name => "Realcity.cz";

    protected override string GetPathToAdsElements() => "//div[@class=\"media advertise item\"]";

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
        PriceComment = ParsePriceComment(node),
        ImageUrl = ParseImageUrl(node)
    };

    private static string ParseTitle(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"title\"]").InnerText).Trim();

    private static string ParseText(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"description\"]").InnerText).Trim();

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//div[@class=\"price\"]/span")?.InnerText;
        if (value is null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static string? ParsePriceComment(HtmlNode node) => ParsePrice(node) is decimal.Zero
        ? node.SelectSingleNode(".//div[@class=\"price\"]/span")?.InnerText?.Trim()
        : null;

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

    private static string ParseAddress(HtmlNode node) => HttpUtility.HtmlDecode(node.SelectSingleNode(".//div[@class=\"address\"]").InnerText).Trim();

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.SelectSingleNode(".//div[@class=\"title\"]/a").GetAttributeValue("href", string.Empty);

        return new Uri(rootHost + relativePath);
    }
        
    private static decimal ParseFloorArea(HtmlNode node)
    {
        var value = HttpUtility.HtmlDecode(ParseTitle(node)).Replace(NonBreakingSpace, string.Empty);

        var result = RegexMatchers.FloorArea().Match(value);
        if (!result.Success)
        {
            value = ParseText(node);
            result = RegexMatchers.FloorArea().Match(value);
            if (!result.Success)
                return decimal.Zero;
        }

        var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;

        return decimal.TryParse(floorAreaValue, out var floorArea)
            ? floorArea
            : decimal.Zero;
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(".//div[contains(@class,\"image\")]//img")?.GetAttributeValue("src", null);

        return path is not null
            ? new Uri($"https://{path[2..]}")   // skip leading '//' characters
            : null;
    }
}