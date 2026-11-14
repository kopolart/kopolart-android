using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Platforms.Android;
using KopolArt.Mobile.Services;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    [QueryProperty(nameof(TargetUrl), "targetUrl")]
    public partial class BrowserPage : ContentPage, IQueryAttributable
    {
        private readonly BrowserViewModel _viewModel;
        private readonly BrowserWelcomeToastCoordinator _toastCoordinator = new();
        private readonly BrowserLoadingAnimationCoordinator _loadingCoordinator = new();
        private readonly BrowserGesturePillCoordinator _gestureCoordinator = new();
        private readonly BrowserFindInPageCoordinator _findCoordinator = new();
        private readonly IAppVisitTrackerService? _visitTrackerService;
        private string _targetUrl = string.Empty;

        public string TargetUrl
        {
            get => _targetUrl;
            set
            {
                _targetUrl = value;
                if (!string.IsNullOrWhiteSpace(_targetUrl) && WebBrowser != null)
                {
                    WebBrowser.Source = _targetUrl;
                }
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("targetUrl", out var target) && target is string url && !string.IsNullOrWhiteSpace(url))
            {
                TargetUrl = url;
            }
        }

        public BrowserPage(
            BrowserViewModel viewModel,
            IAppVisitTrackerService? visitTrackerService = null)
        {
            _visitTrackerService = visitTrackerService;
            try
            {
                InitializeComponent();
                _viewModel = viewModel;
                BindingContext = _viewModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserPage] InitializeComponent Error: {ex}");
                throw;
            }

            LandscapeSafeAreaHelper.Attach(this, applySafePadding: false);
            SizeChanged += OnPageSizeChanged;
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            UpdateOrientationLayout();
        }

        private void UpdateOrientationLayout()
        {
            _loadingCoordinator.UpdateLoadingViewOrientation(
                Width,
                Height,
                LoadingLogo,
                LoadingLabel,
                LoadingContentStack,
                WelcomeLoadingCard,
                WelcomeUserNameLabel,
                WelcomeTitleLabel,
                WelcomeSubtitleLabel,
                AppVersionLabel);
        }

        private void OnPageLoaded(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_targetUrl))
            {
                _targetUrl = _viewModel.SettingsService.SessionLastVisitedUrl ?? Constants.Urls.Home;
            }
            if (WebBrowser.Source == null && !string.IsNullOrWhiteSpace(_targetUrl))
            {
                WebBrowser.Source = _targetUrl;
            }

            UnsubscribeEvents();
            SubscribeEvents();
            UpdateOrientationLayout();

#if ANDROID
            SetupPullToRefreshGuard();
#endif

            if (_viewModel.IsLoading)
            {
                _loadingCoordinator.StartLoadingAnimation(LoadingLabel, Dispatcher);
            }

            if (_viewModel.ShowWelcomeOnLoading && WelcomeLoadingCard != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Task.Delay(500);
                        if (_viewModel.ShowWelcomeOnLoading && WelcomeLoadingCard != null)
                        {
                            await WelcomeLoadingCard.FadeToAsync(1.0, 450, Easing.CubicIn);
                        }
                    }
                    catch { }
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SubscribeEvents();
            UpdateOrientationLayout();
            _viewModel.UsageTrackerService.StartBrowsingTracking();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.UsageTrackerService.PauseBrowsingTracking();
            CaptureAndSaveCurrentUrl();

#if ANDROID
            try
            {
                Android.Webkit.CookieManager.Instance?.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserPage] Cookie flush error: {ex.Message}");
            }
