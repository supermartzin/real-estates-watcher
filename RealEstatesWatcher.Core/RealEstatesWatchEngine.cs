using Microsoft.Extensions.Logging;
using System.Timers;
using Timer = System.Timers.Timer;

using RealEstatesWatcher.AdPostsFilters.Contracts;
using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Core;

public class RealEstatesWatchEngine(WatchEngineSettings settings,
                                    ILogger<RealEstatesWatchEngine>? logger = null)
{
    private readonly ISet<IRealEstateAdsPortal> _adsPortals = new HashSet<IRealEstateAdsPortal>();
    private readonly ISet<IRealEstateAdPostsHandler> _handlers = new HashSet<IRealEstateAdPostsHandler>();
    private readonly ISet<IRealEstateAdPostsFilter> _filters = new HashSet<IRealEstateAdPostsFilter>();
    private readonly ISet<RealEstateAdPost> _posts = new HashSet<RealEstateAdPost>();
    private readonly WatchEngineSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    private Timer? _timer;

    public bool IsRunning { get; private set; }

    public void RegisterAdsPortal(IRealEstateAdsPortal adsPortal)
    {
        ArgumentNullException.ThrowIfNull(adsPortal);

        if (!_settings.EnableMultiplePortalInstances && _adsPortals.Any(portal => portal.Name == adsPortal.Name))
        {
            logger?.LogWarning("Trying to register already registered ads portal named '{PortalName}'.", adsPortal.Name);
            return;
        }

        // add to watched portals
        _adsPortals.Add(adsPortal);

        logger?.LogInformation("Ads portal '{PortalName}' successfully registered.{NewLine}[URL = {PortalUrl}]", adsPortal.Name, Environment.NewLine, adsPortal.WatchedUrl);
    }

    public void RegisterAdPostsHandler(IRealEstateAdPostsHandler adPostsHandler)
    {
        ArgumentNullException.ThrowIfNull(adPostsHandler);

        if (_handlers.Any(handler => handler.Equals(adPostsHandler)))
        {
            logger?.LogWarning("Trying to register already registered ads posts handler of type '{HandlerName}'.", adPostsHandler.GetType().FullName);
            return;
        }

        // add to registered handlers
        _handlers.Add(adPostsHandler);

        logger?.LogInformation("Ad posts handler of type '{HandlerName}' successfully registered.", adPostsHandler.GetType().FullName);
    }

    public void RegisterAdPostsFilter(IRealEstateAdPostsFilter adPostsFilter)
    {
        ArgumentNullException.ThrowIfNull(adPostsFilter);

        if (_filters.Any(filter => filter.Equals(adPostsFilter)))
        {
            logger?.LogWarning("Trying to register already registered ads posts filter of type '{FilterName}'.", adPostsFilter.GetType().FullName);
            return;
        }

        // add to registered filters
        _filters.Add(adPostsFilter);

        logger?.LogInformation("Ad posts filter of type '{FilterName}' successfully registered.", adPostsFilter.GetType().FullName);
    }

    public async Task StartAsync()
    {
        if (IsRunning)
            throw new RealEstatesWatchEngineException("Watcher is already running.");
        if (_adsPortals.Count is 0)
            throw new RealEstatesWatchEngineException("No Ads portals registered for watching.");
        if (_handlers.Count is 0)
            throw new RealEstatesWatchEngineException("No handlers registered for processing Ad posts.");
        if (_settings.CheckIntervalMinutes < 1)
            throw new RealEstatesWatchEngineException("Invalid check interval value: must be at least 1 minute.");
        if (_settings.CheckIntervalMinutes >= int.MaxValue)
            throw new RealEstatesWatchEngineException("Invalid check interval value: too big number.");

        try
        {
            // make initial load of posts
            var posts = await GetCurrentAdsPortalsSnapshot().ConfigureAwait(false);

            logger?.LogDebug("Successfully downloaded initial {PostsCount} post(s) from {PortalsCount} portal(s).", posts.Count, _adsPortals.Count);

            // run posts through filters
            posts = _filters.Aggregate(posts, (current, filter) => filter.Filter(current).ToList());

            logger?.LogDebug("Filtered {Count} post(s) from all downloaded posts.", posts.Count);

            // add to collection of processed posts
            foreach (var post in posts)
            {
                _posts.Add(post);
            }

            // notify handlers
            foreach (var handler in _handlers)
            {
                if (!handler.IsEnabled)
                    continue;

                await handler.HandleInitialRealEstateAdPostsAsync(posts).ConfigureAwait(false);
            }

            logger?.LogDebug("Handlers notified about initial posts.");
        }
        catch (RealEstateAdsPortalException reapEx)
        {
            throw new RealEstatesWatchEngineException($"Error loading initial Ad posts: {reapEx.Message}", reapEx);
        }
        catch (RealEstateAdPostsHandlerException reaphEx)
        {
            throw new RealEstatesWatchEngineException($"Error notifying Ad post handlers: {reaphEx.Message}", reaphEx);
        }

        // start periodic checking timer
        _timer = new Timer
        {
            AutoReset = true,
            Interval = TimeSpan.FromMinutes(_settings.CheckIntervalMinutes).TotalMilliseconds
        };
        _timer.Elapsed += Timer_OnElapsed;
        _timer.Start();

        IsRunning = true;

        logger?.LogInformation("Real estates Watcher has been started with periodic checking interval of {CheckInterval} minute(s).", _settings.CheckIntervalMinutes);
    }

    public Task StopAsync()
    {
        if (!IsRunning)
            throw new RealEstatesWatchEngineException("Watcher is not running.");

        _timer!.Stop();
        _timer.Dispose();
        _timer.Elapsed -= Timer_OnElapsed;
        _timer = null;

        IsRunning = false;

        logger?.LogInformation("Real estates Watcher has been stopped.");
        logger?.LogInformation("------------------------------------------------");

        return Task.CompletedTask;
    }

    private async void Timer_OnElapsed(object? sender, ElapsedEventArgs e)
    {
        logger?.LogDebug("Periodic check started.");

        // get posts snapshot from portals
        var allPosts = await GetCurrentAdsPortalsSnapshot().ConfigureAwait(false);

        // run posts through filters
        allPosts = _filters.Aggregate(allPosts, (current, filter) => filter.Filter(current).ToList());

        // add to collection of processed posts and filter out new ones
        var newPosts = allPosts.Where(_posts.Add).ToList();

        // notify
        switch (newPosts.Count)
        {
            case 1:
                await NotifyHandlers(newPosts[0]).ConfigureAwait(false);
                break;
            case > 1:
                await NotifyHandlers(newPosts).ConfigureAwait(false);
                break;
        }

        logger?.LogDebug("Periodic check finished - found {Count} new ads.", newPosts.Count);
    }

    private async Task NotifyHandlers(RealEstateAdPost adPost)
    {
        foreach (var handler in _handlers)
        {
            if (!handler.IsEnabled)
                continue;

            try
            {
                await handler.HandleNewRealEstateAdPostAsync(adPost).ConfigureAwait(false);
            }
            catch (RealEstateAdPostsHandlerException reaphEx)
            {
                logger?.LogError(reaphEx, "Error notifying Ad posts Handler '{HandlerName}': {ExceptionMessage}", handler.GetType().FullName, reaphEx.Message);
            }
        }
    }

    private async Task NotifyHandlers(IList<RealEstateAdPost> adPosts)
    {
        foreach (var handler in _handlers)
        {
            if (!handler.IsEnabled)
                continue;

            try
            {
                await handler.HandleNewRealEstatesAdPostsAsync(adPosts).ConfigureAwait(false);
            }
            catch (RealEstateAdPostsHandlerException reaphEx)
            {
                logger?.LogError(reaphEx, "Error notifying Ad posts Handler '{HandlerName}': {ExceptionMessage}", handler.GetType().FullName, reaphEx.Message);
            }
        }
    }

    private async Task<IList<RealEstateAdPost>> GetCurrentAdsPortalsSnapshot()
    {
        var posts = new List<RealEstateAdPost>();

        foreach (var adsPortal in _adsPortals)
        {
            try
            {
                posts.AddRange(await adsPortal.GetLatestRealEstateAdsAsync().ConfigureAwait(false));
            }
            catch (RealEstateAdsPortalException reapEx)
            {
                logger?.LogError(reapEx, "Error downloading new Ad posts from '{PortalName}': {ExceptionMessage}", adsPortal.Name, reapEx.Message);
            }
        }

        return posts;
    }
}