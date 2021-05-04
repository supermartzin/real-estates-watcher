using System.Collections.Generic;
using System.Threading.Tasks;

using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdsPortals.Contracts
{
    public interface IRealEstateAdsPortal
    {
        Task<IList<RealEstateAdPost>> GetLatestRealEstateAdsAsync();
    }
}