#endif
        }

        private void OnPageUnloaded(object? sender, EventArgs e)
        {
            _viewModel.UsageTrackerService.PauseBrowsingTracking();
            CaptureAndSaveCurrentUrl();
            UnsubscribeEvents();
            _loadingCoordinator.StopLoadingAnimation(LoadingLabel);
            _loadingCoordinator.Dispose();
            _toastCoordinator.Dispose();
            _gestureCoordinator.Dispose();
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            MainActivity.DeepLinkReceived += OnDeepLinkReceived;
            ThemeBridge.ThemeModeChanged += OnThemeModeChangedFromBridge;
            NavigationBridge.DoubleTapped += OnDoubleTapReturned;
            NavigationBridge.UrlChanged += OnUrlChangedFromBridge;
            NavigationBridge.LongTapped += OnLongTapPageTriggered;
            NavigationBridge.SwipeBackTriggered += OnSwipeBackTriggered;
            NavigationBridge.SwipeForwardTriggered += OnSwipeForwardTriggered;
            _viewModel.RequestReload += OnRequestReload;
            _viewModel.RequestShowPromo += OnRequestShowPromo;
            _viewModel.RequestShowUpdate += OnRequestShowUpdate;
            _viewModel.RequestShowWelcomeToast += OnRequestShowWelcomeToast;
            _viewModel.RequestActivateFindInPage += OnRequestActivateFindInPage;
            _viewModel.RequestSavePageForOffline += OnRequestSavePageForOffline;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            SocialPromo.Closed += OnSocialPromoClosed;
        }

        private void UnsubscribeEvents()
        {
            MainActivity.DeepLinkReceived -= OnDeepLinkReceived;
            ThemeBridge.ThemeModeChanged -= OnThemeModeChangedFromBridge;
            NavigationBridge.DoubleTapped -= OnDoubleTapReturned;
            NavigationBridge.UrlChanged -= OnUrlChangedFromBridge;
            NavigationBridge.LongTapped -= OnLongTapPageTriggered;
            NavigationBridge.SwipeBackTriggered -= OnSwipeBackTriggered;
            NavigationBridge.SwipeForwardTriggered -= OnSwipeForwardTriggered;
            _viewModel.RequestReload -= OnRequestReload;
            _viewModel.RequestShowPromo -= OnRequestShowPromo;
            _viewModel.RequestShowUpdate -= OnRequestShowUpdate;
            _viewModel.RequestShowWelcomeToast -= OnRequestShowWelcomeToast;
            _viewModel.RequestActivateFindInPage -= OnRequestActivateFindInPage;
            _viewModel.RequestSavePageForOffline -= OnRequestSavePageForOffline;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            SocialPromo.Closed -= OnSocialPromoClosed;
        }

        private void OnUrlChangedFromBridge(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _viewModel.UpdateLastVisitedUrl(url);
                });
            }
        }

        private void CaptureAndSaveCurrentUrl()
        {
            try
            {
                string? currentUrl = null;
#if ANDROID
                if (MainThread.IsMainThread && WebBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
                {
                    currentUrl = nativeWebView.Url;
                }
#endif
                if (!string.IsNullOrWhiteSpace(currentUrl) &&
                    !currentUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase) &&
                    !currentUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    _viewModel.UpdateLastVisitedUrl(currentUrl);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserPage] Capture URL error: {ex.Message}");
            }
        }

        private void OnDoubleTapReturned(string? currentUrl)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(currentUrl))
                    {
                        _viewModel.UpdateLastVisitedUrl(currentUrl);
                    }
                    else
                    {
                        CaptureAndSaveCurrentUrl();
                    }
                    await Shell.Current.GoToAsync("..");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserPage] Double tap navigation error: {ex.Message}");
                }
            });
        }

        private void OnLongTapPageTriggered()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    string? currentUrl = null;
#if ANDROID
                    if (WebBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
                    {
                        currentUrl = nativeWebView.Url;
                    }
#endif
                    if (string.IsNullOrWhiteSpace(currentUrl) && WebBrowser?.Source is UrlWebViewSource urlSource)
                    {
                        currentUrl = urlSource.Url;
                    }
                    if (string.IsNullOrWhiteSpace(currentUrl) ||
                        currentUrl.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                        currentUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    string pageTitle = "صفحه کپل‌آرت";
                    try
                    {
                        if (WebBrowser != null)
                        {
                            var title = await WebBrowser.EvaluateJavaScriptAsync("document.title");
                            if (!string.IsNullOrWhiteSpace(title) && title != "null")
                            {
                                pageTitle = title.Trim('\"');
                            }
                        }
                    }
                    catch { }
                    await _viewModel.ShowPageContextMenuAsync(currentUrl, pageTitle);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserPage] Long tap context menu error: {ex.Message}");
                }
            });
        }

        private bool CanNavigateBack()
        {
#if ANDROID
            if (WebBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                return nativeWebView.CanGoBack();
            }
#endif
            return WebBrowser?.CanGoBack ?? false;
        }

        private bool CanNavigateForward()
        {
#if ANDROID
            if (WebBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                return nativeWebView.CanGoForward();
            }
#endif
            return WebBrowser?.CanGoForward ?? false;
        }

        private void ExecuteGoBack()
        {
#if ANDROID
            if (WebBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView && nativeWebView.CanGoBack())
            {
                nativeWebView.GoBack();
                return;
            }
#endif
            WebBrowser?.GoBack();
        }

        private void ExecuteGoForward()
        {
#if ANDROID
            if (WebBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView && nativeWebView.CanGoForward())
            {
                nativeWebView.GoForward();
                return;
            }
#endif
            WebBrowser?.GoForward();
        }

        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            if (CustomWebChromeClient.TryHideCustomView())
            {
                return true;
            }
