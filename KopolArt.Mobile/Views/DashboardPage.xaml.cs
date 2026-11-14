using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Platforms.Android;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class DashboardPage : ContentPage
    {
        private static bool _hasLoadedOnce = false;
        private static bool _hasAnimatedWelcome = false;
        private readonly DashboardViewModel _viewModel;
        private DateTime _lastBackPress = DateTime.MinValue;
        private CancellationTokenSource? _periodicRefreshCts;

        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            LandscapeSafeAreaHelper.Attach(this);

            if (RecentSearchesScrollView != null)
            {
                RecentSearchesScrollView.Loaded += (s, e) => ScrollRecentSearchesToStart();
            }

            if (RecentSearchesStack != null)
            {
                RecentSearchesStack.SizeChanged += (s, e) => ScrollRecentSearchesToStart();
            }

            _viewModel.RecentSearches.CollectionChanged += (s, e) => ScrollRecentSearchesToStart();

            if (SearchEntry != null)
            {
                SearchEntry.Completed += (s, e) => KeyboardHelper.HideKeyboard(SearchEntry);
                SearchEntry.Focused += async (s, e) =>
                {
                    await Task.Delay(130);
                    ScrollSearchToTop();
                };
            }

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DashboardViewModel.ShowSuggestions))
                {
                    if (_viewModel.ShowSuggestions)
                    {
                        ScrollSearchToTop();
                    }
                    else if (string.IsNullOrWhiteSpace(_viewModel.SearchQuery))
                    {
                        ScrollBackToTop();
                    }
                }
            };

            if (_hasLoadedOnce)
            {
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = false;
                    LoadingOverlay.Opacity = 0;
                }

                if (WelcomeLabel != null)
                {
                    WelcomeLabel.Opacity = 1.0;
                    WelcomeLabel.TranslationY = 0;
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.AttachDashboardEvents();
            _viewModel.RefreshDashboardInfo();
            ScrollRecentSearchesToStart();
            TriggerOnlineUsersPulse();
            StartPeriodicOnlineUsersRefresh();
            _viewModel.StartCardRotation();

            if (!_hasLoadedOnce)
            {
                _ = HandleStartupSequenceAsync();
            }
            else
            {
                _ = CheckAndNavigatePendingIntentAsync();
                _ = AnimateWelcomeLabelAsync();
            }

#if ANDROID
            MainActivity.ShortcutActionReceived -= OnShortcutActionReceived;
            MainActivity.ShortcutActionReceived += OnShortcutActionReceived;
            MainActivity.DeepLinkReceived -= OnDeepLinkReceived;
            MainActivity.DeepLinkReceived += OnDeepLinkReceived;
#endif
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.DetachDashboardEvents();
#if ANDROID
            MainActivity.ShortcutActionReceived -= OnShortcutActionReceived;
            MainActivity.DeepLinkReceived -= OnDeepLinkReceived;
#endif
            if (OnlineUsersContainer != null)
            {
                OnlineUsersContainer.Opacity = 1.0;
            }
            StopPeriodicOnlineUsersRefresh();
            _viewModel.StopCardRotation();
        }

        private void TriggerOnlineUsersPulse()
        {
            if (OnlineUsersContainer == null) return;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (OnlineUsersContainer != null && _viewModel.IsOnlineUsersLive)
                    {
                        await OnlineUsersContainer.FadeToAsync(0.45, 250, Easing.SinInOut);
                        await OnlineUsersContainer.FadeToAsync(1.0, 250, Easing.SinInOut);
                    }
                    else if (OnlineUsersContainer != null)
                    {
                        OnlineUsersContainer.Opacity = 1.0;
                    }
                }
                catch { /* ignored */ }
            });
        }

        private void StartPeriodicOnlineUsersRefresh()
        {
            StopPeriodicOnlineUsersRefresh();
            _periodicRefreshCts = new CancellationTokenSource();
            var token = _periodicRefreshCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), token);
                        if (token.IsCancellationRequested) break;

                        if (await _viewModel.LoadOnlineUsersAsync(forceRefresh: true))
                        {
                            TriggerOnlineUsersPulse();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DashboardPage] Periodic online refresh warning: {ex.Message}");
                    }
                }
            }, token);
        }

        private void StopPeriodicOnlineUsersRefresh()
        {
            try
            {
                _periodicRefreshCts?.Cancel();
                _periodicRefreshCts?.Dispose();
                _periodicRefreshCts = null;
            }
            catch
            {
                // Ignored
            }
        }

        private async Task HandleStartupSequenceAsync()
        {
            // 1. Priority check: Check Deep Link and Launcher Shortcut first
            bool hasPendingIntent = await CheckAndNavigatePendingIntentAsync();
            if (hasPendingIntent)
            {
                _hasLoadedOnce = true;
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = false;
                    LoadingOverlay.Opacity = 0;
                }
                await _viewModel.CheckAndShowPostUpdateMessageAsync();
                return;
            }

            // 2. Normal startup: Open directly to Browser if user disabled OpenDashboardAtStartup
            if (!_viewModel.OpenDashboardAtStartup)
            {
                _hasLoadedOnce = true;
                if (LoadingOverlay != null)
                {
                    LoadingOverlay.IsVisible = false;
                    LoadingOverlay.Opacity = 0;
                }

                _viewModel.OpenBrowserCommand.Execute(null);
                await _viewModel.CheckAndShowPostUpdateMessageAsync();
                return;
            }

            // 3. Normal Dashboard startup: Keep splash/loading screen with fade-out animation
            await Task.Delay(1800);
            if (!_hasLoadedOnce)
            {
                _hasLoadedOnce = true;
                if (LoadingOverlay != null)
                {
                    _ = AnimateWelcomeLabelAsync();
                    await LoadingOverlay.FadeToAsync(0, 450, Easing.CubicOut);
                    LoadingOverlay.IsVisible = false;
                }
                else
                {
                    _ = AnimateWelcomeLabelAsync();
                }

                await _viewModel.CheckAndShowPostUpdateMessageAsync();
            }
        }

        private async Task<bool> CheckAndNavigatePendingIntentAsync()
        {
#if ANDROID
            // Priority 1: Check pending Deep Link (e.g. user clicked website link externally)
            var deepLink = MainActivity.PendingDeepLinkUrl;
            if (!string.IsNullOrEmpty(deepLink))
            {
                MainActivity.PendingDeepLinkUrl = null;
                await _viewModel.NavigateDeepLinkAsync(deepLink);
                return true;
            }

            // Priority 2: Check pending Launcher Shortcut / Widget action
            var action = MainActivity.PendingShortcutAction;
            if (!string.IsNullOrEmpty(action))
            {
                MainActivity.PendingShortcutAction = null;
                await _viewModel.NavigateShortcutActionAsync(action);
                return true;
            }
#endif
            return false;
        }

