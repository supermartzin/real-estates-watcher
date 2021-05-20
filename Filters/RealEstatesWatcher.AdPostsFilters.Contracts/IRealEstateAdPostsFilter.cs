using System.Collections.Generic;

using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsFilters.Contracts
{
    public interface IRealEstateAdPostsFilter
    {
        IEnumerable<RealEstateAdPost> Filter(IEnumerable<RealEstateAdPost> adPosts);
    }
}