#endif
            if (_findCoordinator.IsVisible(FindInPageBar))
            {
                _ = _findCoordinator.HideFindInPageAsync(FindInPageBar, FindQueryEntry, FindMatchesLabel, WebBrowser);
                return true;
            }
            if (CanNavigateBack())
            {
                _viewModel.TriggerLoadingState();
                ExecuteGoBack();
                return true;
            }
            CaptureAndSaveCurrentUrl();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("..");
            });
            return true;
        }

        private void OnSwipeBackTriggered()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
#if ANDROID
                    if (CustomWebChromeClient.TryHideCustomView())
                    {
                        return;
                    }
#endif
                    if (_findCoordinator.IsVisible(FindInPageBar))
                    {
                        _ = _findCoordinator.HideFindInPageAsync(FindInPageBar, FindQueryEntry, FindMatchesLabel, WebBrowser);
                        return;
                    }

                    if (CanNavigateBack())
                    {
                        _ = _gestureCoordinator.ShowGesturePillAsync(
                            isBack: true,
                            isReturningToDashboard: false,
                            BackGesturePill,
                            ForwardGesturePill,
                            BackGestureLabel,
                            BackGestureIcon);
                        _viewModel.TriggerLoadingState();
                        ExecuteGoBack();
                    }
                    else
                    {
                        _ = _gestureCoordinator.ShowGesturePillAsync(
                            isBack: true,
                            isReturningToDashboard: true,
                            BackGesturePill,
                            ForwardGesturePill,
                            BackGestureLabel,
                            BackGestureIcon);
                        CaptureAndSaveCurrentUrl();
                        await Task.Delay(220);
                        await Shell.Current.GoToAsync("..");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserPage] Swipe back navigation error: {ex.Message}");
                }
            });
        }

        private void OnSwipeForwardTriggered()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (CanNavigateForward())
                    {
                        _ = _gestureCoordinator.ShowGesturePillAsync(
                            isBack: false,
                            isReturningToDashboard: false,
                            BackGesturePill,
                            ForwardGesturePill,
                            BackGestureLabel,
                            BackGestureIcon);
                        _viewModel.TriggerLoadingState();
                        ExecuteGoForward();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserPage] Swipe forward navigation error: {ex.Message}");
                }
            });
        }

        private void OnDashboardReturnClicked(object? sender, EventArgs e)
        {
            CaptureAndSaveCurrentUrl();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("..");
            });
        }

        private void OnNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url != null)
            {
#if ANDROID
                try
                {                // Keep the WebView security profile in sync with the page being loaded.
                // (file access is only enabled for the app's own offline pages.)
                Platforms.Android.WebViewSetup.ApplyPageSecuritySettings(
                    WebBrowser?.Handler?.PlatformView as Android.Webkit.WebView,
                    e.Url);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserPage] OnNavigating settings error: {ex.Message}");
                }
