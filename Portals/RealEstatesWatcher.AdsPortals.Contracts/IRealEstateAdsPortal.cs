using System.Collections.Generic;
using System.Threading.Tasks;

using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.Contracts;

public interface IRealEstateAdsPortal
{
    string Name { get; }

    Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync();
}