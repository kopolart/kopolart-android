using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.Core.View;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android
{
    public class CustomWebChromeClient : WebChromeClient
    {
        private readonly Activity _activity;
        private global::Android.Views.View? _customView;
        private ICustomViewCallback? _customViewCallback;
        private FrameLayout? _fullscreenContainer;
        private ScreenOrientation _originalOrientation;

        private static WeakReference<CustomWebChromeClient>? _activeInstanceRef;

        public static CustomWebChromeClient? ActiveInstance
        {
            get
            {
                if (_activeInstanceRef != null && _activeInstanceRef.TryGetTarget(out var target))
                {
                    return target;
                }
                return null;
            }
            private set
            {
                _activeInstanceRef = value != null ? new WeakReference<CustomWebChromeClient>(value) : null;
            }
        }

        public static void ClearActiveInstance(CustomWebChromeClient? instance = null)
        {
            if (instance == null || ReferenceEquals(ActiveInstance, instance))
            {
                _activeInstanceRef = null;
            }
        }

        public CustomWebChromeClient(Context context)
        {
            _activity = context as Activity ?? Platform.CurrentActivity ?? throw new InvalidOperationException("Could not find current Activity");
            ActiveInstance = this;
        }

        public static bool TryHideCustomView()
        {
            if (ActiveInstance?._customView != null)
            {
                ActiveInstance.OnHideCustomView();
                return true;
            }
            return false;
        }

        public override Bitmap? DefaultVideoPoster
        {
            get
            {
                // Returning an empty 1x1 transparent bitmap prevents WebView from showing
                // a broken media icon or placeholder before video playback starts.
                return Bitmap.CreateBitmap(1, 1, Bitmap.Config.Argb8888!);
            }
        }

        public override void OnShowCustomView(global::Android.Views.View? view, ICustomViewCallback? callback)
        {
            if (view == null)
            {
                callback?.OnCustomViewHidden();
                return;
            }

            if (_customView != null)
            {
                callback?.OnCustomViewHidden();
                return;
            }

            _customView = view;
            _customViewCallback = callback;
            _originalOrientation = _activity.RequestedOrientation;

            var window = _activity.Window;
            if (window == null)
            {
                callback?.OnCustomViewHidden();
                return;
            }

            var decorView = window.DecorView as FrameLayout;
            if (decorView == null)
            {
                callback?.OnCustomViewHidden();
                return;
            }

            _fullscreenContainer = new FrameLayout(_activity)
            {
                LayoutParameters = new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent)
            };
            _fullscreenContainer.SetBackgroundColor(global::Android.Graphics.Color.Black);

            var viewParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent,
                GravityFlags.Center);

            _fullscreenContainer.AddView(view, viewParams);
            decorView.AddView(_fullscreenContainer);

            var controller = WindowCompat.GetInsetsController(window, decorView);
            if (controller != null)
            {
                controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
                controller.Hide(WindowInsetsCompat.Type.SystemBars());
            }

            _activity.RequestedOrientation = ScreenOrientation.Sensor;
        }

        public override void OnHideCustomView()
        {
            if (_customView == null) return;

            var window = _activity.Window;
            if (window != null && window.DecorView is FrameLayout decorView)
            {
                if (_fullscreenContainer != null)
                {
                    decorView.RemoveView(_fullscreenContainer);
                    _fullscreenContainer.RemoveAllViews();
                    _fullscreenContainer = null;
                }

                var controller = WindowCompat.GetInsetsController(window, decorView);
                if (controller != null)
                {
                    controller.Show(WindowInsetsCompat.Type.SystemBars());
                    bool isDark = MainActivity.IsDarkModeFromCookie();
                    controller.AppearanceLightStatusBars = !isDark;
                    controller.AppearanceLightNavigationBars = !isDark;
                }
            }

            _activity.RequestedOrientation = _originalOrientation;

            _customViewCallback?.OnCustomViewHidden();
            _customViewCallback = null;
            _customView = null;
        }

        public override bool OnShowFileChooser(global::Android.Webkit.WebView? webView, IValueCallback? filePathCallback, FileChooserParams? fileChooserParams)
        {
            if (MainActivity.FilePathCallback != null)
            {
                MainActivity.FilePathCallback.OnReceiveValue(null);
            }

            MainActivity.FilePathCallback = filePathCallback;

            var intent = fileChooserParams?.CreateIntent();
            try
            {
                if (intent != null)
                {
                    _activity.StartActivityForResult(intent, MainActivity.FileSelectCode);
                }
            }
            catch (Exception)
            {
                MainActivity.FilePathCallback = null;
                var alertService = IPlatformApplication.Current?.Services.GetService<ICustomAlertService>();
                if (alertService != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await alertService.ShowAlertAsync("خطا", "خطا در باز کردن انتخاب‌گر فایل", "تأیید", AlertType.Error);
                    });
                }
                return false;
            }

            return true;
        }

        public override bool OnJsAlert(global::Android.Webkit.WebView? view, string? url, string? message, JsResult? result)
        {
            var alertService = IPlatformApplication.Current?.Services.GetService<ICustomAlertService>()
                               ?? Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<ICustomAlertService>();

            if (alertService != null && !string.IsNullOrWhiteSpace(message))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await alertService.ShowAlertAsync("پیام", message, "تأیید", AlertType.Info);
                    }
                    finally
                    {
                        result?.Confirm();
                    }
                });
                return true;
            }

            result?.Confirm();
            return true;
        }

        public override bool OnJsConfirm(global::Android.Webkit.WebView? view, string? url, string? message, JsResult? result)
        {
            var alertService = IPlatformApplication.Current?.Services.GetService<ICustomAlertService>()
                               ?? Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<ICustomAlertService>();

            if (alertService != null && !string.IsNullOrWhiteSpace(message))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        bool confirm = await alertService.ShowConfirmAsync("تأیید", message, "تأیید", "انصراف", AlertType.Question);
                        if (confirm)
                        {
                            result?.Confirm();
                        }
                        else
                        {
                            result?.Cancel();
                        }
                    }
                    catch
                    {
                        result?.Cancel();
                    }
                });
                return true;
            }

            result?.Cancel();
            return true;
        }

        public override bool OnCreateWindow(global::Android.Webkit.WebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
        {
            if (view?.Context == null || resultMsg == null) return false;

            // Immediate Click Interception (Fast-path)
            var hitTestResult = view.GetHitTestResult();
            if (hitTestResult != null)
            {
                var type = hitTestResult.Type;
                if (type == HitTestResult.SrcAnchorType ||
                    type == HitTestResult.SrcImageAnchorType)
                {
                    var url = hitTestResult.Extra;
                    if (!string.IsNullOrEmpty(url))
                    {
                        if (HandleNavigation(view, url))
                        {
                            return false; // Cancel new window creation since we navigated in parent
                        }
                    }
                }
            }

            var newWebView = new global::Android.Webkit.WebView(view.Context);
            newWebView.Settings.JavaScriptEnabled = true;
            newWebView.Settings.DomStorageEnabled = true;
            newWebView.SetWebViewClient(new WindowOpenWebViewClient(view, newWebView));
            newWebView.SetDownloadListener(new CustomDownloadListener(view.Context, view));
            
            var transport = (global::Android.Webkit.WebView.WebViewTransport)resultMsg.Obj!;
            transport.WebView = newWebView;
            resultMsg.SendToTarget();
            
            return true;
        }

        private static bool HandleNavigation(global::Android.Webkit.WebView parentWebView, string url)
        {
            if (string.IsNullOrEmpty(url)) return false;

            if (url.StartsWith(Constants.Urls.ShareScheme, StringComparison.OrdinalIgnoreCase))
            {
                _ = Helpers.NavigationHelper.HandleShareActionAsync(url);
                return true;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (Helpers.NavigationHelper.IsSystemScheme(uri))
            {
                _ = Helpers.NavigationHelper.OpenExternalAsync(uri);
                return true;
            }

            if (Helpers.NavigationHelper.IsInternalUrl(uri))
            {
                parentWebView.LoadUrl(url);
                return true;
            }

            // External URLs (social media, payment gateways, etc.)
            _ = Helpers.NavigationHelper.OpenExternalAsync(uri);
            return true;
        }

        private class WindowOpenWebViewClient : WebViewClient
        {
            private const int CleanupTimeoutMs = 15000;

            private readonly global::Android.Webkit.WebView parentWebView;
            private readonly global::Android.Webkit.WebView tempWebView;
            private bool _isDestroyed;

            public WindowOpenWebViewClient(
                global::Android.Webkit.WebView parentWebView,
                global::Android.Webkit.WebView tempWebView)
            {
                this.parentWebView = parentWebView;
                this.tempWebView = tempWebView;

                // Safety net: if the opened window never navigates (e.g. an
                // about:blank popup that only runs scripts), destroy it after a
                // timeout so the temp WebView cannot leak.
                _ = CleanupAfterTimeoutAsync();
            }

            private async Task CleanupAfterTimeoutAsync()
            {
                try
                {
                    await Task.Delay(CleanupTimeoutMs);
                    CleanupTempWebView();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Temp WebView cleanup timer error: {ex.Message}");
                }
            }

            private void CleanupTempWebView()
            {
                // All state changes happen on the main thread, so the timer,
                // navigation callbacks and error callbacks cannot race.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_isDestroyed) return;
                    _isDestroyed = true;

                    try
                    {
                        tempWebView.StopLoading();
                        tempWebView.SetWebViewClient(null!);
                        tempWebView.SetDownloadListener(null);
                        tempWebView.Destroy();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error cleaning up temp WebView: {ex.Message}");
                    }
                });
            }

            public override void OnPageStarted(global::Android.Webkit.WebView? view, string? url, Bitmap? favicon)
            {
                base.OnPageStarted(view, url, favicon);

                if (string.IsNullOrEmpty(url) || url == "about:blank") return;

                // Handle navigation immediately on start to prevent loading delays
                if (HandleNavigation(parentWebView, url))
                {
                    CleanupTempWebView();
                }
            }

            public override void OnPageFinished(global::Android.Webkit.WebView? view, string? url)
            {
                base.OnPageFinished(view, url);
                if (string.IsNullOrEmpty(url) || url == "about:blank") return;
                CleanupTempWebView();
            }

            public override void OnReceivedError(global::Android.Webkit.WebView? view, IWebResourceRequest? request, WebResourceError? error)
            {
                base.OnReceivedError(view, request, error);
                CleanupTempWebView();
            }

            public override bool ShouldOverrideUrlLoading(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
            {
                var url = request?.Url?.ToString();
                if (string.IsNullOrEmpty(url)) return false;

                try
                {
                    return HandleNavigation(parentWebView, url);
                }
                finally
                {
                    CleanupTempWebView();
                }
            }
        }
    }
}
