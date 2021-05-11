using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Core
{
    public class RealEstatesWatchEngine
    {
        private readonly ILogger<RealEstatesWatchEngine>? _logger;

        private readonly ISet<IRealEstateAdsPortal> _adsPortals;
        private readonly ISet<IRealEstateAdPostsHandler> _handlers;
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
            _posts = new HashSet<RealEstateAdPost>();
        }

        public void RegisterAdsPortal(IRealEstateAdsPortal adsPortal)
        {
            if (adsPortal == null)
                throw new ArgumentNullException(nameof(adsPortal));

            if (_adsPortals.Any(portal => portal.Name == adsPortal.Name))
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
            if (adPostsHandler == null)
                throw new ArgumentNullException(nameof(adPostsHandler));

            if (_handlers.Any(handler => handler.Equals(adPostsHandler)))
            {
                _logger?.LogWarning($"Trying to register already registered ads posts handler of type '{adPostsHandler.GetType().FullName}'.");
                return;
            }

            // add to registered handlers
            _handlers.Add(adPostsHandler);

            _logger?.LogInformation($"Ad posts handler of type '{adPostsHandler.GetType().FullName}' successfully registered.");
        }

        public async Task StartAsync()
        {
            if (IsRunning)
                throw new RealEstatesWatchEngineException("Watcher is already running.");
            if (_adsPortals.Count == 0)
                throw new RealEstatesWatchEngineException("No Ads portals registered for watching.");
            if (_handlers.Count == 0)
                throw new RealEstatesWatchEngineException("No handlers registered for processing Ad posts.");
            if (_settings.CheckIntervalMinutes < 1)
                throw new RealEstatesWatchEngineException("Invalid check interval: must be at least 1 minute.");
            if (_settings.CheckIntervalMinutes >= int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(_settings.CheckIntervalMinutes), "Check interval is not set.");

            try
            {
                // make initial load of posts
                var posts = await GetCurrentAdsPortalsSnapshot().ConfigureAwait(false);
                foreach (var post in posts)
                {
                    _posts.Add(post);
                }

                _logger?.LogDebug($"Successfully downloaded initial {posts.Count} post(s) from {_adsPortals.Count} portal(s).");

                // notify handlers
                foreach (var handler in _handlers)
                {
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

        private async void Timer_OnElapsed(object sender, ElapsedEventArgs e)
        {
            _logger?.LogDebug("Periodic check started...");

            try
            {
                // get posts snapshot from portals
                var posts = await GetCurrentAdsPortalsSnapshot().ConfigureAwait(false);

                foreach (var post in posts)
                {
                    if (_posts.Contains(post))
                        continue; //skip

                    // add to collection
                    _posts.Add(post);

                    // notify
                    await NotifyHandlers(post).ConfigureAwait(false);
                }

                _logger?.LogDebug("Periodic check finished.");
            }
            catch (RealEstateAdsPortalException reapEx)
            {
                _logger?.LogError(reapEx, $"Error downloading new ad posts during periodic check: {reapEx.Message}");
            }
            catch (RealEstateAdPostsHandlerException reaphEx)
            {
                _logger?.LogError(reaphEx, $"Error notifying Ad post handlers during periodic check: {reaphEx.Message}");
            }
        }

        private async Task NotifyHandlers(RealEstateAdPost adPost)
        {
            foreach (var handler in _handlers)
            {
                await handler.HandleNewRealEstateAdPostAsync(adPost).ConfigureAwait(false);
            }
        }

        private async Task<IList<RealEstateAdPost>> GetCurrentAdsPortalsSnapshot()
        {
            var posts = new List<RealEstateAdPost>();

            foreach (var adsPortal in _adsPortals)
            {
                posts.AddRange(await adsPortal.GetLatestRealEstateAdsAsync().ConfigureAwait(false));
            }

            return posts;
        }
    }
}
