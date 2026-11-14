using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IOfflinePageService
    {
        Task InitializeAsync();
        Task<List<OfflinePageItem>> GetOfflinePagesAsync(string? searchQuery = null, string? categoryFilter = null, OfflinePageSortOrder sortOrder = OfflinePageSortOrder.Newest);
        Task<OfflinePageItem?> GetOfflinePageByIdAsync(string id);
        Task<bool> AddOfflinePageAsync(OfflinePageItem item);
        Task<bool> DeleteOfflinePageAsync(string id);
        Task<int> GetOfflinePagesCountAsync();
        Task ClearAllOfflinePagesAsync();
        Task<bool> ImportOfflinePagesAsync(List<OfflinePageItem> pages);
    }
}
