using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;
using RealEstatesWatcher.Scrapers.Contracts;

namespace RealEstatesWatcher.AdsPortals.BezrealitkyCz;

public class BezrealitkyCzAdsPortal(string watchedUrl,
                                    IWebScraper webScraper,
                                    ILogger<BezrealitkyCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, webScraper, logger)
{
    public override string Name => "Bezrealitky.cz";

    protected override string GetPathToAdsElements() => "(//section[contains(@class,'box')])[last()]/article";

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
        AdditionalFees = ParseAdditionalFees(node),
        FloorArea = ParseFloorArea(node),
        ImageUrl = ParseImageUrl(node, RootHost)
    };

    private static string ParseTitle(HtmlNode node)
    {
        var label = node.SelectSingleNode(".//span[contains(@class,'propertyCardLabel')]").InnerText;
        var address = node.SelectSingleNode(".//span[contains(@class,'propertyCardAddress')]").InnerText;

        return $"{label} {address}";
    }

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
        if (value == null)
            return decimal.Zero;

        if (!value.Contains('+'))
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
        if (values.Count != 2)
            return decimal.Zero;

        var value = HttpUtility.HtmlDecode(values[0].InnerText);

        var result = RegexMatchers.Layout().Match(value);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//span[contains(@class,'propertyCardAddress')]").InnerText;

    private static Uri ParseWebUrl(HtmlNode node) => new(node.SelectSingleNode(".//h2[contains(@class,'propertyCardHeadline')]//a").GetAttributeValue("href", string.Empty));

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

    private static Uri? ParseImageUrl(HtmlNode node, string hostUrlPart)
    {
        var path = node.SelectSingleNode(".//li[contains(@class,'image')]")?
                       .FirstChild?
                       .SelectSingleNode(".//img")?
                       .GetAttributeValue("srcset", null);

        if (path is null)
            return null;
            
        return path.Contains(hostUrlPart)
            ? new Uri(path)
            : new Uri(hostUrlPart + path);
    }
}