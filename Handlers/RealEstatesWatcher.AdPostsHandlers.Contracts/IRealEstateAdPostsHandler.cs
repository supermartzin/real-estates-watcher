using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsHandlers.Contracts
{
    public interface IRealEstateAdPostsHandler
    {
        bool IsEnabled { get; }

        Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost);

        Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts);

        Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts);
    }
}