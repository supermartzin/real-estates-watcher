using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.BidliCz;

public class BidliCzAdsPortal : RealEstateAdsPortalBase
{
    public override string Name => "Bidli.cz";

    public BidliCzAdsPortal(string watchedUrl,
                            ILogger<BidliCzAdsPortal>? logger = default) : base(watchedUrl, logger)
    {
        PageEncoding = Encoding.GetEncoding("iso-8859-2");
    }

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
         
        return new RealEstateAdPost(Name,
            ParseTitle(node),
            string.Empty,
            price,
            currency,
            ParseLayout(node),
            ParseAddress(node),
            ParseWebUrl(node, RootHost),
            decimal.Zero,
            ParseFloorArea(node),
            priceComment,
            ParseImageUrl(node, RootHost));
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

        return Regex.IsMatch(value, @"\d")
            ? default
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
        const string urlRegexPattern = @":url\((.*?)\)";

        var styleValue = node.SelectSingleNode(".//span[@class=\"img\"]")?.GetAttributeValue("style", null);
        if (styleValue is null)
            return default;

        var result = Regex.Match(styleValue, urlRegexPattern);
        if (!result.Success)
            return default;

        var relativePath = result.Groups.Skip<Group>(1).First(group => group.Success).Value;

        return new Uri(rootHost + UrlPathSeparator + relativePath);
    }
}