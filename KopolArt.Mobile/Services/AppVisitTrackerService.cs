using System.Collections.Concurrent;
using System.Diagnostics;
using KopolArt.Mobile.Helpers;

namespace KopolArt.Mobile.Services
{
    public class AppVisitTrackerService : IAppVisitTrackerService
    {
        private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(10);
        private static readonly HttpClient _trackingHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private readonly ConcurrentDictionary<string, DateTime> _lastTrackedMap = new();
        private DateTime _lastTrackedUtc = DateTime.MinValue;
        private readonly object _lock = new();

        public async Task TrackBrowseVisitAsync(WebView? webView, string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (url.Contains("josephy.ir", StringComparison.OrdinalIgnoreCase))
            {
                await TrackVisitAsync(Constants.PartialVisitKeys.JosephyBrowse);
                return;
            }

            if (webView == null)
                return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !NavigationHelper.IsInternalUrl(uri))
                return;

            var now = DateTime.UtcNow;
            lock (_lock)
            {
                if (now - _lastTrackedUtc < CooldownPeriod)
                    return;

                _lastTrackedUtc = now;
            }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    string script = "(function() {" +
                        "try {" +
                        "fetch('/add-visit-app/" + Constants.PartialVisitKeys.AndroidAppBrowse + "', { method: 'POST', cache: 'no-store' }).catch(function() {});" +
                        "} catch (e) {}" +
                        "})();";
                    await webView.EvaluateJavaScriptAsync(script);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AppVisitTrackerService] Visit tracking JS evaluation error: {ex.Message}");
                }
            });
        }

        public async Task TrackVisitAsync(string visitId)
        {
            if (string.IsNullOrWhiteSpace(visitId))
                return;

            // Debounce to protect rate limit (permitLimit: 6, windowSeconds: 60)
            var now = DateTime.UtcNow;
            if (_lastTrackedMap.TryGetValue(visitId, out var lastTime) && (now - lastTime).TotalSeconds < 15)
            {
                return;
            }
            _lastTrackedMap[visitId] = now;

            try
            {
                var requestUrl = $"https://kopolart.ir/add-visit-app/{Uri.EscapeDataString(visitId)}";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                _ = await _trackingHttpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppVisitTrackerService] Visit tracking error for '{visitId}': {ex.Message}");
            }
        }
    }
}
