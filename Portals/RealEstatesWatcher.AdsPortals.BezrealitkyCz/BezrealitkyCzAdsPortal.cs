using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.BezrealitkyCz;

public partial class BezrealitkyCzAdsPortal(string watchedUrl,
                                            ILogger<BezrealitkyCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, logger)
{
    [GeneratedRegex("url=(.+?)&")]
    private static partial Regex ImageUrlRegex();

    public override string Name => "Bezrealitky.cz";

    protected override string GetPathToAdsElements() => "(//section[contains(@class,'box')])[last()]/article";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Text = ParseText(node),
        Price = ParsePrice(node),
        Currency = Currency.CZK,
        Layout = ParseLayout(node),
        Address = ParseAddress(node),
        WebUrl = ParseWebUrl(node),
        AdditionalFees = ParseAdditionalFees(node),
        FloorArea = ParseFloorArea(node),
        ImageUrl = ParseImageUrl(node)
    };

    private static string ParseTitle(HtmlNode node)
    {
        var label = node.SelectSingleNode(".//span[contains(@class,'propertyCardLabel')]").InnerText;
        var address = node.SelectSingleNode(".//span[contains(@class,'propertyCardAddress')]").InnerText;

        return $"{label} {address}";
    }

    private static string ParseText(HtmlNode node) => node.SelectSingleNode(".//div[contains(@class,'propertyCardContent')]//p").InnerText;

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//span[contains(@class,'propertyCardAddress')]").InnerText;

    private static Uri ParseWebUrl(HtmlNode node) => new(node.SelectSingleNode(".//h2[contains(@class,'propertyCardHeadline')]//a").GetAttributeValue("href", string.Empty));

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//span[contains(@class,'propertyPriceAmount')]")?.InnerText;
        if (value is null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static decimal ParseAdditionalFees(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//strong[@class=\"product__value\"]")?.InnerText;
        if (value is null || !value.Contains('+'))
            return decimal.Zero;

        // get second value as additional
        value = value.Split('+')[1];

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static Layout ParseLayout(HtmlNode node)
    {
        var values = node.SelectNodes(".//li[contains(@class,'featuresListItem')]");
        if (values.Count < 1)
            return Layout.NotSpecified;

        var value = HttpUtility.HtmlDecode(values[0].InnerText);

        var result = RegexMatchers.Layout().Match(value);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var values = node.SelectNodes(".//li[contains(@class,'featuresListItem')]");
        if (values.Count != 2)
            return decimal.Zero;
        
        var value = HttpUtility.HtmlDecode(values[^1].InnerText);

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
        var values = HttpUtility.UrlDecode(node.SelectSingleNode(".//span[contains(@class,'image')]//img")?
                                               .GetAttributeValue("srcset", null));

        if (values is null)
            return null;

        var result = ImageUrlRegex().Match(values);
        if (!result.Success)
            return null;

        var imageUrl = result.Groups[1].Value;

        return new Uri(imageUrl);
    }
}