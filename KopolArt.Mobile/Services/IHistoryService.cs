using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IHistoryService
    {
        Task InitializeAsync();
        Task AddVisitAsync(string title, string url);
        Task<List<HistoryItem>> GetHistoryAsync(string? searchQuery = null, HistorySortOrder sortOrder = HistorySortOrder.Newest);
        Task DeleteItemAsync(string id);
        Task ClearHistoryAsync();
        Task<int> GetTotalVisitsCountAsync();
        Task ImportHistoryAsync(List<HistoryItem> incoming, bool overwrite);
    }
}
