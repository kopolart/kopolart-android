using Android.Content;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using KopolArt.Mobile.Platforms.Android.Services;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android;

public class CustomDownloadListener : Java.Lang.Object, IDownloadListener
{
    private readonly Context _context;
    private readonly global::Android.Webkit.WebView _webView;
    private readonly IDownloadService _downloadService;

    public CustomDownloadListener(
        Context context,
        global::Android.Webkit.WebView webView,
        IDownloadService? downloadService = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _downloadService = downloadService
            ?? IPlatformApplication.Current?.Services.GetService<IDownloadService>()
            ?? Application.Current?.Handler?.MauiContext?.Services.GetService<IDownloadService>()
            ?? throw new InvalidOperationException("IDownloadService not registered");
    }

    public void OnDownloadStart(
        string? url,
        string? userAgent,
        string? contentDisposition,
        string? mimeType,
        long contentLength)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            ShowToastOnMain("دانلود ناموفق: آدرس نامعتبر است.");
            return;
        }

        // Security: only accept downloads triggered while the WebView
        // is on a trusted KopolArt page.
        if (!IsTrustedPage())
        {
            System.Diagnostics.Debug.WriteLine($"Download blocked (untrusted page): {url}");
            return;
        }

        if (url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
        {
            HandleBlobUrl(url, contentDisposition, mimeType);
            return;
        }

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            string? fileName = !string.IsNullOrWhiteSpace(contentDisposition)
                ? DownloadFileNameResolver.ResolveFileName("data:file", contentDisposition, mimeType)
                : null;
            _ = _downloadService.DownloadDataUriAsync(url, fileName, mimeType);
            return;
        }

        _ = _downloadService.DownloadFileAsync(
            url,
            fileName: null,
            mimeType: mimeType,
            contentDisposition: contentDisposition,
            userAgent: userAgent,
            isRetry: false);
    }

    private bool IsTrustedPage()
    {
        try
        {
            var currentUrl = _webView.Url;
            if (string.IsNullOrWhiteSpace(currentUrl)) return true;

            var uri = new System.Uri(currentUrl);
            if (Helpers.NavigationHelper.IsKopolArtDomain(uri))
                return true;
#if DEBUG
            // Local development only; never trusted in release builds.
            return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
#else
            return false;
#endif
        }
        catch
        {
            return false;
        }
    }

    private void HandleBlobUrl(
        string blobUrl,
        string? contentDisposition,
        string? mimeType)
    {
        if (Looper.MainLooper is { } mainLooper)
        {
            var handler = new Handler(mainLooper);
            handler.Post(() =>
            {
                try
                {
                    var jsUrl = ToJsStringLiteral(blobUrl);
                    var jsDisposition = ToJsStringLiteral(contentDisposition);
                    var jsMime = ToJsStringLiteral(mimeType);

                    var jsScript = $@"
                        (function() {{
                            var xhr = new XMLHttpRequest();
                            xhr.open('GET', {jsUrl}, true);
                            xhr.responseType = 'blob';
                            xhr.onload = function(e) {{
                                if (this.status == 200) {{
                                    var blob = this.response;
                                    var reader = new FileReader();
                                    reader.onloadend = function() {{
                                        var base64data = reader.result;
                                        if (window.BlobDownloaderBridge && typeof window.BlobDownloaderBridge.processBlob === 'function') {{
                                            window.BlobDownloaderBridge.processBlob(
                                                base64data,
                                                {jsDisposition},
                                                {jsMime}
                                            );
                                        }}
                                    }};
                                    reader.readAsDataURL(blob);
                                }}
                            }};
                            xhr.send();
                        }})();";

                    _webView.EvaluateJavascript(jsScript, null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to execute blob download script: {ex.Message}");
                    ShowToastOnMain("خطا در شروع دانلود.");
                }
            });
        }
    }

    public void HandleBlobBase64(
        string base64DataUrl,
        string? contentDisposition,
        string? mimeType)
    {
        if (Looper.MainLooper is { } mainLooper)
        {
            var handler = new Handler(mainLooper);
            handler.Post(() =>
            {
                if (!IsTrustedPage())
                {
                    System.Diagnostics.Debug.WriteLine("Blob download blocked (untrusted page).");
                    return;
                }

                string? resolvedFileName = !string.IsNullOrWhiteSpace(contentDisposition)
                    ? DownloadFileNameResolver.ResolveFileName("blob-data", contentDisposition, mimeType)
                    : null;

                _ = _downloadService.DownloadDataUriAsync(base64DataUrl, resolvedFileName, mimeType);
            });
        }
    }

    private static string ToJsStringLiteral(string? value)
    {
        if (value == null) return "null";
        return "'" + value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "") + "'";
    }

    private void ShowToastOnMain(string message)
    {
        if (Looper.MainLooper is { } mainLooper)
        {
            var handler = new Handler(mainLooper);
            handler.Post(() =>
            {
                Toast.MakeText(_context, message, ToastLength.Long)?.Show();
            });
        }
    }
}
