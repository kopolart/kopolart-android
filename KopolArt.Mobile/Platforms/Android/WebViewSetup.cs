using Microsoft.Maui.Handlers;

namespace KopolArt.Mobile.Platforms.Android;

public static class WebViewSetup
{
    public static void ConfigureCustomWebView()
    {
        WebViewHandler.Mapper.AppendToMapping("CustomWebViewSetup", (handler, view) =>
        {
            handler.PlatformView.Settings.JavaScriptEnabled = true;
            handler.PlatformView.Settings.DomStorageEnabled = true;
            handler.PlatformView.Settings.DatabaseEnabled = true;
            // Security baseline: navigation is locked to kopolart.ir (see CustomWebViewClient),
            // so local files, content:// access and mixed content are never needed for web pages.
            // Offline pages receive scoped file access per page in ApplyPageSecuritySettings().
            handler.PlatformView.Settings.AllowFileAccess = false;
            handler.PlatformView.Settings.AllowContentAccess = false;
            handler.PlatformView.Settings.AllowFileAccessFromFileURLs = false;
            handler.PlatformView.Settings.AllowUniversalAccessFromFileURLs = false;
            handler.PlatformView.Settings.MixedContentMode = global::Android.Webkit.MixedContentHandling.NeverAllow;

            // Viewport & Scaling configuration matching Chrome mobile browser 1:1
            handler.PlatformView.Settings.UseWideViewPort = true;
            handler.PlatformView.Settings.LoadWithOverviewMode = false;
            handler.PlatformView.Settings.TextZoom = 100;
            handler.PlatformView.Settings.DefaultFontSize = 16;
            handler.PlatformView.Settings.DefaultFixedFontSize = 13;
            handler.PlatformView.Settings.MinimumFontSize = 1;
            handler.PlatformView.Settings.MinimumLogicalFontSize = 1;
            handler.PlatformView.Settings.SetLayoutAlgorithm(global::Android.Webkit.WebSettings.LayoutAlgorithm.Normal!);
            handler.PlatformView.Settings.BuiltInZoomControls = false;
            handler.PlatformView.Settings.DisplayZoomControls = false;
            handler.PlatformView.Settings.CacheMode = global::Android.Webkit.CacheModes.Default;

            // Enable Cookies
            global::Android.Webkit.CookieManager.Instance?.SetAcceptCookie(true);
            global::Android.Webkit.CookieManager.Instance?.SetAcceptThirdPartyCookies(handler.PlatformView, true);
            
            // Get the default User Agent
            string defaultUserAgent = global::Android.Webkit.WebSettings.GetDefaultUserAgent(global::Android.App.Application.Context) ?? "";
            // Append our custom identifier
            handler.PlatformView.Settings.UserAgentString = $"{defaultUserAgent}{Constants.Keys.UserAgentSuffix}";

            handler.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
            handler.PlatformView.Settings.SetSupportZoom(false);
            handler.PlatformView.Settings.SetSupportMultipleWindows(true);
            handler.PlatformView.Settings.JavaScriptCanOpenWindowsAutomatically = true;

            handler.PlatformView.SetBackgroundColor(global::Android.Graphics.Color.Transparent);

            if (handler.PlatformView.Context == null) return;

            handler.PlatformView.SetWebViewClient(
                new CustomWebViewClient(handler));

            handler.PlatformView.SetWebChromeClient(
                new CustomWebChromeClient(handler.PlatformView.Context));

            var downloadListener = new CustomDownloadListener(handler.PlatformView.Context, handler.PlatformView);
            handler.PlatformView.SetDownloadListener(downloadListener);

            // Add Javascript Interface for Blob URL conversion downloads
            handler.PlatformView.AddJavascriptInterface(
                new BlobDownloadBridge(downloadListener),
                "BlobDownloaderBridge"
            );

            // Add Javascript Interface for real-time dark mode synchronization
            handler.PlatformView.AddJavascriptInterface(
                new ThemeBridge(),
                ThemeBridge.InterfaceName
            );

            // Add Javascript Interface for double-tap and long-tap return gesture
            handler.PlatformView.AddJavascriptInterface(
                new NavigationBridge(),
                NavigationBridge.InterfaceName
            );

            // Suppress native text selection and native context menu on long-press
            // while preserving editable inputs and showing image context menu on images
            handler.PlatformView.SetOnLongClickListener(new DisableSelectionLongClickListener(downloadListener));

            // Attach native swipe gesture listener for ultra-reliable, smooth horizontal swipe navigation
            handler.PlatformView.SetOnTouchListener(new AndroidWebViewSwipeTouchListener());

#if DEBUG
            global::Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
        });
}

        /// <summary>
        /// Applies the security profile that matches the page about to load. Only the app's own
        /// offline pages get file access; every other page runs without it. Mixed content is blocked
        /// everywhere, and cross-file / universal file access are disabled in both cases.
        /// </summary>
    /// <summary>
    /// Applies the minimal file-access profile that the current page requires.
    /// Only the app's own offline pages (file:// paths inside OfflinePages) get
    /// AllowFileAccess = true so that their local assets load. Every online page
    /// runs with all file access disabled.
    /// AllowFileAccessFromFileURLs and AllowUniversalAccessFromFileURLs are ALWAYS
    /// false — offline pages served via file:// must never be able to read arbitrary
    /// local files or make unconstrained cross-origin requests.
    /// MixedContentMode is ALWAYS NeverAllow.
    /// </summary>
    public static void ApplyPageSecuritySettings(
        global::Android.Webkit.WebView? view,
        string? url)
    {
        if (view?.Settings == null) return;

        bool isOfflinePage = Helpers.NavigationHelper.IsTrustedOfflinePageUrl(url);

        // Scoped file access: only for our own offline pages, never for online pages.
        view.Settings.AllowFileAccess = isOfflinePage;

        // Cross-file and universal access are disabled regardless of page type.
        view.Settings.AllowFileAccessFromFileURLs = false;
        view.Settings.AllowUniversalAccessFromFileURLs = false;

        // Mixed content is always blocked.
        view.Settings.MixedContentMode = global::Android.Webkit.MixedContentHandling.NeverAllow;
    }

    private sealed class DisableSelectionLongClickListener(CustomDownloadListener? downloadListener = null) : Java.Lang.Object, global::Android.Views.View.IOnLongClickListener
    {
        public bool OnLongClick(global::Android.Views.View? v)
        {
            if (v is global::Android.Webkit.WebView webView)
            {
                var hitTest = webView.GetHitTestResult();
                if (hitTest != null)
                {
                    if (hitTest.Type == global::Android.Webkit.HitTestResult.EditTextType)
                    {
                        return false; // Allow standard Android long-click for editable input elements
                    }

                    if (hitTest.Type == global::Android.Webkit.HitTestResult.ImageType ||
                        hitTest.Type == global::Android.Webkit.HitTestResult.SrcImageAnchorType ||
                        hitTest.Type == global::Android.Webkit.HitTestResult.ImageAnchorType)
                    {
                        var imageUrl = hitTest.Extra;
                        var ctx = webView.Context ?? global::Android.App.Application.Context;
                        if (!string.IsNullOrWhiteSpace(imageUrl) && ctx != null)
                        {
                            ImageContextMenuHelper.ShowImageContextMenu(ctx, imageUrl, downloadListener);
                            return true; // Consumed by image context menu
                        }
                    }
                }
            }

            // Return true to consume long-click on all other content, preventing Android text selection & context menu
            return true;
        }
    }
}
