using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.BazosCz;

public class BazosCzAdsPortal : RealEstateAdsPortalBase
{
    public override string Name => "Bazoš.cz";

    public BazosCzAdsPortal(string adsUrl,
        ILogger<BazosCzAdsPortal>? logger = default) : base(adsUrl, logger)
    {
    }
        
    protected override string GetPathToAdsElements() => @"//div[@class=""maincontent""]/div[contains(@class,""inzeraty inzeratyflex"")]";
        
    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new(Name,
        ParseTitle(node),
        ParseText(node),
        ParsePrice(node),
        Currency.CZK,
        ParseLayout(node),
        ParseAddress(node),
        ParseWebUrl(node, RootHost),
        decimal.Zero,
        ParseFloorArea(node),
        imageUrl: ParseImageUrl(node),
        publishTime: ParsePublishDate(node),
        priceComment: ParsePriceComment(node));

    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(@".//*[@class=""nadpis""]").InnerText;

    private static string ParseText(HtmlNode node) => node.SelectSingleNode(@".//div[@class=""popis""]").InnerText;

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(@"./div[@class=""inzeratylok""]").InnerHtml.Replace("<br>", " ");

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.SelectSingleNode(@".//*[@class=""nadpis""]").FirstChild.GetAttributeValue("href", string.Empty);

        return new Uri(rootHost + relativePath);
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(@".//img[@class=""obrazek""]")?.GetAttributeValue("src", null);

        return path is not null
            ? new Uri(path)
            : default;
    }

    private static DateTime? ParsePublishDate(HtmlNode node)
    {
        const string dateTimeFormat = "d.M.yyyy";
        const string dateTimeParseRegex = @"\[([0-9.\s]+)\]";

        var value = node.SelectSingleNode(@".//span[@class=""velikost10""]")?.InnerText;
        if (value is null)
            return default;

        var result = Regex.Match(value, dateTimeParseRegex);
        if (!result.Success)
            return default;

        var dateTimeValue = result.Groups[1].Value.Replace(" ", "");

        return DateTime.TryParseExact(dateTimeValue, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var publishTime)
            ? publishTime
            : default;
    }

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(@"./div[@class=""inzeratycena""]")?.InnerText;
        if (value is null)
            return decimal.Zero;

        value = Regex.Replace(value, RegexPatterns.AllNonNumberValues, "");
            
        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var value = ParseTitle(node);

        var result = Regex.Match(value, RegexPatterns.FloorArea);
        if (!result.Success)
        {
            value = ParseText(node);
            result = Regex.Match(value, RegexPatterns.FloorArea);
            if (!result.Success)
                return decimal.Zero;
        }

        var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        floorAreaValue = floorAreaValue.Replace(".", ",");

        return decimal.TryParse(floorAreaValue, NumberStyles.AllowDecimalPoint, new NumberFormatInfo{ NumberDecimalSeparator = ","}, out var floorArea)
            ? floorArea
            : decimal.Zero;
    }

    private static string? ParsePriceComment(HtmlNode node) => ParsePrice(node) is decimal.Zero
        ? node.SelectSingleNode("./div[@class=\"inzeratycena\"]")?.InnerText?.Trim()
        : null;

    private static Layout ParseLayout(HtmlNode node)
    {
        var value = ParseTitle(node);

        var result = Regex.Match(value, RegexPatterns.Layout);
        if (!result.Success)
        {
            value = ParseText(node);
            result = Regex.Match(value, RegexPatterns.Layout);
            if (!result.Success)
                return Layout.NotSpecified;
        }

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = Regex.Replace(layoutValue, RegexPatterns.AllWhitespaceValues, "");

        return LayoutExtensions.ToLayout(layoutValue);
    }
}