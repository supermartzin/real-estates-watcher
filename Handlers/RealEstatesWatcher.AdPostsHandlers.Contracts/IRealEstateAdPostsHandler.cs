using RealEstatesWatcher.Models;

namespace RealEstatesWatcher.AdPostsHandlers.Contracts;

public interface IRealEstateAdPostsHandler
{
    bool IsEnabled { get; }

    Task HandleNewRealEstateAdPostAsync(RealEstateAdPost adPost, CancellationToken cancellationToken = default);

    Task HandleNewRealEstatesAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default);

    Task HandleInitialRealEstateAdPostsAsync(IList<RealEstateAdPost> adPosts, CancellationToken cancellationToken = default);
}