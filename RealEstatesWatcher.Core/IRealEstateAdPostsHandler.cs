using System.Collections.Generic;
using System.Threading.Tasks;

using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Core
{
    public interface IRealEstateAdPostsHandler
    {
        Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost);

        Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts);
    }
}