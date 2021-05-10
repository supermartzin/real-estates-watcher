using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;

namespace RealEstatesWatcher.Core
{
    public class RealEstatesWatcher
    {
        private readonly ILogger<RealEstatesWatcher>? _logger;

        private readonly ISet<IRealEstateAdsPortal> _adsPortals;
        private readonly ISet<IRealEstateAdPostsHandler> _handlers;

        public RealEstatesWatcher(ILogger<RealEstatesWatcher>? logger = default)
        {
            _logger = logger;

            _adsPortals = new HashSet<IRealEstateAdsPortal>();
            _handlers = new HashSet<IRealEstateAdPostsHandler>();
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
        }
    }
}
