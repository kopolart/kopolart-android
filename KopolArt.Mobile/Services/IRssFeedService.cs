using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IRssFeedService
    {
        Task<List<RssFeedItem>> FetchFeedAsync(CancellationToken cancellationToken = default);
        Task<RssFeedItem?> GetLatestNewPostAsync(CancellationToken cancellationToken = default);
        Task<List<RssFeedItem>> GetUnseenNewPostsAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<string, int>> GetRecentCategoryCountsAsync(TimeSpan? window = null, CancellationToken cancellationToken = default);
        void MarkPostAsNotified(RssFeedItem post);
        bool CanNotifyNow();
    }
}
