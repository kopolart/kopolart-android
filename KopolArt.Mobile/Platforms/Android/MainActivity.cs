using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace KopolArt.Mobile.Platforms.Android
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        WindowSoftInputMode = SoftInput.AdjustResize,
        EnableOnBackInvokedCallback = true,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]
    [IntentFilter(
        [Intent.ActionView],
        Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
        DataScheme = "https",
        DataHost = "kopolart.ir")]
    [IntentFilter(
        [Intent.ActionView],
        Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
        DataScheme = "https",
        DataHost = "www.kopolart.ir")]
    [IntentFilter(
        [Intent.ActionView],
        Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
        DataScheme = "http",
        DataHost = "kopolart.ir")]
    [IntentFilter(
        [Intent.ActionView],
        Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
        DataScheme = "http",
        DataHost = "www.kopolart.ir")]
    public partial class MainActivity : MauiAppCompatActivity
    {
        // ─── File chooser (WebView upload) ────────────────────────────────────
        public static global::Android.Webkit.IValueCallback? FilePathCallback;
        public const int FileSelectCode = 100;


        // ─── Permission callbacks ─────────────────────────────────────────────
        // A list (not a single delegate) so that concurrent requests cannot
        // overwrite each other's callback.
        private static readonly object PermissionCallbackLock = new();
        private static readonly List<Action<bool>> NotificationPermissionCallbacks = [];
        private static readonly List<Action<bool>> StoragePermissionCallbacks = [];

        private const int NotificationPermissionCode = 1002;
        private const int StoragePermissionCode = 1003;

        // ─── Static instance (used by CustomDownloadListener) ─────────────────
        public static MainActivity? Instance { get; private set; }

        // ─── Deep link support ────────────────────────────────────────────────
        /// <summary>
        /// Holds the deep link URL when the app is cold-started via a link.
        /// MainPage reads and clears this on construction.
        /// </summary>
        public static string? PendingDeepLinkUrl { get; set; }

        /// <summary>
        /// Fired when a deep link URL arrives while the app is already running.
        /// </summary>
        public static event EventHandler<string>? DeepLinkReceived;

        // ─── Launcher shortcuts support ───────────────────────────────────────
        public static string? PendingShortcutAction { get; set; }
        public static event EventHandler<string>? ShortcutActionReceived;

        // ─── Lifecycle ────────────────────────────────────────────────────────
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            // Capture deep link URL BEFORE base.OnCreate
            ExtractDeepLinkUrl(Intent);
            ExtractShortcutAction(Intent);
            ExtractUpdateNotificationAction(Intent);
            // Set window background immediately based on preference to avoid white/light flash
            try
            {
                var themeMode = Preferences.Get(Constants.Keys.AppThemeMode, "System");
                bool isDarkInit = themeMode switch
                {
                    "Dark" => true,
                    "Light" => false,
                    _ => Preferences.Get(Constants.Keys.AppThemeIsDark, IsSystemInDarkMode())
                };
                var initHex = isDarkInit ? Constants.Theme.DarkColor : Constants.Theme.LightColor;
                var initColor = global::Android.Graphics.Color.ParseColor(initHex);
                Window?.SetBackgroundDrawable(new global::Android.Graphics.Drawables.ColorDrawable(initColor));
            }
            catch { /* ignored */ }

            base.OnCreate(savedInstanceState);

            if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window != null)
            {
                var lp = Window.Attributes;
                if (lp != null)
                {
                    lp.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                    Window.Attributes = lp;
                }
            }

            Instance = this;

            // Initialize typeface cache directly from assets for rotation font preservation
            TypefaceCache.Initialize();

            // Initialize dynamic launcher shortcuts
            ShortcutHelper.InitializeShortcuts(this);

            // Schedule periodic 48-hour background update check
            UpdateCheckScheduler.SchedulePeriodicCheck(this, UpdateCheckScheduler.DefaultIntervalHours);

            // Apply initial status/navigation bar colors based on cached preference or theme cookie
            bool isDark = Preferences.ContainsKey(Constants.Keys.AppThemeIsDark)
                ? Preferences.Get(Constants.Keys.AppThemeIsDark, false)
                : IsDarkModeFromCookie();
            ApplyThemeColors(isDark);
        }


        protected override void OnPause()
        {
            base.OnPause();
            try
            {
                global::Android.Webkit.CookieManager.Instance?.Flush();
            }
            catch { /* ignored */ }
        }

        protected override void OnDestroy()
        {
            // Guard: after a recreation the old instance's OnDestroy can run
            // AFTER the new instance's OnCreate - never null out the new one.
            if (Instance == this)
                Instance = null;

            CustomWebChromeClient.ClearActiveInstance();

            base.OnDestroy();
        }

        public override void OnConfigurationChanged(global::Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            try
            {
                bool isDark = IsDarkModeFromCookie();
                ApplyThemeColors(isDark);
                PostConfigurationFontRefresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] OnConfigurationChanged theme sync error: {ex.Message}");
            }
        }

        private void PostConfigurationFontRefresh()
        {
            RunOnUiThread(RefreshCurrentPageFonts);

            _ = Task.Delay(80).ContinueWith(_ =>
            {
                RunOnUiThread(RefreshCurrentPageFonts);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private static void RefreshCurrentPageFonts()
        {
            try
            {
                var app = Microsoft.Maui.Controls.Application.Current;
                var page = (app?.Windows.Count > 0 ? app.Windows[0].Page : null)
                    ?? (Shell.Current != null ? Shell.Current.CurrentPage : null);

                if (page != null)
                {
                    Helpers.LandscapeSafeAreaHelper.RefreshPageFonts(page);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] RefreshCurrentPageFonts error: {ex.Message}");
            }
        }

        // ─── Permission requests (called by CustomDownloadListener) ───────────

        /// <summary>
        /// Requests POST_NOTIFICATIONS on Android 13+.
        /// On older versions always returns true immediately.
        /// </summary>
        public static void RequestNotificationPermission(Action<bool> callback)
        {
            var activity = Instance;
            if (activity == null)
            {
                callback(false);
                return;
            }

            // POST_NOTIFICATIONS only required on Android 13+ (API 33)
            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                callback(true);
                return;
            }

            if (ContextCompat.CheckSelfPermission(
                    activity,
                    global::Android.Manifest.Permission.PostNotifications)
                == Permission.Granted)
            {
                callback(true);
                return;
            }

            // Store callback; only launch a new system request if none is
            // already pending (concurrent callers just wait for the result).
            bool requestAlreadyPending;
            lock (PermissionCallbackLock)
            {
                requestAlreadyPending = NotificationPermissionCallbacks.Count > 0;
                NotificationPermissionCallbacks.Add(callback);
            }

            if (!requestAlreadyPending)
            {
                ActivityCompat.RequestPermissions(
                    activity,
                    [global::Android.Manifest.Permission.PostNotifications],
                    NotificationPermissionCode);
            }
        }

        // ─── Permission result ────────────────────────────────────────────────
        public override void OnRequestPermissionsResult(
            int requestCode,
            string[] permissions,
            Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(
                requestCode, permissions, grantResults);

            bool granted = grantResults.Length > 0 &&
                           grantResults[0] == Permission.Granted;

            switch (requestCode)
            {
                case NotificationPermissionCode:
                    Action<bool>[] callbacks;
                    lock (PermissionCallbackLock)
                    {
                        callbacks = [.. NotificationPermissionCallbacks];
                        NotificationPermissionCallbacks.Clear();
                    }

                    foreach (var callback in callbacks)
                        callback(granted);
                    break;

                case StoragePermissionCode:
                    Action<bool>[] storageCallbacks;
                    lock (PermissionCallbackLock)
                    {
                        storageCallbacks = [.. StoragePermissionCallbacks];
                        StoragePermissionCallbacks.Clear();
                    }

                    foreach (var callback in storageCallbacks)
                        callback(granted);
                    break;
            }
        }

        /// <summary>
        /// Requests WRITE_EXTERNAL_STORAGE on Android 8/9 (API 26-28), where the app writes
        /// finished downloads into the public Downloads folder. Android 10+ uses MediaStore
        /// and returns true immediately.
        /// </summary>
        public static Task<bool> RequestLegacyStoragePermissionAsync()
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            MainThread.BeginInvokeOnMainThread(
                () => RequestLegacyStoragePermission(granted => tcs.TrySetResult(granted)));
            return tcs.Task;
        }

        private static void RequestLegacyStoragePermission(Action<bool> callback)
        {
            var activity = Instance;
            if (activity == null)
            {
                callback(false);
                return;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                callback(true);
                return;
            }

            if (ContextCompat.CheckSelfPermission(
                    activity,
                    global::Android.Manifest.Permission.WriteExternalStorage)
                == Permission.Granted)
            {
                callback(true);
                return;
            }

            bool requestAlreadyPending;
            lock (PermissionCallbackLock)
            {
                requestAlreadyPending = StoragePermissionCallbacks.Count > 0;
                StoragePermissionCallbacks.Add(callback);
            }

            if (!requestAlreadyPending)
            {
                ActivityCompat.RequestPermissions(
                    activity,
                    [global::Android.Manifest.Permission.WriteExternalStorage],
                    StoragePermissionCode);
            }
        }

        // ─── File chooser result (WebView upload) ─────────────────────────────
        protected override void OnActivityResult(
            int requestCode,
            Result resultCode,
            Intent? data)
        {
            if (requestCode == FileSelectCode)
            {
                if (FilePathCallback == null)
                    return;

                global::Android.Net.Uri[]? results = null;

                if (resultCode == Result.Ok)
                {
                    if (data?.Data != null)
                    {
                        results = [data.Data];
                    }
                    else if (data?.ClipData != null)
                    {
                        var clipData = data.ClipData;
                        results = new global::Android.Net.Uri[
                            clipData.ItemCount];
                        for (int i = 0; i < clipData.ItemCount; i++)
                            results[i] = clipData.GetItemAt(i)!.Uri!;
                    }
                }

                FilePathCallback.OnReceiveValue(results);
                FilePathCallback = null;
            }
            else if (requestCode == FolderSelectCode)
            {
                if (resultCode == Result.Ok && data?.Data != null)
                {
                    var treeUri = data.Data;
                    try
                    {
                        var flags = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                        if (flags == 0)
                        {
                            flags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
                        }
                        ContentResolver?.TakePersistableUriPermission(treeUri, flags);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainActivity] TakePersistableUriPermission failed: {ex.Message}");
                    }

                    string displayName = ResolveTreeUriToPath(treeUri);
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = ResolveFriendlyFolderName(treeUri);
                    }

                    _folderPickerTcs?.TrySetResult((treeUri.ToString(), displayName));
                }
                else
                {
                    _folderPickerTcs?.TrySetResult(null);
                }
                _folderPickerTcs = null;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        public static string ResolveTreeUriToPath(global::Android.Net.Uri? treeUri)
        {
            if (treeUri == null) return Helpers.StoragePathHelper.DefaultDownloadFolderDisplay;
            return Helpers.StoragePathHelper.ResolveUriToDisplayPath(treeUri.ToString());
        }

        private static string ResolveFriendlyFolderName(global::Android.Net.Uri treeUri)
        {
            try
            {
                string path = treeUri.Path ?? string.Empty;
                int colonIdx = path.LastIndexOf(':');
                if (colonIdx >= 0 && colonIdx < path.Length - 1)
                {
                    string subPath = path[(colonIdx + 1)..].Trim('/');
                    if (!string.IsNullOrWhiteSpace(subPath))
                    {
                        return subPath;
                    }
                }

                var segments = treeUri.PathSegments;
                if (segments != null && segments.Count > 0)
                {
                    var last = segments[^1];
                    int cIdx = last.LastIndexOf(':');
                    return cIdx >= 0 ? last[(cIdx + 1)..] : last;
                }
            }
            catch { /* ignored */ }
            return "پوشه انتخابی";
        }

    }
}