#if ANDROID
        private void OnDeepLinkReceived(object? sender, string url)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.NavigateDeepLinkAsync(url);
            });
        }

        private void OnShortcutActionReceived(object? sender, string action)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _viewModel.NavigateShortcutActionAsync(action);
            });
        }
#endif

        private async Task AnimateWelcomeLabelAsync()
        {
            if (WelcomeLabel == null) return;

            if (_hasAnimatedWelcome)
            {
                WelcomeLabel.Opacity = 1.0;
                WelcomeLabel.TranslationY = 0;
                return;
            }

            _hasAnimatedWelcome = true;

            try
            {
                WelcomeLabel.Opacity = 0;
                WelcomeLabel.TranslationY = 18;

                await Task.WhenAll(
                    WelcomeLabel.FadeToAsync(1.0, 1000, Easing.CubicOut),
                    WelcomeLabel.TranslateToAsync(0, 0, 1000, Easing.CubicOut)
                );
            }
            catch
            {
                WelcomeLabel.Opacity = 1.0;
                WelcomeLabel.TranslationY = 0;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Double-press back to exit on Dashboard
            if ((DateTime.Now - _lastBackPress).TotalSeconds < 2)
            {
                return false; // Allow system to exit app
            }

            _lastBackPress = DateTime.Now;

#if ANDROID
            Android.Widget.Toast.MakeText(
                Android.App.Application.Context,
                "برای خروج از برنامه، دوباره دکمه بازگشت را بزنید",
                Android.Widget.ToastLength.Short)?.Show();
#endif

            return true; // Cancel first back press
        }

        private void ScrollSearchToTop()
        {
            if (SearchSectionContainer == null || DashboardScrollView == null) return;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (SearchSectionContainer != null && DashboardScrollView != null)
                    {
                        await DashboardScrollView.ScrollToAsync(SearchSectionContainer, ScrollToPosition.Start, true);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DashboardPage] ScrollSearchToTop error: {ex.Message}");
                }
            });
        }

        private void ScrollBackToTop()
        {
            if (DashboardScrollView == null) return;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (DashboardScrollView != null)
                    {
                        await DashboardScrollView.ScrollToAsync(0, 0, true);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DashboardPage] ScrollBackToTop error: {ex.Message}");
                }
            });
        }

        private void ScrollRecentSearchesToStart()
        {
            if (RecentSearchesScrollView == null) return;
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
#if ANDROID
                    if (RecentSearchesScrollView.Handler?.PlatformView is global::Android.Views.ViewGroup vg)
                    {
                        vg.LayoutDirection = global::Android.Views.LayoutDirection.Rtl;

                        var hsv = vg as global::Android.Widget.HorizontalScrollView;
                        if (hsv == null)
                        {
                            for (int i = 0; i < vg.ChildCount; i++)
                            {
                                if (vg.GetChildAt(i) is global::Android.Widget.HorizontalScrollView childHsv)
                                {
                                    hsv = childHsv;
                                    break;
                                }
                            }
                        }

                        if (hsv != null)
                        {
                            hsv.LayoutDirection = global::Android.Views.LayoutDirection.Rtl;
                            hsv.Post(() =>
                            {
                                try
                                {
                                    var child = hsv.GetChildAt(0);
                                    if (child != null)
                                    {
                                        int maxScroll = Math.Max(0, child.Right - hsv.Width);
                                        hsv.ScrollTo(maxScroll, 0);
                                    }
                                    hsv.FullScroll(global::Android.Views.FocusSearchDirection.Right);
                                }
                                catch { }
                            });
                            return;
                        }
                    }
#endif
                    await RecentSearchesScrollView.ScrollToAsync(0, 0, false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DashboardPage] ScrollRecentSearchesToStart error: {ex.Message}");
                }
            });
        }
    }
}
