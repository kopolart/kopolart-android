namespace KopolArt.Mobile.Services
{
    public interface ISearchHistoryService
    {
        List<string> GetRecentSearches();
        Task<List<string>> GetRecentSearchesAsync();
        void AddRecentSearch(string query);
        Task AddRecentSearchAsync(string query);
        void RemoveRecentSearch(string query);
        Task RemoveRecentSearchAsync(string query);
        void ClearAllRecentSearches();
        Task ClearAllRecentSearchesAsync();
        Task ImportRecentSearchesAsync(IEnumerable<string> searches, bool overwrite = false);
    }
}
