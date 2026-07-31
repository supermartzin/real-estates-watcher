using System.Globalization;
using RealEstatesWatcher.AdPostsHandlers.Base.Html;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class HtmlBasedAdPostsHandlerTests
{
    private static readonly NumberFormatInfo NumberFormat = new()
    {
        NumberDecimalDigits = 0,
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = "."
    };

    [Fact]
    public void CreatePost_RendersListingDataAndRemovesAllPlaceholders()
    {
        var handler = new TestHtmlHandler(NumberFormat);

        var html = handler.CreatePost(TestData.CreatePost());

        Assert.Contains("Apartment", html);
        Assert.Contains("Test portal", html);
        Assert.Contains("2 500 000 CZK", html);
        Assert.Contains("3 500 CZK", html);
        Assert.Contains("60 m²", html);
        Assert.Contains("2+kk", html);
        Assert.Contains("query=Prague+1", html);
        Assert.DoesNotContain("{$", html);
    }

    [Fact]
    public void CreatePost_HidesMissingOptionalValuesAndShowsPriceComment()
    {
        var handler = new TestHtmlHandler(NumberFormat);
        var post = new RealEstateAdPost
        {
            AdsPortalName = "Portal",
            Title = "Contact us",
            Text = string.Empty,
            Price = decimal.Zero,
            PriceComment = "Price on request",
            Address = string.Empty,
            WebUrl = new Uri("https://example.test/ad"),
            Currency = Currency.Other,
            Layout = Layout.NotSpecified,
            FloorArea = null,
            AdditionalFees = null,
            ImageUrl = null
        };

        var html = handler.CreatePost(post);

        Assert.Contains("Price on request", html);
        Assert.Contains("display: none", html);
    }

    [Fact]
    public void CreatePost_WithEmptyAddress_RemovesAllTemplateTokens()
    {
        var handler = new TestHtmlHandler(NumberFormat);
        var post = new RealEstateAdPost
        {
            AdsPortalName = "Portal",
            Title = "Listing",
            Text = string.Empty,
            Price = decimal.Zero,
            Address = string.Empty,
            WebUrl = new Uri("https://example.test/ad"),
            Currency = Currency.Other,
            Layout = Layout.NotSpecified
        };

        Assert.DoesNotContain("{$", handler.CreatePost(post));
    }

    [Fact]
    public void CreatePage_RendersEveryPostInsideACompleteDocument()
    {
        var handler = new TestHtmlHandler(NumberFormat);

        var html = handler.CreatePage([TestData.CreatePost("1"), TestData.CreatePost("2", title: "Second")], "<h1>Listings</h1>");

        Assert.StartsWith("<!DOCTYPE html>", html.TrimStart());
        Assert.Contains("<h1>Listings</h1>", html);
        Assert.Contains("Apartment", html);
        Assert.Contains("Second", html);
        Assert.DoesNotContain("<maintitle/>", html);
        Assert.DoesNotContain("<posts/>", html);
    }

    private sealed class TestHtmlHandler(NumberFormatInfo numberFormat) : HtmlBasedAdPostsHandlerBase(numberFormat)
    {
        public string CreatePost(RealEstateAdPost post) => CreateHtmlPostElement(post);

        public string CreatePage(IEnumerable<RealEstateAdPost> posts, string title) => CreateFullHtmlPage(posts, title);
    }
}
