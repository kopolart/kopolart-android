using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IBookmarkService
    {
        Task InitializeAsync();
        Task<List<BookmarkItem>> GetBookmarksAsync(string? searchQuery = null, string? categoryFilter = null, BookmarkSortOrder sortOrder = BookmarkSortOrder.Newest);
        Task<bool> IsBookmarkedAsync(string url);
        Task<bool> ToggleBookmarkAsync(string url, string title);
        Task AddBookmarkAsync(string title, string url);
        Task<bool> RemoveBookmarkAsync(string url);
        Task DeleteItemAsync(string id);
        Task ClearAllAsync();
        Task<int> GetBookmarksCountAsync();
        Task ImportBookmarksAsync(List<BookmarkItem> incoming, bool overwrite);
    }
}
