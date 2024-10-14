using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.Contracts;

public interface IRealEstateAdsPortal
{
    string Name { get; }

    Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync();
}