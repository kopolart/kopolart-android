using Android.Views;
using AndroidX.Core.View;

namespace KopolArt.Mobile.Platforms.Android
{
    public partial class MainActivity
    {
        // ─── Theme helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether the Android system is currently in dark mode.
        /// </summary>
        public static bool IsSystemInDarkMode()
        {
            try
            {
                var uiMode = global::Android.App.Application.Context.Resources?.Configuration?.UiMode;
                if (uiMode.HasValue)
                {
                    return (uiMode.Value & global::Android.Content.Res.UiMode.NightMask) == global::Android.Content.Res.UiMode.NightYes;
                }
            }
            catch
            {
                // ignored
            }

            return Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        }

        /// <summary>
        /// Reads the darkmode-kopolart cookie to determine if dark mode is active.
        /// Supports "dark", "light", and "auto" (which checks system dark mode).
        /// </summary>
        public static bool IsDarkModeFromCookie()
        {
            bool isSystemDark = IsSystemInDarkMode();
            try
            {
                var cookies = global::Android.Webkit.CookieManager
                    .Instance?.GetCookie(Constants.Urls.Home);

                if (string.IsNullOrEmpty(cookies)) return isSystemDark;

                foreach (var part in cookies.Split(';'))
                {
                    var trimmed = part.Trim();
                    if (trimmed.StartsWith(
                            $"{Constants.Theme.DarkModeCookieName}=",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var value = trimmed.Substring(
                            Constants.Theme.DarkModeCookieName.Length + 1)
                            .Trim()
                            .ToLowerInvariant();

                        if (value == "dark") return true;
                        if (value == "light") return false;
                        if (value == "auto" || value == "system") return isSystemDark;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error reading dark mode cookie: {ex.Message}");
            }

            return isSystemDark;
        }

        /// <summary>
        /// Applies status bar and navigation bar colors based on the theme.
        /// Safe to call from any thread.
        /// </summary>
        public static void ApplyThemeColors(bool isDark)
        {
            var activity = Instance ?? Platform.CurrentActivity as MainActivity;
            if (activity?.Window == null) return;

            activity.RunOnUiThread(() =>
            {
                try
                {
                    var window = activity.Window;
                    if (window == null) return;

                    var colorHex = isDark
                        ? Constants.Theme.DarkColor
                        : Constants.Theme.LightColor;

                    var color = global::Android.Graphics.Color.ParseColor(colorHex);

                    // Set window background drawable and decor view background color
                    window.SetBackgroundDrawable(new global::Android.Graphics.Drawables.ColorDrawable(color));
                    window.DecorView.SetBackgroundColor(color);

                    // Ensure system bars draw their backgrounds
                    window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                    window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);

#pragma warning disable CA1422
                    window.SetStatusBarColor(color);
                    window.SetNavigationBarColor(color);
#pragma warning restore CA1422

                    if (OperatingSystem.IsAndroidVersionAtLeast(29))
                    {
                        window.NavigationBarContrastEnforced = false;
                    }

                    if (OperatingSystem.IsAndroidVersionAtLeast(28) && window.Attributes != null)
                    {
                        var lp = window.Attributes;
                        lp.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
                        window.Attributes = lp;
                    }

                    var controller = WindowCompat.GetInsetsController(window, window.DecorView);
                    if (controller != null)
                    {
                        controller.AppearanceLightStatusBars = !isDark;
                        controller.AppearanceLightNavigationBars = !isDark;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ApplyThemeColors] Error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Returns the physical display cutout (notch) insets in density-independent pixels (dp).
        /// </summary>
        public static (double left, double top, double right, double bottom) GetCutoutInsetsDp()
        {
            try
            {
                var activity = Instance ?? Platform.CurrentActivity as MainActivity;
                if (activity?.Window == null) return (0, 0, 0, 0);

                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    var insets = activity.Window.DecorView.RootWindowInsets;
                    var cutout = insets?.DisplayCutout;
                    if (cutout != null)
                    {
                        var density = activity.Resources?.DisplayMetrics?.Density ?? 1.0f;
                        if (density <= 0) density = 1.0f;

                        return (
                            cutout.SafeInsetLeft / density,
                            cutout.SafeInsetTop / density,
                            cutout.SafeInsetRight / density,
                            cutout.SafeInsetBottom / density
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] GetCutoutInsetsDp error: {ex.Message}");
            }

            return (0, 0, 0, 0);
        }

        /// <summary>
        /// Returns physical landscape insets in density-independent pixels (dp):
        /// (cutoutLeft, cutoutRight, systemLeftInset, systemRightInset)
        /// where systemLeftInset / systemRightInset measure how far Android's content view
        /// is already offset from the physical screen edges (e.g. by the system navigation bar).
        /// </summary>
        public static (double cutoutLeft, double cutoutRight, double systemLeftInset, double systemRightInset) GetLandscapeInsetsDp()
        {
            try
            {
                var activity = Instance ?? Platform.CurrentActivity as MainActivity;
                if (activity?.Window == null) return (0, 0, 0, 0);

                var density = activity.Resources?.DisplayMetrics?.Density ?? 1.0f;
                if (density <= 0) density = 1.0f;

                double cutoutLeft = 0;
                double cutoutRight = 0;

                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    var insets = activity.Window.DecorView.RootWindowInsets;
                    var cutout = insets?.DisplayCutout;
                    if (cutout != null)
                    {
                        cutoutLeft = cutout.SafeInsetLeft / density;
                        cutoutRight = cutout.SafeInsetRight / density;
                    }
                }

                double systemLeft = 0;
                double systemRight = 0;

                var contentView = activity.FindViewById(global::Android.Resource.Id.Content);
                int screenWidthPx = 0;

                if (OperatingSystem.IsAndroidVersionAtLeast(30) && activity.WindowManager != null)
                {
                    var metrics = activity.WindowManager.CurrentWindowMetrics;
                    screenWidthPx = metrics.Bounds.Width();
                }

                if (screenWidthPx <= 0 && activity.WindowManager != null)
                {
                    var dm = new global::Android.Util.DisplayMetrics();
#pragma warning disable CA1422
                    activity.WindowManager.DefaultDisplay?.GetRealMetrics(dm);
#pragma warning restore CA1422
                    screenWidthPx = dm.WidthPixels;
                }

                if (contentView != null && screenWidthPx > 0 && contentView.Width > 0)
                {
                    int[] loc = new int[2];
                    contentView.GetLocationOnScreen(loc);
                    int leftPx = loc[0];
                    int rightPx = screenWidthPx - (leftPx + contentView.Width);

                    if (leftPx > 0) systemLeft = leftPx / density;
                    if (rightPx > 0) systemRight = rightPx / density;
                }
                else if (activity.Window?.DecorView != null)
                {
                    var rootInsets = activity.Window.DecorView.RootWindowInsets;
                    if (rootInsets != null)
                    {
                        var compat = WindowInsetsCompat.ToWindowInsetsCompat(rootInsets, activity.Window.DecorView);
                        var navInsets = compat?.GetInsets(WindowInsetsCompat.Type.NavigationBars());
                        if (navInsets != null)
                        {
                            systemLeft = navInsets.Left / density;
                            systemRight = navInsets.Right / density;
                        }
                    }
                }

                return (cutoutLeft, cutoutRight, systemLeft, systemRight);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] GetLandscapeInsetsDp error: {ex.Message}");
                return (0, 0, 0, 0);
            }
        }

    }
}
