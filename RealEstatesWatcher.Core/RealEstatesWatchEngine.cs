using Microsoft.Extensions.Logging;
using System.Timers;
using Timer = System.Timers.Timer;

using RealEstatesWatcher.AdPostsFilters.Contracts;
using RealEstatesWatcher.AdPostsHandlers.Contracts;
using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Core
{
    public class RealEstatesWatchEngine
    {
        private readonly ILogger<RealEstatesWatchEngine>? _logger;

        private readonly ISet<IRealEstateAdsPortal> _adsPortals;
        private readonly ISet<IRealEstateAdPostsHandler> _handlers;
        private readonly ISet<IRealEstateAdPostsFilter> _filters;
        private readonly ISet<RealEstateAdPost> _posts;
        private readonly WatchEngineSettings _settings;

        private Timer? _timer;

        public bool IsRunning { get; private set; }

        public RealEstatesWatchEngine(WatchEngineSettings settings,
                                      ILogger<RealEstatesWatchEngine>? logger = default)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger;

            _adsPortals = new HashSet<IRealEstateAdsPortal>();
            _handlers = new HashSet<IRealEstateAdPostsHandler>();
            _filters = new HashSet<IRealEstateAdPostsFilter>();
            _posts = new HashSet<RealEstateAdPost>();
        }

        public void RegisterAdsPortal(IRealEstateAdsPortal adsPortal)
        {
            ArgumentNullException.ThrowIfNull(adsPortal);

            if (!_settings.EnableMultiplePortalInstances && _adsPortals.Any(portal => portal.Name == adsPortal.Name))
            {
                _logger?.LogWarning($"Trying to register already registered ads portal named '{adsPortal.Name}'.");
                return;
            }

            // add to watched portals
            _adsPortals.Add(adsPortal);

            _logger?.LogInformation($"Ads portal '{adsPortal.Name}' successfully registered.");
        }

        public void RegisterAdPostsHandler(IRealEstateAdPostsHandler adPostsHandler)
        {
            ArgumentNullException.ThrowIfNull(adPostsHandler);

            if (_handlers.Any(handler => handler.Equals(adPostsHandler)))
            {
                _logger?.LogWarning($"Trying to register already registered ads posts handler of type '{adPostsHandler.GetType().FullName}'.");
                return;
            }

            // add to registered handlers
            _handlers.Add(adPostsHandler);

            _logger?.LogInformation($"Ad posts handler of type '{adPostsHandler.GetType().FullName}' successfully registered.");
        }

        public void RegisterAdPostsFilter(IRealEstateAdPostsFilter adPostsFilter)
        {
            ArgumentNullException.ThrowIfNull(adPostsFilter);

            if (_filters.Any(filter => filter.Equals(adPostsFilter)))
            {
                _logger?.LogWarning($"Trying to register already registered ads posts filter of type '{adPostsFilter.GetType().FullName}'.");
                return;
            }

            // add to registered filters
            _filters.Add(adPostsFilter);

            _logger?.LogInformation($"Ad posts filter of type '{adPostsFilter.GetType().FullName}' successfully registered.");
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
                throw new RealEstatesWatchEngineException("Invalid check interval: must be at least 1 minute.");
            if (_settings.CheckIntervalMinutes >= int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(_settings.CheckIntervalMinutes), "Check interval is not set.");

            try
            {
                // make initial load of posts
                var posts = await GetCurrentAdsPortalsSnapshot().ConfigureAwait(false);

                _logger?.LogDebug($"Successfully downloaded initial {posts.Count} post(s) from {_adsPortals.Count} portal(s).");

                // run posts through filters
                posts = _filters.Aggregate(posts, (current, filter) => filter.Filter(current).ToList());

                _logger?.LogDebug($"Filtered {posts.Count} post(s) from all downloaded posts.");

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

                _logger?.LogDebug("Handlers notified about initial posts.");
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

            _logger?.LogInformation($"Real estates Watcher has been started with periodic checking interval of {_settings.CheckIntervalMinutes} minute(s).");
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

            _logger?.LogInformation("Real estates Watcher has been stopped.");

            return Task.CompletedTask;
        }

        private async void Timer_OnElapsed(object? sender, ElapsedEventArgs e)
        {
            _logger?.LogDebug("Periodic check started.");

            // get posts snapshot from portals
            var allPosts = await GetCurrentAdsPortalsSnapshot().ConfigureAwait(false);

            // run posts through filters
            allPosts = _filters.Aggregate(allPosts, (current, filter) => filter.Filter(current).ToList());

            var newPosts = new List<RealEstateAdPost>();
            foreach (var post in allPosts)
            {
                if (_posts.Contains(post))
                    continue; // skip

                // add to collections
                _posts.Add(post);
                newPosts.Add(post);
            }

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

            _logger?.LogDebug("Periodic check finished.");
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
                    _logger?.LogError(reaphEx, $"Error notifying Ad posts Handler '{handler.GetType().FullName}': {reaphEx.Message}");
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
                    _logger?.LogError(reaphEx, $"Error notifying Ad posts Handler '{handler.GetType().FullName}': {reaphEx.Message}");
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
                    _logger?.LogError(reapEx, $"Error downloading new Ad posts from '{adsPortal.Name}': {reapEx.Message}");
                }
            }

            return posts;
        }
    }
}
