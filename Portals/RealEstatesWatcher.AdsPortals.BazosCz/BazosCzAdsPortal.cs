using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.BazosCz;

public partial class BazosCzAdsPortal : RealEstateAdsPortalBase
{
    [GeneratedRegex(@"\[([0-9.\s]+)\]")]
    private static partial Regex DateTimeParseRegex();

    public override string Name => "Bazoš.cz";

    public BazosCzAdsPortal(string watchedUrl,
        ILogger<BazosCzAdsPortal>? logger = null) : base(watchedUrl, logger)
    {
    }
        
    protected override string GetPathToAdsElements() => @"//div[@class=""maincontent""]/div[contains(@class,""inzeraty inzeratyflex"")]";
        
    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Text = ParseText(node),
        Price = ParsePrice(node),
        PriceComment = ParsePriceComment(node),
        Currency = Currency.CZK,
        Layout = ParseLayout(node),
        Address = ParseAddress(node),
        WebUrl = ParseWebUrl(node, RootHost),
        PublishTime = ParsePublishDate(node),
        ImageUrl = ParseImageUrl(node),
        FloorArea = ParseFloorArea(node)
    };

    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(""".//*[@class="nadpis"]""").InnerText;

    private static string ParseText(HtmlNode node) => node.SelectSingleNode(""".//div[@class="popis"]""").InnerText;

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode("""./div[@class="inzeratylok"]""").InnerHtml.Replace("<br>", " ");

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.SelectSingleNode(""".//*[@class="nadpis"]""").FirstChild.GetAttributeValue("href", string.Empty);

        return new Uri(rootHost + relativePath);
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var path = node.SelectSingleNode(""".//img[@class="obrazek"]""")?.GetAttributeValue("src", null);

        return path is not null
            ? new Uri(path)
            : null;
    }

    private static DateTime? ParsePublishDate(HtmlNode node)
    {
        const string dateTimeFormat = "d.M.yyyy";

        var value = node.SelectSingleNode(""".//span[@class="velikost10"]""")?.InnerText;
        if (value is null)
            return null;

        var result = DateTimeParseRegex().Match(value);
        if (!result.Success)
            return null;

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

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);
            
        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var value = ParseTitle(node);

        var result = RegexMatchers.FloorArea().Match(value);
        if (!result.Success)
        {
            value = ParseText(node);
            result = RegexMatchers.FloorArea().Match(value);
            if (!result.Success)
                return decimal.Zero;
        }

        var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        floorAreaValue = floorAreaValue.Replace(".", ",")
                                       .Replace(" ", string.Empty)
                                       .Trim(',');

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
}