using RealEstatesWatcher.AdPostsFilters.Contracts;
using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Core;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Tests;

public class RealEstatesWatchEngineTests
{
    [Fact]
    public void Constructor_RejectsNullSettings() =>
        Assert.Throws<ArgumentNullException>(() => new RealEstatesWatchEngine(null!));

    [Fact]
    public async Task Start_RequiresAtLeastOnePortal()
    {
        var engine = CreateEngine();

        var exception = await Assert.ThrowsAsync<RealEstatesWatchEngineException>(() => engine.StartAsync());

        Assert.Contains("No Ads portals", exception.Message);
    }

    [Fact]
    public async Task Start_RequiresAtLeastOneHandler()
    {
        var engine = CreateEngine();
        engine.RegisterAdsPortal(new StubPortal("Portal", [TestData.CreatePost()]));

        var exception = await Assert.ThrowsAsync<RealEstatesWatchEngineException>(() => engine.StartAsync());

        Assert.Contains("No handlers", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Start_RejectsInvalidIntervals(int interval)
    {
        var engine = CreateEngine(new WatchEngineSettings { CheckIntervalMinutes = interval });
        engine.RegisterAdsPortal(new StubPortal("Portal", [TestData.CreatePost()]));
        engine.RegisterAdPostsHandler(new RecordingHandler());

        await Assert.ThrowsAsync<RealEstatesWatchEngineException>(() => engine.StartAsync());
    }

    [Fact]
    public async Task Start_LoadsFiltersAndNotifiesEnabledHandlers()
    {
        var engine = CreateEngine();
        var handler = new RecordingHandler();
        engine.RegisterAdsPortal(new StubPortal("Portal", [TestData.CreatePost("keep"), TestData.CreatePost("drop")]));
        engine.RegisterAdPostsFilter(new DelegateFilter(posts => posts.Where(post => post.WebUrl.AbsolutePath.EndsWith("keep", StringComparison.Ordinal))));
        engine.RegisterAdPostsHandler(handler);

        await engine.StartAsync();
        try
        {
            Assert.True(engine.IsRunning);
            var batch = Assert.Single(handler.InitialBatches);
            Assert.Collection(batch, post => Assert.EndsWith("/keep", post.WebUrl.AbsolutePath));
        }
        finally
        {
            if (engine.IsRunning)
                await engine.StopAsync();
        }
    }

    [Fact]
    public async Task Start_DoesNotNotifyDisabledHandlers()
    {
        var engine = CreateEngine();
        var handler = new RecordingHandler { IsEnabled = false };
        engine.RegisterAdsPortal(new StubPortal("Portal", [TestData.CreatePost()]));
        engine.RegisterAdPostsHandler(handler);

        await engine.StartAsync();
        try
        {
            Assert.Empty(handler.InitialBatches);
        }
        finally
        {
            if (engine.IsRunning)
                await engine.StopAsync();
        }
    }

    [Fact]
    public async Task DuplicatePortalNames_AreIgnoredByDefault()
    {
        var first = new StubPortal("Same", [TestData.CreatePost("first")]);
        var duplicate = new StubPortal("Same", [TestData.CreatePost("second")]);
        var engine = CreateEngine();
        engine.RegisterAdsPortal(first);
        engine.RegisterAdsPortal(duplicate);
        engine.RegisterAdPostsHandler(new RecordingHandler());

        await engine.StartAsync();
        try
        {
            Assert.Equal(1, first.CallCount);
            Assert.Equal(0, duplicate.CallCount);
        }
        finally
        {
            if (engine.IsRunning)
                await engine.StopAsync();
        }
    }

    [Fact]
    public async Task MultiplePortalInstances_CanBeEnabled()
    {
        var first = new StubPortal("Same", [TestData.CreatePost("first")]);
        var second = new StubPortal("Same", [TestData.CreatePost("second")]);
        var engine = CreateEngine(new WatchEngineSettings
        {
            CheckIntervalMinutes = 1,
            EnableMultiplePortalInstances = true
        });
        engine.RegisterAdsPortal(first);
        engine.RegisterAdsPortal(second);
        engine.RegisterAdPostsHandler(new RecordingHandler());

        await engine.StartAsync();
        try
        {
            Assert.Equal(1, first.CallCount);
            Assert.Equal(1, second.CallCount);
        }
        finally
        {
            if (engine.IsRunning)
                await engine.StopAsync();
        }
    }

    [Fact]
    public async Task Start_WhileRunningAndStop_WhileStoppedAreRejected()
    {
        var engine = CreateEngine();
        engine.RegisterAdsPortal(new StubPortal("Portal", [TestData.CreatePost()]));
        engine.RegisterAdPostsHandler(new RecordingHandler());

        await Assert.ThrowsAsync<RealEstatesWatchEngineException>(() => engine.StopAsync());
        await engine.StartAsync();
        try
        {
            await Assert.ThrowsAsync<RealEstatesWatchEngineException>(() => engine.StartAsync());
        }
        finally
        {
            await engine.StopAsync();
        }
        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task Start_WithEmptyInitialSnapshot_StillStartsPeriodicChecks()
    {
        var engine = CreateEngine();
        engine.RegisterAdsPortal(new StubPortal("Portal", []));
        engine.RegisterAdPostsHandler(new RecordingHandler());

        await engine.StartAsync();

        Assert.True(engine.IsRunning);
        await engine.StopAsync();
    }

    private static RealEstatesWatchEngine CreateEngine(WatchEngineSettings? settings = null) => new(
        settings ?? new WatchEngineSettings
        {
            CheckIntervalMinutes = 1,
            PerformCheckOnStartup = true
        });

    private sealed class StubPortal(string name, IList<RealEstateAdPost> posts) : IRealEstateAdsPortal
    {
        public string Name { get; } = name;
        public string WatchedUrl => "https://example.test";
        public int CallCount { get; private set; }

        public Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync()
        {
            CallCount++;
            return Task.FromResult(posts);
        }
    }

    private sealed class RecordingHandler : IRealEstateAdPostsHandler
    {
        public bool IsEnabled { get; init; } = true;
        public List<IList<RealEstateAdPost>> InitialBatches { get; } = [];

        public Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
        {
            InitialBatches.Add(adPosts);
            return Task.CompletedTask;
        }
    }

    private sealed class DelegateFilter(Func<IEnumerable<RealEstateAdPost>, IEnumerable<RealEstateAdPost>> filter) : IRealEstateAdPostsFilter
    {
        public IEnumerable<RealEstateAdPost> Filter(IEnumerable<RealEstateAdPost> adPosts) => filter(adPosts);
    }
}
