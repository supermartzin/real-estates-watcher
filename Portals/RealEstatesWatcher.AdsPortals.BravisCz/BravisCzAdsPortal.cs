﻿using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Base;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.BravisCz;

public class BravisCzAdsPortal(string watchedUrl,
                               ILogger<BravisCzAdsPortal>? logger = null) : RealEstateAdsPortalBase(watchedUrl, logger)
{
    public override string Name => "Bravis.cz";

    protected override string GetPathToAdsElements() => "//ul[@class=\"itemslist\"]/li[not(@class)]";

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
        AdditionalFees = ParseAdditionalFees(node),
        FloorArea = ParseFloorArea(node),
        PriceComment = ParsePriceComment(node),
        ImageUrl = ParseImageUrl(node, RootHost)
    };
        
    private static string ParseTitle(HtmlNode node) => node.SelectSingleNode(".//h1").InnerText.Trim();

    private static decimal ParsePrice(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//strong[@class='price']")?.FirstChild?.InnerText;
        if (value == null)
            return decimal.Zero;

        value = RegexMatchers.AllNonNumberValues().Replace(value, string.Empty);

        return decimal.TryParse(value, out var price)
            ? price
            : decimal.Zero;
    }

    private static decimal ParseAdditionalFees(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//strong[@class='price']/small")?.InnerText;
        if (value == null)
            return decimal.Zero;

        var subValues = value.Split('+');
        var totalFees = decimal.Zero;
        foreach (var subValue in subValues)
        {
            var feeValue = subValue;

            var index = subValue.IndexOf(",-", StringComparison.InvariantCulture);
            if (index > -1)
                feeValue = subValue[..index];

            feeValue = RegexMatchers.AllNonNumberValues().Replace(feeValue, string.Empty);

            if (decimal.TryParse(feeValue, out var fee))
                totalFees += fee;
        }

        return totalFees;
    }

    private static Layout ParseLayout(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//ul[@class='params']/li[contains(text(),\"Typ\")]").InnerText;

        var result = RegexMatchers.Layout().Match(value);
        if (!result.Success)
            return Layout.NotSpecified;

        var layoutValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;
        layoutValue = RegexMatchers.AllWhitespaceCharacters().Replace(layoutValue, string.Empty);

        return LayoutExtensions.ToLayout(layoutValue);
    }

    private static string ParseAddress(HtmlNode node) => node.SelectSingleNode(".//em[@class=\"location\"]").InnerText;

    private static Uri ParseWebUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.SelectSingleNode(".//a[@class=\"main\"]").GetAttributeValue("href", null);

        return new Uri(rootHost +  UrlPathSeparator + relativePath);
    }

    private static decimal ParseFloorArea(HtmlNode node)
    {
        var value = node.SelectSingleNode(".//ul[@class='params']/li[contains(text(),\"Plocha\")]").InnerText;

        var result = RegexMatchers.FloorArea().Match(value);
        if (!result.Success)
            return decimal.Zero;

        var floorAreaValue = result.Groups.Skip<Group>(1).First(group => group.Success).Value;

        return decimal.TryParse(floorAreaValue, NumberStyles.Number, new NumberFormatInfo { NumberDecimalSeparator = "," }, out var floorArea)
            ? floorArea
            : decimal.Zero;
    }

    private static string? ParsePriceComment(HtmlNode node) => node.SelectSingleNode(".//string[@class='price']/small")?.InnerText?.Trim('(', ')');

    private static Uri? ParseImageUrl(HtmlNode node, string rootHost)
    {
        var relativePath = node.SelectSingleNode(".//a[@class=\"img\"]/img")?.GetAttributeValue("src", null);

        return relativePath is not null
            ? new Uri(rootHost + relativePath)
            : null;
    }
}