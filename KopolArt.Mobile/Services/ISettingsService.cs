using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        string? SessionLastVisitedUrl { get; set; }
        bool HasJustUpdated { get; }
        string PreviousVersion { get; }
        event EventHandler<AppSettings>? SettingsChanged;

        Task InitializeAsync();
        Task SaveSettingsAsync(AppSettings settings);
        Task ResetToDefaultsAsync();

        void ApplyTheme(string themeMode);
        void ApplyThemeFromWeb(string themeMode, bool isDark);
        bool IsEffectiveDarkTheme();

        Task<long> GetCacheSizeBytesAsync();
        Task<long> GetAppDataSizeBytesAsync();
        int GetCookieCount();

        Task ClearCacheAsync();
        Task ClearCookiesAsync();
        Task ClearAppDataAsync();
    }
}
