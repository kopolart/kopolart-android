namespace KopolArt.Mobile.Services
{
    public interface IDownloadService
    {
        Task<bool> DownloadFileAsync(
            string url,
            string? fileName = null,
            string? mimeType = null,
            string? contentDisposition = null,
            string? userAgent = null,
            bool isRetry = false);

        Task<bool> DownloadDataUriAsync(
            string dataUri,
            string? fileName = null,
            string? mimeType = null);
    }
}
