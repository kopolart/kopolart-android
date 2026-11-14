using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IDownloadHistoryService
    {
        Task InitializeAsync();
        Task AddDownloadAsync(string fileName, string downloadUrl, string mimeType, long fileSizeBytes, string localFilePath, string status = "Completed");
        Task<List<DownloadItem>> GetDownloadsAsync(string? searchQuery = null, DownloadSortOrder sortOrder = DownloadSortOrder.Newest);
        Task DeleteItemAsync(string id);
        Task ClearAllAsync();
        bool DoesLocalFileExist(DownloadItem item);
        Task<bool> OpenDownloadedFileAsync(string localFilePath, string? mimeType);
        string ResolveDisplayPath(string? localFilePath, string? fileName = null);
        Task<(int TotalCount, long TotalBytes)> GetDownloadStatsAsync();
        Task ImportDownloadsAsync(List<DownloadItem> incoming, bool overwrite);
    }
}
