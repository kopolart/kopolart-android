using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    /// <summary>
    /// Builds and shows the "new post(s)" notification. Kept behind an interface so the settings
    /// screen can fire a test notification through the exact same code path the background job uses.
    /// </summary>
    public interface INewContentNotifier
    {
        /// <summary>
        /// Shows one digest notification covering all given posts (newest first).
        /// Returns false when notifications are not permitted or the notification was not shown.
        /// </summary>
        Task<bool> ShowDigestAsync(IReadOnlyList<RssFeedItem> posts);

        /// <summary>
        /// Shows a sample digest so the layout can be verified without waiting for a real post.
        /// </summary>
        Task<bool> ShowTestAsync();
    }
}
