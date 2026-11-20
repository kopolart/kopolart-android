namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Centralized URL policy and navigation helper for the WebView security model.
    /// Determines which URLs may be loaded in-app, which must go to the system browser,
    /// and which file:// paths are trusted (offline pages in the app's private directory).
    /// All security decisions are made here so callers remain thin.
    /// </summary>
    public static class NavigationHelper
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Offline-page path helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Absolute path to the directory where offline pages are stored.
        /// Only paths inside this directory are trusted for file:// loading.
        /// </summary>
        public static string OfflinePagesDirectory =>
            Path.Combine(FileSystem.AppDataDirectory, "OfflinePages");

        /// <summary>
        /// Returns true if the URL is one of the app's own offline pages stored
        /// in the private OfflinePages directory.
        /// </summary>
        public static bool IsTrustedOfflinePageUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)
                || !url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            try
            {
                var offlineRoot = Path.Combine(FileSystem.AppDataDirectory, "OfflinePages");
                if (!offlineRoot.EndsWith(Path.DirectorySeparatorChar))
                    offlineRoot += Path.DirectorySeparatorChar;

                return uri.LocalPath.StartsWith(offlineRoot, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Domain / scheme policy
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the URI's host matches kopolart.ir exactly or as a sub-domain.
        /// Uses exact / dot-suffix comparison to avoid suffix-confusion attacks
        /// (e.g. evil-kopolart.ir or notkopolart.ir would not match).
        /// </summary>
        public static bool IsKopolArtDomain(Uri? uri)
        {
            var host = uri?.Host;
            if (string.IsNullOrWhiteSpace(host))
                return false;

            host = host.TrimEnd('.');
            const string mainHost = "kopolart.ir";

            return host.Equals(mainHost, StringComparison.OrdinalIgnoreCase)
                || host.EndsWith("." + mainHost, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True only for https://kopolart.ir URLs (and sub-domains) or the app's own
        /// offline pages. The in-app WebView exposes native JavaScript bridges, so any
        /// other URL must be handed to the system browser.
        /// </summary>
        public static bool IsTrustedWebViewUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            // file:// must be checked before Uri parsing because it needs a path check.
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                return IsTrustedOfflinePageUrl(url);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            // about:blank carries no host and cannot load anything by itself.
            if (string.Equals(uri.Scheme, "about", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && IsKopolArtDomain(uri);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  URL classification helpers (Uri overloads used by callers)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns true if the URI is an internal kopolart.ir page.</summary>
        public static bool IsInternalUrl(Uri? uri) => IsKopolArtDomain(uri);

        /// <summary>Returns true if the URI is an internal kopolart.ir page (string overload).</summary>
        public static bool IsInternalUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && IsKopolArtDomain(uri);
        }

        /// <summary>Returns true for tel:, mailto:, javascript: URIs.</summary>
        public static bool IsSystemScheme(Uri? uri)
        {
            if (uri == null) return false;
            var scheme = uri.Scheme;
            return scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase)
                || scheme.Equals("tel", StringComparison.OrdinalIgnoreCase)
                || scheme.Equals("javascript", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns true for tel:, mailto:, javascript: URL strings.</summary>
        public static bool IsSystemScheme(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true for well-known social media domains that may open in an external
        /// browser from inside the site (e.g. share buttons).
        /// </summary>
        public static bool IsAllowedSocialUrl(Uri? uri)
        {
            var host = uri?.Host ?? string.Empty;
            return host.Contains("twitter.com", StringComparison.OrdinalIgnoreCase)
                || host.Contains("x.com", StringComparison.OrdinalIgnoreCase)
                || host.Contains("facebook.com", StringComparison.OrdinalIgnoreCase)
                || host.Contains("instagram.com", StringComparison.OrdinalIgnoreCase)
                || host.Contains("t.me", StringComparison.OrdinalIgnoreCase)
                || host.Contains("telegram.me", StringComparison.OrdinalIgnoreCase)
                || host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                || host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns true for well-known social media URL strings.</summary>
        public static bool IsAllowedSocialUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && IsAllowedSocialUrl(uri);
        }

        /// <summary>Returns true for developer / support URLs (e.g. GitHub).</summary>
        public static bool IsExternalDeveloperUrl(Uri? uri)
        {
            var host = uri?.Host ?? string.Empty;
            return host.Contains("github.com", StringComparison.OrdinalIgnoreCase)
                || host.Contains("josephy.ir", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Returns true for developer / support URL strings.</summary>
        public static bool IsExternalDeveloperUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) && IsExternalDeveloperUrl(uri);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  External launch helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Opens a URI in the system browser / default handler.</summary>
        public static async Task OpenExternalAsync(Uri? uri)
        {
            if (uri == null) return;
            try
            {
                await Launcher.Default.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationHelper] OpenExternal failed: {ex.Message}");
            }
        }

        /// <summary>Opens a URL string in the system browser / default handler.</summary>
        public static async Task OpenExternalAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                await Launcher.Default.OpenAsync(url);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationHelper] OpenExternal failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles a kopolart-share: action URL by triggering the platform share sheet.
        /// </summary>
        public static async Task HandleShareActionAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = url,
                    Title = "KopolArt"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationHelper] Share failed: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  In-app navigation helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Navigates to the BrowserPage and loads the given URL.
        /// Passes the URL as a shell query parameter so BrowserPage picks it up.
        /// </summary>
        public static async Task NavigateToBrowserAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                await Shell.Current.GoToAsync($"BrowserPage?url={Uri.EscapeDataString(url)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationHelper] NavigateToBrowserAsync failed: {ex.Message}");
                try
                {
                    await Shell.Current.GoToAsync("BrowserPage");
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[NavigationHelper] Shell navigation to BrowserPage failed: {navEx.Message}");
                }
            }
        }

        /// <summary>
        /// Navigates backward through the navigation stack, with a fallback to the dashboard.
        /// </summary>
        public static async Task GoBackSafelyAsync()
        {
            try
            {
                if (Shell.Current is null)
                    return;

                if (Shell.Current.Navigation?.NavigationStack?.Count > 1)
                    await Shell.Current.Navigation.PopAsync();
                else
                    await Shell.Current.GoToAsync("//DashboardPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationHelper] GoBackSafelyAsync fallback: {ex.Message}");
                try
                {
                    if (Shell.Current is not null)
                        await Shell.Current.GoToAsync("//DashboardPage");
                }
                catch (Exception navEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[NavigationHelper] Shell navigation to DashboardPage failed: {navEx.Message}");
                }
            }
        }
    }
}
