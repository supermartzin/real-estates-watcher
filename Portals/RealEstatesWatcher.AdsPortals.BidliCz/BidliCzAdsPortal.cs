using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.BidliCz;

public partial class BidliCzAdsPortal(string watchedUrl,
                                      ILogger<BidliCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, logger)
{
    [GeneratedRegex(@":url\((.*?)\)")]
    private static partial Regex UrlRegex();

    public override string Name => "Bidli.cz";

    protected override Encoding PageEncoding => Encoding.GetEncoding("iso-8859-2");

    protected override string GetPathToAdsElements() => "//div[@class=\"items-list\"]/a[@class=\"item\"]";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node)
    {
        var currency = Currency.CZK;
        var priceComment = default(string);
        var price = ParsePrice(node);
        if (price is decimal.Zero)
        {
            priceComment = ParsePriceComment(node);
            currency = Currency.Other;
        }

        return new RealEstateAdPost
        {
            AdsPortalName = Name,
            Title = ParseTitle(node),
            Text = string.Empty,
            Price = price,
            Currency = currency,
            Layout = ParseLayout(node),
            Address = ParseAddress(node),
            WebUrl = ParseWebUrl(node, RootHost),
            AdditionalFees = decimal.Zero,
            FloorArea = ParseFloorArea(node),
            PriceComment = priceComment,
            ImageUrl = ParseImageUrl(node, RootHost)
        };
    }
        
    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//span[@class=\"kategorie\"]").InnerText;

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//span[@class=\"cena\"]").InnerText;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);
        if (string.IsNullOrEmpty(value))
            return decimal.Zero;

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static string? ParsePriceComment(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//span[@class=\"cena\"]").InnerText;

        return RegexMatchers.AtLeastOneDigitValue().IsMatch(value)
            ? null
            : value.Trim();
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

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//span[@class=\"adresa\"]").InnerText;

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.GetAttributeValue("href", string.Empty);

        return new Uri(rootHost + UrlPathSeparator + relativePath);
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

    private static Uri? ParseImageUrl(HtmlNode node, string rootHost)
    {
        var styleValue = node.SelectSingleNode(".//span[@class=\"img\"]")?.GetAttributeValue("style", null);
        if (styleValue is null)
            return null;

        var result = UrlRegex().Match(styleValue);
        if (!result.Success)
            return null;

        var relativePath = result.Groups.Skip<Group>(1).First(group => group.Success).Value;

        return new Uri(rootHost + UrlPathSeparator + relativePath);
    }
}