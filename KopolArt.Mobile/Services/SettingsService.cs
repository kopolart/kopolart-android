using System.Text.Json;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class SettingsService : ISettingsService
    {
        private static readonly string SettingsFilePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
        private readonly object _lock = new();
        private readonly SemaphoreSlim _saveLock = new(1, 1);

        public AppSettings Settings { get; private set; } = new();
        public string? SessionLastVisitedUrl { get; set; }
        public bool HasJustUpdated { get; private set; }
        public string PreviousVersion { get; private set; } = string.Empty;
        public event EventHandler<AppSettings>? SettingsChanged;

        public SettingsService()
        {
            LoadSettingsSynchronous();
        }

        private void LoadSettingsSynchronous()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings);
                    if (loaded != null)
                    {
                        Settings = loaded;
                        bool prefOnboarding = Preferences.Get(Constants.Keys.HasCompletedOnboarding, false);
                        Settings.HasCompletedOnboarding = Settings.HasCompletedOnboarding || prefOnboarding;
                        Preferences.Set(Constants.Keys.OpenDashboardAtStartup, Settings.OpenDashboardAtStartup);
                        Preferences.Set(Constants.Keys.HasCompletedOnboarding, Settings.HasCompletedOnboarding);
                        Preferences.Set("WidgetOpacity", Settings.WidgetOpacity);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Sync load fallback: {ex.Message}");
            }

            if (!Preferences.ContainsKey(Constants.Keys.AppThemeMode) && Preferences.ContainsKey(Constants.Keys.AppThemeIsDark))
            {
                bool isDark = Preferences.Get(Constants.Keys.AppThemeIsDark, false);
                Settings.AppThemeMode = isDark ? "Dark" : "Light";
                Preferences.Remove(Constants.Keys.AppThemeIsDark);
            }
            Settings.AppThemeMode = Preferences.Get(Constants.Keys.AppThemeMode, Settings.AppThemeMode);
            Settings.OpenDashboardAtStartup = Preferences.Get(Constants.Keys.OpenDashboardAtStartup, Settings.OpenDashboardAtStartup);
            Settings.HasCompletedOnboarding = Preferences.Get(Constants.Keys.HasCompletedOnboarding, false) || Settings.HasCompletedOnboarding;
            Preferences.Set(Constants.Keys.OpenDashboardAtStartup, Settings.OpenDashboardAtStartup);
            Preferences.Set(Constants.Keys.HasCompletedOnboarding, Settings.HasCompletedOnboarding);
        }

        public Task InitializeAsync()
        {
            // Settings are loaded once by the constructor. Re-reading the file here
            // could overwrite values that were saved after construction.

            // Detect if the app was just updated
            var currentVersion = AppInfo.Current.VersionString;
            if (!string.IsNullOrEmpty(Settings.LastRunAppVersion) && Settings.LastRunAppVersion != currentVersion)
            {
                HasJustUpdated = true;
                PreviousVersion = Settings.LastRunAppVersion;
            }

            if (Settings.LastRunAppVersion != currentVersion)
            {
                Settings.LastRunAppVersion = currentVersion;
                _ = SaveSettingsAsync(Settings);
            }

            // Initialize in-memory session last visited URL
            SessionLastVisitedUrl = null;

            // Apply theme on init
            ApplyTheme(Settings.AppThemeMode);

            return Task.CompletedTask;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            string json;
            lock (_lock)
            {
                Settings = settings;
                json = JsonSerializer.Serialize(settings, AppJsonContext.Default.AppSettings);
            }

            Preferences.Set(Constants.Keys.OpenDashboardAtStartup, settings.OpenDashboardAtStartup);
            Preferences.Set(Constants.Keys.HasCompletedOnboarding, settings.HasCompletedOnboarding);
            Preferences.Set("WidgetOpacity", settings.WidgetOpacity);

            await _saveLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var tempPath = SettingsFilePath + ".tmp";
                await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
                File.Move(tempPath, SettingsFilePath, overwrite: true);
                SettingsChanged?.Invoke(this, settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to save settings: {ex.Message}");
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public async Task ResetToDefaultsAsync()
        {
            SessionLastVisitedUrl = null;
            var defaultSettings = new AppSettings
            {
                HasCompletedOnboarding = true,
                WidgetOpacity = 100
            };
            await SaveSettingsAsync(defaultSettings);
            ApplyTheme(defaultSettings.AppThemeMode);
        }

        public void ApplyTheme(string themeMode)
        {
            ApplyThemeInternal(themeMode, updateCookie: true);
        }

        public void ApplyThemeFromWeb(string themeMode, bool isDark)
        {
            ApplyThemeInternal(themeMode, updateCookie: false, forcedIsDark: isDark);
        }

        private void ApplyThemeInternal(string themeMode, bool updateCookie, bool? forcedIsDark = null)
        {
            if (string.Equals(themeMode, "auto", StringComparison.OrdinalIgnoreCase))
            {
                themeMode = "System";
            }

            bool isDark;
            AppTheme appTheme;

            switch (themeMode)
            {
                case "Dark":
                    isDark = true;
                    appTheme = AppTheme.Dark;
                    break;
                case "Light":
                    isDark = false;
                    appTheme = AppTheme.Light;
                    break;
                default: // "System"
                    appTheme = AppTheme.Unspecified;
                    if (forcedIsDark.HasValue)
                    {
                        isDark = forcedIsDark.Value;
                    }
                    else
                    {
#if ANDROID
                        isDark = Platforms.Android.MainActivity.IsSystemInDarkMode();
#else
                        isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
#endif
                    }
                    break;
            }

            // Keep Preferences and Settings in sync so cold starts know theme synchronously
            lock (_lock)
            {
                Settings.AppThemeMode = themeMode;
            }
            Preferences.Set(Constants.Keys.AppThemeMode, themeMode);
            _ = SaveSettingsAsync(Settings);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Application.Current != null)
                {
                    Application.Current.UserAppTheme = appTheme;
                }

#if ANDROID
                Platforms.Android.MainActivity.ApplyThemeColors(isDark);

                // Only set cookie when theme was changed from the APP settings,
                // NEVER when it originated from the website!
                if (updateCookie)
                {
                    try
                    {
                        // The website expects "auto", "dark", or "light" (not "system")
                        string cookieValue = themeMode.Equals("System", StringComparison.OrdinalIgnoreCase)
                            ? "auto"
                            : themeMode.ToLowerInvariant();

                        var cookieManager = Android.Webkit.CookieManager.Instance;
                        cookieManager?.SetCookie(Constants.Urls.Home, $"{Constants.Theme.DarkModeCookieName}={cookieValue}; path=/; max-age=31536000");
                        cookieManager?.Flush();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SettingsService] Cookie sync error: {ex.Message}");
                    }
                }
#endif
            });
        }

        public bool IsEffectiveDarkTheme()
        {
            var mode = Settings.AppThemeMode;
            if (string.Equals(mode, "Dark", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(mode, "Light", StringComparison.OrdinalIgnoreCase))
                return false;

#if ANDROID
            return Platforms.Android.MainActivity.IsSystemInDarkMode();
#else
            return Application.Current?.RequestedTheme == AppTheme.Dark;
#endif
        }

        public Task<long> GetCacheSizeBytesAsync()
        {
            return Task.Run(() =>
            {
                long size = 0;
#if ANDROID
                try
                {
                    var cacheDir = Android.App.Application.Context.CacheDir;
                    if (cacheDir != null && cacheDir.Exists())
                    {
                        size += GetDirectorySize(new DirectoryInfo(cacheDir.AbsolutePath));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsService] GetCacheSize error: {ex.Message}");
                }
#endif
                return size;
            });
        }

        public Task<long> GetAppDataSizeBytesAsync()
        {
            return Task.Run(() =>
            {
                long size = 0;
#if ANDROID
                try
                {
                    var filesDir = Android.App.Application.Context.FilesDir;
                    if (filesDir != null && filesDir.Exists())
                    {
                        size += GetDirectorySize(new DirectoryInfo(filesDir.AbsolutePath));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsService] GetAppDataSize error: {ex.Message}");
                }
#endif
                return size;
            });
        }

        public int GetCookieCount()
        {
#if ANDROID
            try
            {
                var cookieManager = Android.Webkit.CookieManager.Instance;
                var cookies = cookieManager?.GetCookie(Constants.Urls.Home);
                if (string.IsNullOrWhiteSpace(cookies))
                    return 0;

                var parts = cookies.Split(';', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] GetCookieCount error: {ex.Message}");
                return 0;
            }
#else
            return 0;
#endif
        }

        public Task ClearCacheAsync()
        {
            return Task.Run(() =>
            {
#if ANDROID
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Clear WebView cache
                        var webView = new Android.Webkit.WebView(Android.App.Application.Context);
                        webView.ClearCache(true);
                        webView.Destroy();
                    });

                    var cacheDir = Android.App.Application.Context.CacheDir;
                    if (cacheDir != null && cacheDir.Exists())
                    {
                        DeleteDirectoryContents(cacheDir, deleteSelf: false);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsService] ClearCache error: {ex.Message}");
                }
#endif
            });
        }

        public Task ClearCookiesAsync()
        {
            return Task.Run(() =>
            {
#if ANDROID
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var cookieManager = Android.Webkit.CookieManager.Instance;
                        cookieManager?.RemoveAllCookies(null);
                        cookieManager?.Flush();
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsService] ClearCookies error: {ex.Message}");
                }
#endif
            });
        }

        public Task ClearAppDataAsync()
        {
            return Task.Run(async () =>
            {
                SessionLastVisitedUrl = null;
                await ClearCacheAsync();
                await ClearCookiesAsync();
#if ANDROID
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Android.Webkit.WebStorage.Instance?.DeleteAllData();
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsService] ClearAppData error: {ex.Message}");
                }
#endif
            });
        }

        private static long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var file in dir.GetFiles())
                {
                    size += file.Length;
                }
                foreach (var subDir in dir.GetDirectories())
                {
                    size += GetDirectorySize(subDir);
                }
            }
            catch
            {
                // Ignored permission or missing folder
            }
            return size;
        }

#if ANDROID
        private static void DeleteDirectoryContents(Java.IO.File dir, bool deleteSelf = false)
        {
            if (dir.IsDirectory)
            {
                var children = dir.ListFiles();
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        DeleteDirectoryContents(child, deleteSelf: true);
                    }
                }
            }
            if (deleteSelf)
            {
                try
                {
                    dir.Delete();
                }
                catch
                {
                    // ignored
                }
            }
        }
#endif
    }
}
