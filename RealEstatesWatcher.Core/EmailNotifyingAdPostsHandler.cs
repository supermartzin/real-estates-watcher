using System.Collections.Generic;
using System.Threading.Tasks;

using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.Core
{
    public class EmailNotifyingAdPostsHandler : IRealEstateAdPostsHandler
    {
        public Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts)
        {
            throw new System.NotImplementedException();
        }
    }
}