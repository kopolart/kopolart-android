using Android.Webkit;
using Java.Interop;

namespace KopolArt.Mobile.Platforms.Android
{
    /// <summary>
    /// JavaScript bridge interface allowing the web page to notify the native app
    /// immediately in real time when the user toggles dark mode.
    /// </summary>
    public class ThemeBridge : Java.Lang.Object
    {
        public const string InterfaceName = "NativeThemeBridge";

        public static event EventHandler<bool>? ThemeChanged;
        public static event EventHandler<(string Mode, bool IsDark)>? ThemeModeChanged;

        [JavascriptInterface]
        [Export("onThemeChanged")]
        public void OnThemeChanged(bool isDark)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ThemeModeChanged?.Invoke(this, (isDark ? "dark" : "light", isDark));
                ThemeChanged?.Invoke(this, isDark);
            });
        }

        [JavascriptInterface]
        [Export("onThemeModeChanged")]
        public void OnThemeModeChanged(string? mode, bool isDark)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ThemeModeChanged?.Invoke(this, (mode ?? "auto", isDark));
                ThemeChanged?.Invoke(this, isDark);
            });
        }
    }
}
