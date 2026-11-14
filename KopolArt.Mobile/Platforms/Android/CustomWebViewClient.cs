using Android.Graphics;
using Android.Webkit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace KopolArt.Mobile.Platforms.Android
{
    /// <summary>
    /// Custom WebViewClient that ensures immediate real-time dark mode synchronization
    /// by flushing cookies early, checking theme state, and injecting client-side DOM and cookie watchers.
    /// </summary>
    public class CustomWebViewClient(IWebViewHandler handler) : MauiWebViewClient((WebViewHandler)handler)
    {
        private const string ThemeWatcherScript = @"
(function() {
    if (window.__kopolartThemeWatcherInstalled) {
        if (window.__kopolartCheckTheme) window.__kopolartCheckTheme();
        return;
    }
    window.__kopolartThemeWatcherInstalled = true;

    function getCookieMode() {
        try {
            var match = document.cookie.match(/(?:^|;\s*)darkmode-kopolart=([^;]*)/);
            if (match && match[1]) {
                var val = decodeURIComponent(match[1]).trim().toLowerCase();
                if (val === 'dark' || val === 'light' || val === 'auto') {
                    return val;
                }
            }
        } catch(e) {}
        return null;
    }

    function getThemeInfo() {
        var mode = null;
        var isDark = false;

        // 1. Check window.darkModeManager if available
        if (window.darkModeManager && typeof window.darkModeManager.getSavedMode === 'function') {
            try {
                var m = window.darkModeManager.getSavedMode();
                if (m === 'dark' || m === 'light' || m === 'auto') {
                    mode = m;
                }
            } catch(e) {}
        }

        // 2. Check DOM data-theme-mode attribute
        var docEl = document.documentElement;
        if (!mode && docEl) {
            var attr = docEl.getAttribute('data-theme-mode');
            if (attr) {
                var a = attr.trim().toLowerCase();
                if (a === 'dark' || a === 'light' || a === 'auto') {
                    mode = a;
                }
            }
        }

        // 3. Check darkmode-kopolart cookie
        if (!mode) {
            mode = getCookieMode();
        }

        if (!mode) {
            mode = 'auto';
        }

        // Determine isDark
        if (mode === 'dark') {
            isDark = true;
        } else if (mode === 'light') {
            isDark = false;
        } else {
            // mode is 'auto' (System): resolve based on DOM data-theme, classes, or prefers-color-scheme
            if (docEl && docEl.getAttribute('data-theme') === 'dark') {
                isDark = true;
            } else if (docEl && (docEl.classList.contains('dark') || docEl.classList.contains('woodmart-dark') || docEl.getAttribute('data-color-scheme') === 'dark')) {
                isDark = true;
            } else if (document.body && (document.body.classList.contains('dark') || document.body.classList.contains('woodmart-dark') || document.body.getAttribute('data-theme') === 'dark')) {
                isDark = true;
            } else {
                isDark = !!(window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
            }
        }

        return { mode: mode, isDark: isDark };
    }

    var lastMode = null;
    var lastIsDark = null;

    function syncTheme() {
        try {
            var info = getThemeInfo();
            if (info.mode !== lastMode || info.isDark !== lastIsDark) {
                lastMode = info.mode;
                lastIsDark = info.isDark;
                if (window.NativeThemeBridge) {
                    if (typeof window.NativeThemeBridge.onThemeModeChanged === 'function') {
                        window.NativeThemeBridge.onThemeModeChanged(info.mode, info.isDark);
                    } else if (typeof window.NativeThemeBridge.onThemeChanged === 'function') {
                        window.NativeThemeBridge.onThemeChanged(info.isDark);
                    }
                }
            }
        } catch (e) {}
    }

    window.__kopolartCheckTheme = syncTheme;
    syncTheme();

    function hookDarkModeManager() {
        try {
            if (window.darkModeManager && !window.darkModeManager.__kopolartHooked) {
                window.darkModeManager.__kopolartHooked = true;
                var origSync = window.darkModeManager.sync;
                if (typeof origSync === 'function') {
                    window.darkModeManager.sync = function() {
                        var res = origSync.apply(this, arguments);
                        syncTheme();
                        return res;
                    };
                }
                var origToggle = window.darkModeManager.toggle;
                if (typeof origToggle === 'function') {
                    window.darkModeManager.toggle = function() {
                        var res = origToggle.apply(this, arguments);
                        setTimeout(syncTheme, 10);
                        setTimeout(syncTheme, 100);
                        return res;
                    };
                }
            }
        } catch(e) {}
    }

    hookDarkModeManager();

    try {
        var observer = new MutationObserver(function() {
            hookDarkModeManager();
            syncTheme();
        });
        if (document.documentElement) {
            observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class', 'data-theme', 'data-theme-mode', 'data-color-scheme', 'style'] });
        }
        if (document.body) {
            observer.observe(document.body, { attributes: true, attributeFilter: ['class', 'data-theme', 'data-theme-mode', 'data-color-scheme', 'style'] });
        }
    } catch(e) {}

    var media = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)');
    if (media && media.addEventListener) {
        media.addEventListener('change', function() {
            syncTheme();
        });
    }

    document.addEventListener('click', function(e) {
        var target = e.target;
        if (target && typeof target.closest === 'function' && (target.closest('.dark-mode-btn') || target.closest('[class*=""dark-mode""]'))) {
            setTimeout(syncTheme, 0);
            setTimeout(syncTheme, 50);
            setTimeout(syncTheme, 200);
            setTimeout(syncTheme, 500);
        } else {
            setTimeout(syncTheme, 100);
            setTimeout(syncTheme, 400);
        }
    }, true);

    setInterval(function() {
        hookDarkModeManager();
        syncTheme();
    }, 1000);
})();
";

        private const string DisableSelectionScript = @"
(function() {
    function applySelectionStyles() {
        if (document.getElementById('__kopolart_disable_selection')) return;
        try {
            var style = document.createElement('style');
            style.id = '__kopolart_disable_selection';
            style.type = 'text/css';
            style.textContent = 'html, body { -webkit-text-size-adjust: 100% !important; text-size-adjust: 100% !important; } *, *::before, *::after { -webkit-user-select: none !important; -moz-user-select: none !important; -ms-user-select: none !important; user-select: none !important; -webkit-touch-callout: none !important; } input, textarea, [contenteditable=""true""], [contenteditable=""""] { -webkit-user-select: text !important; -moz-user-select: text !important; -ms-user-select: text !important; user-select: text !important; }';
            var target = document.head || document.documentElement || document.body;
            if (target) {
                target.appendChild(style);
            }
        } catch(e) {}
    }

    applySelectionStyles();
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applySelectionStyles, { once: true });
    }
})();
";

        private const string DoubleTapScript = @"
