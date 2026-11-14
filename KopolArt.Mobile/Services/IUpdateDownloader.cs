namespace KopolArt.Mobile.Services
{
    public interface IUpdateDownloader
    {
        Task<string> DownloadUpdateAsync(string url, Action<double> progressCallback, CancellationToken cancellationToken);
        Task<string> DownloadUpdateAsync(string url, Action<double, long, long> progressCallback, CancellationToken cancellationToken);
    }
}
