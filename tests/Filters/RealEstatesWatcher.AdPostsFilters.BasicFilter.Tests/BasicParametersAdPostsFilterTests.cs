using RealEstatesWatcher.AdPostsFilters.BasicFilter;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class BasicParametersAdPostsFilterTests
{
    [Fact]
    public void Constructor_RejectsNullSettings() =>
        Assert.Throws<ArgumentNullException>(() => new BasicParametersAdPostsFilter(null!));

    [Fact]
    public void Filter_RejectsNullPosts()
    {
        var filter = new BasicParametersAdPostsFilter(new BasicParametersAdPostsFilterSettings());

        Assert.Throws<ArgumentNullException>(() => filter.Filter(null!));
    }

    [Fact]
    public void Filter_AppliesAllConfiguredBounds()
    {
        var filter = new BasicParametersAdPostsFilter(new BasicParametersAdPostsFilterSettings
        {
            MinPrice = 2_000_000m,
            MaxPrice = 4_000_000m,
            MinFloorArea = 50m,
            MaxFloorArea = 80m,
            Layouts = new HashSet<Layout> { Layout.TwoPlusKk }
        });
        var posts = new[]
        {
            TestData.CreatePost("match"),
            TestData.CreatePost("cheap", price: 1_999_999m),
            TestData.CreatePost("expensive", price: 4_000_001m),
            TestData.CreatePost("small", floorArea: 49m),
            TestData.CreatePost("large", floorArea: 81m),
            TestData.CreatePost("layout", layout: Layout.ThreePlusKk)
        };

        var result = filter.Filter(posts).ToList();

        Assert.Collection(result, post => Assert.EndsWith("/match", post.WebUrl.AbsolutePath));
    }

    [Fact]
    public void Filter_IncludesInclusiveBoundaryValues()
    {
        var filter = new BasicParametersAdPostsFilter(new BasicParametersAdPostsFilterSettings
        {
            MinPrice = 2_500_000m,
            MaxPrice = 2_500_000m,
            MinFloorArea = 60m,
            MaxFloorArea = 60m
        });

        Assert.Single(filter.Filter([TestData.CreatePost()]));
    }

    [Fact]
    public void Filter_KeepsListingsWithUnknownPriceAreaOrLayout()
    {
        var filter = new BasicParametersAdPostsFilter(new BasicParametersAdPostsFilterSettings
        {
            MinPrice = 1m,
            MaxPrice = 2m,
            MinFloorArea = 1m,
            MaxFloorArea = 2m,
            Layouts = new HashSet<Layout> { Layout.FivePlusOne }
        });
        var unknown = TestData.CreatePost(
            price: decimal.Zero,
            floorArea: decimal.Zero,
            layout: Layout.NotSpecified);

        Assert.Single(filter.Filter([unknown]));
    }

    [Fact]
    public void Filter_IsDeferredAndDoesNotModifyTheInput()
    {
        var posts = new List<RealEstateAdPost> { TestData.CreatePost("first") };
        var filter = new BasicParametersAdPostsFilter(new BasicParametersAdPostsFilterSettings());
        var result = filter.Filter(posts);
        posts.Add(TestData.CreatePost("second"));

        Assert.Equal(2, result.Count());
        Assert.Equal(2, posts.Count);
    }
}
