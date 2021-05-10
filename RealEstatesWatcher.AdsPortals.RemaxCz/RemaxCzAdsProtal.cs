using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using RealEstatesWatcher.AdsPortals.Contracts;
using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.RemaxCz
{
    public class RemaxCzAdsProtal : IRealEstateAdsPortal
    {
        private readonly ILogger<RemaxCzAdsProtal>? _logger;

        private readonly string _adsUrl;
        private readonly string _rootHost;

        public string Name => "Remax.cz";

        public RemaxCzAdsProtal(string adsUrl,
                                ILogger<RemaxCzAdsProtal>? logger = default)
        {
            _adsUrl = adsUrl ?? throw new ArgumentNullException(nameof(adsUrl));
            _rootHost = ParseRootHost(adsUrl);
            _logger = logger;
        }
        
        public async Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync()
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"({Name}): Error getting latest ads: {ex.Message}");

                throw new RealEstateAdsPortalException($"({Name}): Error getting latest ads: {ex.Message}", ex);
            }
        }


        private static string ParseRootHost(string url) => $"https://{new Uri(url).Host}";
    }
}
