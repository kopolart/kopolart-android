namespace KopolArt.Mobile.Services
{
    public interface IAppVisitTrackerService
    {
        Task TrackBrowseVisitAsync(WebView? webView, string? url);
        Task TrackVisitAsync(string visitId);
    }
}
