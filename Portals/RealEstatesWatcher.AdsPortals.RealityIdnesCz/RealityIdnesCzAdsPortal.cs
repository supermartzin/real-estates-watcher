using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RealityIdnesCz;

public partial class RealityIdnesCzAdsPortal(string adsUrl,
                                             ILogger<RealityIdnesCzAdsPortal>? logger = default) : RealEstateAdsPortalBase(adsUrl, logger)
{
    [GeneratedRegex(RegexPatterns.AllNonNumberValues)]
    private static partial Regex AllNonNumberValuesRegex();

    [GeneratedRegex(RegexPatterns.Layout)]
    private static partial Regex LayoutRegex();

    [GeneratedRegex(RegexPatterns.AllWhitespaceValues)]
    private static partial Regex AllWhitespaceCharactersRegex();

    [GeneratedRegex(RegexPatterns.FloorArea)]
    private static partial Regex FloorAreaRegex();

    public override string Name => "Reality.idnes.cz";

    protected override string GetPathToAdsElements() => "//div[@class=\"c-products__item\"]";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
                                                                                    ParseTitle(node),
                                                                                    string.Empty,
                                                                                    ParsePrice(node),
                                                                                    Currency.CZK,
                                                                                    ParseLayout(node),
                                                                                    ParseAddress(node),
                                                                                    ParseWebUrl(node, RootHost),
                                                                                    decimal.Zero,
                                                                                    ParseFloorArea(node),
                                                                                    imageUrl: ParseImageUrl(node));

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

        value = AllNonNumberValuesRegex().Replace(value, "");

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static Layout ParseLayout(HtmlNode node)
    {
        var value = ParseTitle(node);

        var result = LayoutRegex().Match(value);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = AllWhitespaceCharactersRegex().Replace(layoutValue, "");

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
        var value = HttpUtility.HtmlDecode(ParseTitle(node)).Trim();

        var result = FloorAreaRegex().Match(value);
        if (!result.Success)
            return decimal.Zero;

        var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;

        return decimal.TryParse(floorAreaValue, out var floorArea)
            ? floorArea
            : decimal.Zero;
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(".//span[@class=\"c-products__img\"]/img")?.GetAttributeValue("data-src", null);

        return path is not null
            ? new Uri(path)
            : default;
    }
}