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
    public void Registration_RejectsNullDependencies()
    {
        var engine = CreateEngine();

        Assert.Throws<ArgumentNullException>(() => engine.RegisterAdsPortal(null!));
        Assert.Throws<ArgumentNullException>(() => engine.RegisterAdPostsHandler(null!));
        Assert.Throws<ArgumentNullException>(() => engine.RegisterAdPostsFilter(null!));
    }

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
    public async Task Start_WhenAllPostsAreFilteredOut_StillStartsWithoutNotification()
    {
        var engine = CreateEngine();
        var handler = new RecordingHandler();
        engine.RegisterAdsPortal(new StubPortal("Portal", [TestData.CreatePost()]));
        engine.RegisterAdPostsFilter(new DelegateFilter(_ => []));
        engine.RegisterAdPostsHandler(handler);

        await engine.StartAsync();
        try
        {
            Assert.True(engine.IsRunning);
            Assert.Empty(handler.InitialBatches);
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task Start_WhenPortalFails_ContinuesSchedulingOtherChecks()
    {
        var engine = CreateEngine();
        var portal = new StubPortal("Portal", [])
        {
            Exception = new RealEstateAdsPortalException("unavailable")
        };
        engine.RegisterAdsPortal(portal);
        engine.RegisterAdPostsHandler(new RecordingHandler());

        await engine.StartAsync();
        try
        {
            Assert.True(engine.IsRunning);
            Assert.Equal(1, portal.CallCount);
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task Start_WhenInitialHandlerFails_WrapsTheDomainException()
    {
        var engine = CreateEngine();
        var expected = new RealEstateAdPostsHandlerException("write failed");
        engine.RegisterAdsPortal(new StubPortal("Portal", [TestData.CreatePost()]));
        engine.RegisterAdPostsHandler(new RecordingHandler { InitialException = expected });

        var exception = await Assert.ThrowsAsync<RealEstatesWatchEngineException>(() => engine.StartAsync());

        Assert.Same(expected, exception.InnerException);
        Assert.False(engine.IsRunning);
    }

    [Fact]
    public async Task Start_WithDeferredFirstCheck_DoesNotLoadPortalImmediately()
    {
        var portal = new StubPortal("Portal", [TestData.CreatePost()]);
        var engine = CreateEngine(new WatchEngineSettings
        {
            CheckIntervalMinutes = 1,
            PerformCheckOnStartup = false,
            StartCheckAtSpecificTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddSeconds(30))
        });
        engine.RegisterAdsPortal(portal);
        engine.RegisterAdPostsHandler(new RecordingHandler());

        await engine.StartAsync();
        try
        {
            Assert.True(engine.IsRunning);
            Assert.Equal(0, portal.CallCount);
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task DuplicateHandlerAndFilterInstances_AreOnlyInvokedOnce()
    {
        var engine = CreateEngine();
        var handler = new RecordingHandler();
        var filter = new DelegateFilter(posts => posts);
        engine.RegisterAdsPortal(new StubPortal("Portal", [TestData.CreatePost()]));
        engine.RegisterAdPostsHandler(handler);
        engine.RegisterAdPostsHandler(handler);
        engine.RegisterAdPostsFilter(filter);
        engine.RegisterAdPostsFilter(filter);

        await engine.StartAsync();
        try
        {
            Assert.Single(handler.InitialBatches);
            Assert.Equal(1, filter.CallCount);
        }
        finally
        {
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
        public Exception? Exception { get; init; }

        public Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync()
        {
            CallCount++;
            return Exception is not null
                ? Task.FromException<IList<RealEstateAdPost>>(Exception)
                : Task.FromResult(posts);
        }
    }

    private sealed class RecordingHandler : IRealEstateAdPostsHandler
    {
        public bool IsEnabled { get; init; } = true;
        public List<IList<RealEstateAdPost>> InitialBatches { get; } = [];
        public Exception? InitialException { get; init; }

        public Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default)
        {
            if (InitialException is not null)
                return Task.FromException(InitialException);

            InitialBatches.Add(adPosts);
            return Task.CompletedTask;
        }
    }

    private sealed class DelegateFilter(Func<IEnumerable<RealEstateAdPost>, IEnumerable<RealEstateAdPost>> filter) : IRealEstateAdPostsFilter
    {
        public int CallCount { get; private set; }

        public IEnumerable<RealEstateAdPost> Filter(IEnumerable<RealEstateAdPost> adPosts)
        {
            CallCount++;
            return filter(adPosts);
        }
    }
}

public class RealEstatesWatchEngineExceptionTests
{
    [Fact]
    public void Constructors_PreserveMessagesAndInnerExceptions()
    {
        var inner = new InvalidOperationException("inner");

        Assert.Null(new RealEstatesWatchEngineException().InnerException);
        Assert.Equal("message", new RealEstatesWatchEngineException("message").Message);
        Assert.Same(inner, new RealEstatesWatchEngineException("message", inner).InnerException);
    }
}
