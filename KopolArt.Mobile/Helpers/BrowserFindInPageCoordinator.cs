namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Coordinates Find In Page UI presentation, Android soft keyboard control, and search querying.
    /// </summary>
    public class BrowserFindInPageCoordinator
    {
        public bool IsVisible(Border? findInPageBar) => findInPageBar != null && findInPageBar.IsVisible;

        public async Task ShowFindInPageAsync(
            Border? findInPageBar,
            Entry? findQueryEntry,
            WebView? webBrowser,
            Action<int, int, bool> onResult)
        {
            if (findInPageBar == null) return;

#if ANDROID
            SetupFindListener(webBrowser, onResult);
#endif

            findInPageBar.IsVisible = true;
            findInPageBar.Opacity = 0;
            findInPageBar.TranslationY = -60;
            await Task.WhenAll(
                findInPageBar.FadeToAsync(1, 200, Easing.CubicOut),
                findInPageBar.TranslateToAsync(0, 0, 200, Easing.SpringOut)
            );
            findQueryEntry?.Focus();
        }

        public async Task HideFindInPageAsync(
            Border? findInPageBar,
            Entry? findQueryEntry,
            Label? findMatchesLabel,
            WebView? webBrowser)
        {
            if (findInPageBar == null || !findInPageBar.IsVisible) return;
            findQueryEntry?.Unfocus();
            _ = findQueryEntry?.HideSoftInputAsync(CancellationToken.None);

#if ANDROID
            HideAndroidKeyboard();

            if (webBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                nativeWebView.ClearMatches();
            }
#endif
            if (findMatchesLabel != null) findMatchesLabel.Text = string.Empty;
            if (findQueryEntry != null) findQueryEntry.Text = string.Empty;

            await Task.WhenAll(
                findInPageBar.FadeToAsync(0, 160, Easing.CubicIn),
                findInPageBar.TranslateToAsync(0, -60, 160, Easing.CubicIn)
            );
            findInPageBar.IsVisible = false;
        }

        public void OnQueryTextChanged(string query, Label? findMatchesLabel, WebView? webBrowser)
        {
#if ANDROID
            if (webBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                if (string.IsNullOrEmpty(query))
                {
                    nativeWebView.ClearMatches();
                    if (findMatchesLabel != null) findMatchesLabel.Text = string.Empty;
                }
                else
                {
                    nativeWebView.FindAllAsync(query);
                }
            }
#endif
        }

        public void FindNext(WebView? webBrowser, bool forward)
        {
#if ANDROID
            if (webBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                nativeWebView.FindNext(forward);
            }
#endif
        }

        public void SetupFindListener(WebView? webBrowser, Action<int, int, bool> onResult)
        {
#if ANDROID
            try
            {
                if (webBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
                {
                    nativeWebView.SetFindListener(new WebViewFindListener(onResult));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserFindInPageCoordinator] SetupFindListener error: {ex.Message}");
            }
#endif
        }

#if ANDROID
        private static void HideAndroidKeyboard()
        {
            try
            {
                var activity = Platform.CurrentActivity;
                if (activity != null)
                {
                    var imm = (Android.Views.InputMethods.InputMethodManager?)activity.GetSystemService(Android.Content.Context.InputMethodService);
                    var currentFocus = activity.CurrentFocus;
                    if (imm != null && currentFocus != null)
                    {
                        imm.HideSoftInputFromWindow(currentFocus.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
                    }
                    else if (imm != null && activity.Window?.DecorView != null)
                    {
                        imm.HideSoftInputFromWindow(activity.Window.DecorView.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserFindInPageCoordinator] Hide keyboard error: {ex.Message}");
            }
        }
#endif
    }
}