(function() {
    if (window.__kopolartGesturesInstalled) return;
    window.__kopolartGesturesInstalled = true;

    var lastTapTime = 0;
    var lastTapX = 0;
    var lastTapY = 0;
    var touchStartX = 0;
    var touchStartY = 0;
    var touchStartTime = 0;
    var touchMoved = false;
    var lastScrollTime = 0;
    var longPressTimer = null;
    var longPressFired = false;

    function cancelLongPress() {
        if (longPressTimer) {
            clearTimeout(longPressTimer);
            longPressTimer = null;
        }
    }

    function clearTextSelection() {
        try {
            if (window.getSelection) {
                var sel = window.getSelection();
                if (sel) {
                    if (typeof sel.empty === 'function') sel.empty();
                    else if (typeof sel.removeAllRanges === 'function') sel.removeAllRanges();
                }
            }
        } catch (e) {}
    }

    function isInteractive(target) {
        if (!target) return false;
        var tag = (target.tagName || '').toUpperCase();
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || tag === 'A' || tag === 'BUTTON') {
            return true;
        }
        if (target.isContentEditable) return true;
        if (typeof target.closest === 'function') {
            if (target.closest('a') ||
                target.closest('button') ||
                target.closest('[role=""button""]') ||
                target.closest('input') ||
                target.closest('select') ||
                target.closest('textarea') ||
                target.closest('.dark-mode-btn') ||
                target.closest('[class*=""dark-mode""]')) {
                return true;
            }
        }
        return false;
    }

    // Suppress text selection initiation on non-interactive elements
    document.addEventListener('selectstart', function(e) {
        if (!isInteractive(e.target)) {
            e.preventDefault();
            return false;
        }
    }, false);

    // Suppress context menu on non-interactive elements
    document.addEventListener('contextmenu', function(e) {
        if (!isInteractive(e.target)) {
            e.preventDefault();
            return false;
        }
    }, false);

    window.addEventListener('scroll', function() {
        lastScrollTime = Date.now();
        cancelLongPress();
    }, { passive: true });

    document.addEventListener('touchstart', function(e) {
        if (!e.touches || e.touches.length !== 1) {
            touchMoved = true;
            cancelLongPress();
            return;
        }
        var touch = e.touches[0];
        touchStartX = touch.clientX;
        touchStartY = touch.clientY;
        touchStartTime = Date.now();
        touchMoved = false;
        longPressFired = false;

        var target = e.target;
        var isImg = (target && target.tagName && target.tagName.toUpperCase() === 'IMG') || (target && typeof target.closest === 'function' && target.closest('img'));
        if (!isInteractive(target) && !isImg) {
            cancelLongPress();
            longPressTimer = setTimeout(function() {
                if (!touchMoved && (Date.now() - lastScrollTime) > 250) {
                    longPressFired = true;
                    clearTextSelection();
                    if (window.NavigationBridge && typeof window.NavigationBridge.onLongTap === 'function') {
                        window.NavigationBridge.onLongTap();
                    }
                }
            }, 650);
        } else {
            cancelLongPress();
        }
    }, { passive: true });

    document.addEventListener('touchmove', function(e) {
        if (!e.touches || e.touches.length === 0) return;
        var touch = e.touches[0];
        if (Math.hypot(touch.clientX - touchStartX, touch.clientY - touchStartY) > 12) {
            touchMoved = true;
            cancelLongPress();
        }
    }, { passive: true });

    document.addEventListener('touchend', function(e) {
        cancelLongPress();

        if (longPressFired) {
            lastTapTime = 0;
            clearTextSelection();
            return;
        }

        var now = Date.now();
        if (!e.changedTouches || e.changedTouches.length === 0) return;
        var touch = e.changedTouches[0];
        var target = e.target;

        // If finger dragged, or touch took > 250ms, or page was scrolling within 350ms -> CANCEL
        if (touchMoved || (now - touchStartTime) > 250 || (now - lastScrollTime) < 350) {
            lastTapTime = 0;
            return;
        }

        if (Math.hypot(touch.clientX - touchStartX, touch.clientY - touchStartY) > 10) {
            lastTapTime = 0;
            return;
        }

        var hasSelection = window.getSelection && window.getSelection().toString().trim().length > 0;

        if (isInteractive(target) || hasSelection) {
            lastTapTime = 0;
            return;
        }

        var timeDiff = now - lastTapTime;
        var distDiff = Math.hypot(touch.clientX - lastTapX, touch.clientY - lastTapY);

        if (timeDiff >= 70 && timeDiff <= 360 && distDiff <= 30) {
            if (window.NavigationBridge) {
                var currentHref = window.location.href;
                if (typeof window.NavigationBridge.onDoubleTapWithUrl === 'function') {
                    window.NavigationBridge.onDoubleTapWithUrl(currentHref);
                } else if (typeof window.NavigationBridge.onDoubleTap === 'function') {
                    window.NavigationBridge.onDoubleTap();
                }
            }
            lastTapTime = 0;
        } else {
            lastTapTime = now;
            lastTapX = touch.clientX;
            lastTapY = touch.clientY;
        }
    }, { passive: true });

    document.addEventListener('touchcancel', function() {
        cancelLongPress();
    }, { passive: true });
})();
";

        private const string UrlWatcherScript = @"
