using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RealityIdnesCz;

public class RealityIdnesCzAdsPortal(string watchedUrl,
                                     ILogger<RealityIdnesCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, logger)
{
    public override string Name => "Reality.idnes.cz";

    protected override string GetPathToAdsElements() => "//div[@class=\"c-products__item\"]";

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
        PriceComment = ParsePriceComment(node),
        ImageUrl = ParseImageUrl(node)
    };

    private static string ParseTitle(HtmlNode node)
    {
        var title = node.SelectSingleNode(".//h2[@class=\"c-products__title\"]").InnerText;

        title = title.Replace("\n", " ").Trim();
        title = HttpUtility.HtmlDecode(title);
        title = title[0].ToString().ToUpper() + title[1..];

        return title;
    }

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//p[@class=\"c-products__price\"]/strong")?.InnerText;
        if (value is null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, "");

        return decimal.TryParse(value.Trim(), out var price)
            ? price
            : decimal.Zero;
    }

    private static string? ParsePriceComment(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//p[@class=\"c-products__price\"]/strong")?.InnerText;
        if (value is null)
            return null;

        return RegexMatchers.AtLeastOneDigitValue().IsMatch(value) ? null : value;
    }

    private static Layout ParseLayout(HtmlNode node)
    {
        var value = ParseTitle(node);

        var result = RegexMatchers.Layout().Match(value);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, "");

        return LayoutExtensions.ToLayout(layoutValue);
    }

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//p[@class=\"c-products__info\"]").InnerText.Trim();

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.SelectSingleNode(".//a[@class=\"c-products__link\"]").GetAttributeValue("href", string.Empty);

        return new Uri(relativePath ?? rootHost);
    }

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var value = HttpUtility.HtmlDecode(ParseTitle(node)).Replace(NonBreakingSpace, string.Empty);

        var result = RegexMatchers.FloorArea().Match(value);
        if (!result.Success)
            return decimal.Zero;

        var floorAreaValue = result.Groups
            .Skip<Group>(1)
            .First(group => group.Success)
            .Value;

        return decimal.TryParse(floorAreaValue, out var floorArea)
            ? floorArea
            : decimal.Zero;
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(".//span[@class=\"c-products__img\"]/img")?.GetAttributeValue("data-src", null);

        return path is not null
            ? new Uri(path)
            : null;
    }
}