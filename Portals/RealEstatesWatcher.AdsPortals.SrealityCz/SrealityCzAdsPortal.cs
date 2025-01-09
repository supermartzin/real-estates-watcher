using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Scrapers.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.SrealityCz;

public class SrealityCzAdsPortal(string watchedUrl,
                                 IWebScraper webScraper, 
                                 ILogger<SrealityCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, webScraper, logger)
{
    public override string Name => "Sreality.cz";

    protected override string GetPathToAdsElements() => "//ul[@data-e2e='estates-list']/li[not(@data-e2e='nativni')]";

    protected override RealEstateAdPost ParseRealEstateAdPost(HtmlNode node) => new()
    {
        AdsPortalName = Name,
        Title = ParseTitle(node),
        Address = ParseAddress(node),
        Text = string.Empty,
        Price = ParsePrice(node),
        Currency = Currency.CZK,
        Layout = ParseLayout(node),
        WebUrl = ParseWebUrl(node, RootHost),
        FloorArea = ParseFloorArea(node),
        ImageUrl = ParseImageUrl(node)
    };

    private static string ParseTitle(HtmlNode node) => node.SelectNodes(".//p").Count < 1
        ? string.Empty
        : HttpUtility.HtmlDecode(node.SelectNodes(".//p")[0].InnerText.Trim());

    private static string ParseAddress(HtmlNode node) => node.SelectNodes(".//p").Count < 2 
            ? string.Empty
            : HttpUtility.HtmlDecode(node.SelectNodes(".//p")[1].InnerText.Trim());

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.FirstChild.GetAttributeValue("href", string.Empty);

        return new Uri(rootHost + relativePath);
    }

    private static Uri? ParseImageUrl(HtmlNode node)
    {
        var imageNodes = node.SelectSingleNode(".//ul/li")?.SelectNodes(".//img");

        var path = imageNodes?.Count switch
        {
            1 => imageNodes[0].GetAttributeValue("src", null),
            2 => imageNodes[1].GetAttributeValue("src", null),
            _ => null
        };

        return path is not null
            ? new Uri($"https:{path}")
            : null;
    }

    private static decimal ParsePrice(HtmlNode node)
    {
        var descriptionNodes = node.SelectNodes(".//p");
        if (descriptionNodes.Count < 3)
            return decimal.Zero;

        var value = HttpUtility.HtmlDecode(descriptionNodes[2].InnerText);

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static Layout ParseLayout(HtmlNode node)
    {
        var result = RegexMatchers.Layout().Match(ParseTitle(node));

        return result.Success 
            ? LayoutExtensions.ToLayout(result.Groups[1].Value)
            : Layout.NotSpecified;
    }

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var title = ParseTitle(node);

        // workaround for the case like "Prodej bytu 4+1 111 m²" when it parses to "1111 m²"
        var layout = RegexMatchers.Layout().Match(title);
        if (layout.Success)
            title = title.Replace(layout.Groups[1].Value, string.Empty);

        var result = RegexMatchers.FloorArea().Match(title);
        if (!result.Success)
            return decimal.Zero;

        var value = result.Groups.Skip<Group>(1).First(group => group.Success).Value;


        return decimal.TryParse(value, out var floorArea)
            ? floorArea
            : decimal.Zero;
    }
}