(function() {
    if (window.__kopolartUrlWatcherInstalled) return;
    window.__kopolartUrlWatcherInstalled = true;

    function reportCurrentUrl() {
        try {
            var href = window.location.href;
            if (href && window.NavigationBridge && typeof window.NavigationBridge.onUrlChanged === 'function') {
                window.NavigationBridge.onUrlChanged(href);
            }
        } catch(e) {}
    }

    reportCurrentUrl();

    var origPushState = history.pushState;
    if (origPushState) {
        history.pushState = function() {
            var ret = origPushState.apply(this, arguments);
            setTimeout(reportCurrentUrl, 50);
            return ret;
        };
    }

    var origReplaceState = history.replaceState;
    if (origReplaceState) {
        history.replaceState = function() {
            var ret = origReplaceState.apply(this, arguments);
            setTimeout(reportCurrentUrl, 50);
            return ret;
        };
    }

    window.addEventListener('popstate', function() {
        setTimeout(reportCurrentUrl, 50);
    }, { passive: true });

    window.addEventListener('hashchange', function() {
        setTimeout(reportCurrentUrl, 50);
    }, { passive: true });
})();
";

        private volatile string? _currentPageUrl;

        /// <summary>
        /// Blocks every navigation that must not render inside the app WebView. The in-app
        /// WebView exposes native JavaScript bridges, so unknown hosts are handed to the
        /// system browser instead of being loaded here (phishing / redirect safety).
        /// </summary>
        public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
        {
            string? url = request?.Url?.ToString();

            if (string.IsNullOrWhiteSpace(url))
            {
                return base.ShouldOverrideUrlLoading(view, request);
            }

            // App-internal share links never load a page.
            if (url.StartsWith(Constants.Urls.ShareScheme, StringComparison.OrdinalIgnoreCase))
            {
                _ = Helpers.NavigationHelper.HandleShareActionAsync(url);
                return true;
            }

            if (Helpers.NavigationHelper.IsTrustedWebViewUrl(url))
            {
                return base.ShouldOverrideUrlLoading(view, request);
            }

            // Legacy http:// links to our own site: stay in the app, upgraded to https.
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                Helpers.NavigationHelper.IsKopolArtDomain(uri) &&
                view != null)
            {
                view.LoadUrl(UpgradeToHttps(uri));
                return true;
            }

            BlockInAppNavigation(view, url);
            return true;
        }

        private static string UpgradeToHttps(Uri uri)
        {
            try
            {
                return new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri.ToString();
            }
            catch
            {
                return uri.ToString();
            }
        }

        /// <summary>
        /// Cancels an in-app navigation and opens the URL in the system browser instead.
        /// Non-web schemes are dropped unless the app explicitly supports them.
        /// </summary>
        private static void BlockInAppNavigation(global::Android.Webkit.WebView? view, string url)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] Blocked in-app navigation: {url}");
                view?.StopLoading();

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return;

                if (Helpers.NavigationHelper.IsSystemScheme(uri) || uri.Scheme is "http" or "https")
                {
                    _ = Helpers.NavigationHelper.OpenExternalAsync(uri);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] BlockInAppNavigation error: {ex.Message}");
            }
        }

        public override void OnPageStarted(global::Android.Webkit.WebView? view, string? url, Bitmap? favicon)
        {
            // Safety net for server redirects and script navigations that never reach
            // ShouldOverrideUrlLoading: nothing outside kopolart.ir may run in this WebView.
            if (!string.IsNullOrWhiteSpace(url) && !Helpers.NavigationHelper.IsTrustedWebViewUrl(url))
            {
                BlockInAppNavigation(view, url);
                return;
            }

            base.OnPageStarted(view, url, favicon);
            _currentPageUrl = url;

            WebViewSetup.ApplyPageSecuritySettings(view, url);

            SyncThemeEarly(view);
            if (!string.IsNullOrWhiteSpace(url))
            {
                NavigationBridge.ReportUrl(url);
            }
        }

        public override void OnPageCommitVisible(global::Android.Webkit.WebView? view, string? url)
        {
            base.OnPageCommitVisible(view, url);
            InjectDisableSelection(view);
            if (url != null && url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                InjectOfflineFontStyles(view);
            }
        }

        public override void OnPageFinished(global::Android.Webkit.WebView? view, string? url)
        {
            base.OnPageFinished(view, url);
            _currentPageUrl = url;

            SyncThemeEarly(view);
            if (!string.IsNullOrWhiteSpace(url))
            {
                NavigationBridge.ReportUrl(url);
            }
            InjectDisableSelection(view);
            InjectThemeWatcher(view);
            InjectDoubleTapWatcher(view);
            InjectUrlWatcher(view);
            if (url != null && url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                InjectOfflineFontStyles(view);
            }
        }

        public override WebResourceResponse? ShouldInterceptRequest(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
        {
            try
            {
                if (request?.Url == null)
                {
                    return base.ShouldInterceptRequest(view, request);
                }

                // Only intercept resources if viewing an offline file page or when the device has no internet access.
                // When user is actively browsing online, do NOT intercept so the live web server handles everything authentically.
                // NOTE: NEVER call view.Url here, because ShouldInterceptRequest runs on Chromium's background thread
                // and calling WebView methods from non-UI thread throws a fatal runtime exception.
                string? currentUrl = _currentPageUrl;
                bool isOfflineOrFilePage = (currentUrl != null && currentUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    || string.Equals(request.Url.Scheme, "file", StringComparison.OrdinalIgnoreCase)
                    || request.Url.ToString().StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                    || Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

                if (isOfflineOrFilePage)
                {
                    var uri = request.Url;
                    string path = uri.Path?.ToLowerInvariant() ?? string.Empty;

                    string? assetName = null;
                    string? mimeType = null;

                    if (path.EndsWith("/abar-var.woff2") || path == "abar-var.woff2")
                    {
                        assetName = Constants.Fonts.AbarVarWoff2;
                        mimeType = "font/woff2";
                    }
                    else if (path.EndsWith("/kopolart-fa.woff2") || path == "kopolart-fa.woff2")
                    {
                        assetName = Constants.Fonts.KopolArtFaWoff2;
                        mimeType = "font/woff2";
                    }
                    else if (path.EndsWith("/kopolart-fa.woff") || path == "kopolart-fa.woff")
                    {
                        assetName = Constants.Fonts.KopolArtFaWoff;
                        mimeType = "font/woff";
                    }
                    else if (path.EndsWith("/abar-static.woff") || path == "abar-static.woff")
                    {
                        assetName = Constants.Fonts.AbarStaticWoff;
                        mimeType = "font/woff";
                    }
                    else if (path.EndsWith("/abar-static-bold.woff") || path == "abar-static-bold.woff")
                    {
                        assetName = Constants.Fonts.AbarStaticBoldWoff;
                        mimeType = "font/woff";
                    }
                    else if (path.EndsWith("/kopolart-fa.css") || path == "kopolart-fa.css")
                    {
                        assetName = Constants.Fonts.KopolArtFaCss;
                        mimeType = "text/css";
                    }

                    if (assetName != null && mimeType != null)
                    {
                        try
                        {
                            var stream = global::Android.App.Application.Context.Assets?.Open(assetName);
                            if (stream != null)
                            {
                                var headers = new Dictionary<string, string>
                                {
                                    { "Access-Control-Allow-Origin", "*" },
                                    { "Cache-Control", "max-age=31536000" }
                                };
                                return new WebResourceResponse(mimeType, "UTF-8", 200, "OK", headers, stream);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] Asset font stream error for '{assetName}': {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] ShouldInterceptRequest error: {ex.Message}");
            }

            return base.ShouldInterceptRequest(view, request);
        }

        private static void InjectOfflineFontStyles(global::Android.Webkit.WebView? view)
        {
            if (view == null) return;
            try
            {
                string fontTag = Helpers.OfflineFontHelper.GetOfflineFontsStyleTag();
                string script = $@"(function() {{
    if (document.getElementById('__kopolartOfflineStyles') || document.getElementById('__kopolart-offline-fonts')) return;
    var container = document.createElement('div');
    container.innerHTML = {System.Text.Json.JsonSerializer.Serialize(fontTag)};
    var styleEl = container.firstElementChild;
    if (styleEl) {{
        styleEl.id = '__kopolartOfflineStyles';
        (document.head || document.documentElement).appendChild(styleEl);
    }}
}})();";
                view.EvaluateJavascript(script, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] InjectOfflineFontStyles error: {ex.Message}");
            }
        }

        private static void InjectDisableSelection(global::Android.Webkit.WebView? view)
        {
            if (view == null) return;

            try
            {
                view.EvaluateJavascript(DisableSelectionScript, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] InjectDisableSelection error: {ex.Message}");
            }
        }

        private static void SyncThemeEarly(global::Android.Webkit.WebView? view)
        {
            try
            {
                CookieManager.Instance?.Flush();
                bool isDark = MainActivity.IsDarkModeFromCookie();
                MainActivity.ApplyThemeColors(isDark);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] SyncThemeEarly error: {ex.Message}");
            }
        }

        private static void InjectThemeWatcher(global::Android.Webkit.WebView? view)
        {
            if (view == null) return;

            try
            {
                view.EvaluateJavascript(ThemeWatcherScript, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] InjectThemeWatcher error: {ex.Message}");
            }
        }

        private static void InjectDoubleTapWatcher(global::Android.Webkit.WebView? view)
        {
            if (view == null) return;

            try
            {
                view.EvaluateJavascript(DoubleTapScript, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] InjectDoubleTapWatcher error: {ex.Message}");
            }
        }

        private static void InjectUrlWatcher(global::Android.Webkit.WebView? view)
        {
            if (view == null) return;

            try
            {
                view.EvaluateJavascript(UrlWatcherScript, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CustomWebViewClient] InjectUrlWatcher error: {ex.Message}");
            }
        }
    }
}

