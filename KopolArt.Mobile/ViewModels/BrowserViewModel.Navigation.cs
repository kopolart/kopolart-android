using KopolArt.Mobile.Helpers;

namespace KopolArt.Mobile.ViewModels
{
    public partial class BrowserViewModel
    {
        public bool OnNavigating(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (url.StartsWith(Constants.Urls.ShareScheme, StringComparison.OrdinalIgnoreCase))
            {
                _ = NavigationHelper.HandleShareActionAsync(url);
                return true;
            }
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return true;
            if (NavigationHelper.IsSystemScheme(uri))
            {
                _ = NavigationHelper.OpenExternalAsync(uri);
                return true;
            }
            if (NavigationHelper.IsExternalDeveloperUrl(uri))
            {
                _ = NavigationHelper.OpenExternalAsync(uri);
                return true;
            }
            if (NavigationHelper.IsInternalUrl(uri))
            {
                if (_settingsService.Settings.ShowLoadingOnNavigation)
                {
                    SetLoadingState();
                }
                return false;
            }
            if (NavigationHelper.IsAllowedSocialUrl(uri))
            {
                _ = NavigationHelper.OpenExternalAsync(uri);
                return true;
            }

            // Defense in depth: unknown hosts must never render inside the app WebView
            // (it exposes native JavaScript bridges). Hand them to the system browser instead.
            if (uri.Scheme is "http" or "https")
            {
                _ = NavigationHelper.OpenExternalAsync(uri);
            }
            return true;
        }

        public async void OnNavigated(bool success, string? currentUrl = null, string? currentTitle = null)
        {
            IsPullRefreshing = false;
            if (_disposed)
                return;
            if (success)
            {
                CancelRetry();
                _retryCount = 0;
                _hasSuccessfullyLoadedPage = true;
                // Refresh user welcome info upon navigation completion
                InitializeWelcomeInfo();
                // Record history and last visited url
                if (!string.IsNullOrWhiteSpace(currentUrl) && !currentUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                {
                    _ = _historyService.AddVisitAsync(currentTitle ?? currentUrl, currentUrl);
                    UpdateLastVisitedUrl(currentUrl);
                }
                // Record network consumption delta
                _networkStatsService.RecordTrafficDelta();
                if (!_isInitialLoadDone)
                {
                    if (ShowWelcomeOnLoading)
                    {
                        await Task.Delay(1200);
                        if (_disposed)
                            return;
                    }
                    _isInitialLoadDone = true;
                    _usageTrackerService.HasShownWelcomeOnLoading = true;
                    ShowWelcomeOnLoading = false;
                }
                SetSuccessState();
                if (!_welcomeToastShown)
                {
                    _welcomeToastShown = true;
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!_disposed)
                            RequestShowWelcomeToast?.Invoke(this, string.Empty);
                    });
                }
                if (!_promoChecked)
                {
                    _promoChecked = true;
                    if (_socialPromoService.ShouldShowPromo())
                    {
                        _socialPromoService.MarkPromoShown();
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (!_disposed)
                                RequestShowPromo?.Invoke(this, EventArgs.Empty);
                        });
                    }
                    else
                    {
                        _ = CheckForUpdatesAsync();
                    }
                }
                return;
            }

            // Non-destructive offline handling: if page was already successfully loaded, do not destroy page or show ErrorView immediately!
            if (_hasSuccessfullyLoadedPage)
            {
                _retryCount++;
                if (_retryCount < MaxRetries)
                {
                    _ = RetryAfterDelayAsync(_retryCount);
                }
                else
                {
                    CancelRetry();
                    // Retries exhausted: still preserve current page content so user's reading and website's offline banner remain visible
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsLoading = false;
                        IsBrowserVisible = true;
                        IsError = false;
                    });
                }
                return;
            }

            // Initial load failed (app opened with no internet): retry then show full ErrorView
            _retryCount++;
            if (_retryCount < MaxRetries)
            {
                _ = RetryAfterDelayAsync(_retryCount);
            }
            else
            {
                CancelRetry();
                ShowErrorOnMainThread();
            }
        }
        public void UpdateLastVisitedUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                return;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && NavigationHelper.IsInternalUrl(uri))
            {
                _settingsService.SessionLastVisitedUrl = url;
                _settingsService.Settings.LastVisitedUrl = url;
                _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
            }
        }
        public void OnPromoClosed()
        {
            _ = CheckForUpdatesAsync();
        }
    }
}
