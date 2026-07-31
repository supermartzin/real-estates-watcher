using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class LayoutExtensionsTests
{
    [Theory]
    [InlineData(Layout.OnePlusOne, "1+1")]
    [InlineData(Layout.OnePlusKk, "1+kk")]
    [InlineData(Layout.TwoPlusOne, "2+1")]
    [InlineData(Layout.TwoPlusKk, "2+kk")]
    [InlineData(Layout.ThreePlusOne, "3+1")]
    [InlineData(Layout.ThreePlusKk, "3+kk")]
    [InlineData(Layout.FourPlusOne, "4+1")]
    [InlineData(Layout.FourPlusKk, "4+kk")]
    [InlineData(Layout.FivePlusOne, "5+1")]
    [InlineData(Layout.FivePlusKk, "5+kk")]
    [InlineData(Layout.NotSpecified, "Not Specified")]
    public void ToDisplayString_ReturnsExpectedValue(Layout layout, string expected) =>
        Assert.Equal(expected, layout.ToDisplayString());

    [Theory]
    [InlineData("1+1", Layout.OnePlusOne)]
    [InlineData("1kk", Layout.OnePlusKk)]
    [InlineData("1+KK", Layout.OnePlusKk)]
    [InlineData("2+1", Layout.TwoPlusOne)]
    [InlineData("2kk", Layout.TwoPlusKk)]
    [InlineData("3+kk", Layout.ThreePlusKk)]
    [InlineData("4+1", Layout.FourPlusOne)]
    [InlineData("5kk", Layout.FivePlusKk)]
    [InlineData(null, Layout.NotSpecified)]
    [InlineData("studio", Layout.NotSpecified)]
    public void ToLayout_ParsesKnownValuesAndFallsBackForUnknown(string? value, Layout expected) =>
        Assert.Equal(expected, LayoutExtensions.ToLayout(value));
}

public class RealEstateAdPostTests
{
    [Fact]
    public void Price_RejectsNegativeValues() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TestData.CreatePost(price: -1m));

    [Fact]
    public void Equality_UsesUrlPathAndIgnoresQueryAndFragment()
    {
        var first = TestData.CreatePost();
        var second = new RealEstateAdPost
        {
            AdsPortalName = "Another portal",
            Title = "Changed",
            Text = string.Empty,
            Price = 1m,
            Address = string.Empty,
            WebUrl = new Uri("https://example.test/ads/1?tracking=yes#details"),
            Currency = Currency.EUR,
            Layout = Layout.NotSpecified
        };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Single(new HashSet<RealEstateAdPost> { first, second });
    }

    [Fact]
    public void Equality_DistinguishesDifferentPaths() =>
        Assert.NotEqual(TestData.CreatePost("1"), TestData.CreatePost("2"));

    [Fact]
    public void Equality_HandlesNullSameReferenceAndOtherTypes()
    {
        var post = TestData.CreatePost();

        Assert.False(post.Equals((RealEstateAdPost?)null));
        Assert.False(post.Equals(null));
        Assert.False(post.Equals("not a post"));
        Assert.True(post.Equals(post));
    }

    [Fact]
    public void OptionalValues_HaveDocumentedDefaults()
    {
        var post = new RealEstateAdPost
        {
            AdsPortalName = "Portal",
            Title = "Listing",
            Text = string.Empty,
            Price = decimal.Zero,
            Address = string.Empty,
            WebUrl = new Uri("https://example.test/listing"),
            Currency = Currency.Other,
            Layout = Layout.NotSpecified
        };

        Assert.Equal(decimal.Zero, post.FloorArea);
        Assert.Equal(decimal.Zero, post.AdditionalFees);
        Assert.Null(post.PriceComment);
        Assert.Null(post.ImageUrl);
        Assert.Null(post.PublishTime);
    }

    [Fact]
    public void ToString_ContainsTheMainListingFields()
    {
        var text = TestData.CreatePost().ToString();

        Assert.Contains("[Test portal]", text);
        Assert.Contains("Apartment", text);
        Assert.Contains("CZK 2500000", text);
        Assert.Contains("2+kk", text);
        Assert.Contains("https://example.test/ads/1", text);
    }
}