#endif
                if (e.Url.Contains("josephy.ir", StringComparison.OrdinalIgnoreCase) && _visitTrackerService != null)
                {
                    _ = _visitTrackerService.TrackVisitAsync(Constants.PartialVisitKeys.JosephyBrowse);
                }
                bool shouldCancel = _viewModel.OnNavigating(e.Url);
                if (shouldCancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private async void OnNavigated(object sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Cancel)
            {
                _viewModel.OnNavigated(true);
                return;
            }
            if (e.Result == WebNavigationResult.Success)
            {
                string? pageTitle = null;
                try
                {
                    pageTitle = await WebBrowser.EvaluateJavaScriptAsync("document.title");
                    if (pageTitle != null)
                    {
                        pageTitle = pageTitle.Trim('"');
                    }
                }
                catch { }
                string urlToReport = e.Url;
#if ANDROID
                try
                {
                    Android.Webkit.CookieManager.Instance?.Flush();
                }
                catch { }
                if (WebBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView && !string.IsNullOrWhiteSpace(nativeWebView.Url))
                {
                    urlToReport = nativeWebView.Url;
                }
#endif
                _viewModel.OnNavigated(true, urlToReport, pageTitle);
                if (_visitTrackerService != null && WebBrowser != null)
                {
                    _ = _visitTrackerService.TrackBrowseVisitAsync(WebBrowser, urlToReport);
                }
            }
            else
            {
                _viewModel.OnNavigated(false, e.Url);
            }
        }

        private void OnThemeModeChangedFromBridge(object? sender, (string Mode, bool IsDark) e)
        {
            try
            {
                string appThemeMode = e.Mode.ToLowerInvariant() switch
                {
                    "dark" => "Dark",
                    "light" => "Light",
                    _ => "System"
                };
                _viewModel.SettingsService.ApplyThemeFromWeb(appThemeMode, e.IsDark);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserPage] Theme sync error: {ex.Message}");
            }
        }

        private void OnRequestReload(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => WebBrowser.Reload());
        }

        private void OnRequestShowPromo(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => SocialPromo.Show());
        }

        private void OnRequestShowUpdate(object? sender, (Models.UpdateInfo Info, List<Models.UpdateRelease> Releases) e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    UpdatePopup.Show(_viewModel.UpdateService, e.Info, e.Releases);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserPage] Error showing UpdatePopup: {ex}");
                }
            });
        }

        private void OnDeepLinkReceived(object? sender, string url)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WebBrowser.Source = url;
            });
        }

        private void OnSocialPromoClosed(object? sender, EventArgs e)
        {
            _viewModel.OnPromoClosed();
        }

        private void OnRequestShowWelcomeToast(object? sender, string message)
        {
            _toastCoordinator.ShowToast(
                WelcomeToastBorder,
                WelcomeToastText,
                () => _viewModel.UsageTrackerService.GetFormattedUsageTime(),
                Dispatcher);
        }

        private void OnWelcomeToastTapped(object? sender, EventArgs e)
        {
            _toastCoordinator.DismissToast(WelcomeToastBorder);
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BrowserViewModel.IsLoading))
            {
                if (_viewModel.IsLoading)
                    _loadingCoordinator.StartLoadingAnimation(LoadingLabel, Dispatcher);
                else
                    _loadingCoordinator.StopLoadingAnimation(LoadingLabel);
            }
            else if (e.PropertyName == nameof(BrowserViewModel.ShowWelcomeOnLoading))
            {
                if (!_viewModel.ShowWelcomeOnLoading && WelcomeLoadingCard != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        WelcomeLoadingCard.Opacity = 0;
                    });
                }
            }
        }

        private void OnRequestActivateFindInPage(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _findCoordinator.ShowFindInPageAsync(
                    FindInPageBar,
                    FindQueryEntry,
                    WebBrowser,
                    (activeOrdinal, totalCount, isDone) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (FindMatchesLabel == null) return;
                            if (string.IsNullOrWhiteSpace(FindQueryEntry?.Text))
                            {
                                FindMatchesLabel.Text = string.Empty;
                                return;
                            }
                            if (totalCount == 0)
                            {
                                FindMatchesLabel.Text = "یافت نشد";
                            }
                            else
                            {
                                int current = activeOrdinal + 1;
                                FindMatchesLabel.Text = $"{current} از {totalCount}".ToPersianDigits();
                            }
                        });
                    });
            });
        }

        private void OnFindQueryTextChanged(object? sender, TextChangedEventArgs e)
        {
            string query = e.NewTextValue?.Trim() ?? string.Empty;
            _findCoordinator.OnQueryTextChanged(query, FindMatchesLabel, WebBrowser);
        }

        private void OnFindQueryCompleted(object? sender, EventArgs e)
        {
            _findCoordinator.FindNext(WebBrowser, forward: true);
        }

        private void OnFindNextClicked(object? sender, EventArgs e)
        {
            _findCoordinator.FindNext(WebBrowser, forward: true);
        }

        private void OnFindPreviousClicked(object? sender, EventArgs e)
        {
            _findCoordinator.FindNext(WebBrowser, forward: false);
        }

        private async void OnFindCloseTapped(object? sender, EventArgs e)
        {
            await _findCoordinator.HideFindInPageAsync(FindInPageBar, FindQueryEntry, FindMatchesLabel, WebBrowser);
        }

        private async void OnOpenOfflinePagesClicked(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(OfflinePagesPage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserPage] Navigate to OfflinePagesPage error: {ex.Message}");
            }
        }

        private void OnRequestSavePageForOffline(object? sender, (string Url, string Title) e)
        {
            BrowserOfflineSaveCoordinator.SavePageForOffline(
                WebBrowser,
                e.Url,
                e.Title,
                _viewModel.OfflinePageService,
                _viewModel.CustomAlertService);
        }

#if ANDROID
        private void SetupPullToRefreshGuard()
        {
            try
            {
                if (PageRefreshView?.Handler?.PlatformView is AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout swipeRefresh)
                {
                    swipeRefresh.SetOnChildScrollUpCallback(new WebViewChildScrollUpCallback(() =>
                    {
                        if (WebBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
                        {
                            return nativeWebView.ScrollY > 0 || nativeWebView.CanScrollVertically(-1);
                        }
                        return false;
                    }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserPage] SetupPullToRefreshGuard error: {ex.Message}");
            }
        }
#endif
    }
}