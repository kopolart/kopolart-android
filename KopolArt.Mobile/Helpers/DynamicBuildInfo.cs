using System.Reflection;

namespace KopolArt.Mobile.Helpers
{
    public static class DynamicBuildInfo
    {
        public static string AppVersion => AppInfo.Current.VersionString;
        public static string BuildNumber => AppInfo.Current.BuildString;
        public static string PackageName => AppInfo.Current.PackageName;
        public static string AppName => AppInfo.Current.Name;

        public static string MauiVersion
        {
            get
            {
                try
                {
                    var assembly = typeof(Element).Assembly;
                    var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    if (!string.IsNullOrWhiteSpace(infoVersion))
                    {
                        var plusIdx = infoVersion.IndexOf('+');
                        return plusIdx > 0 ? infoVersion[..plusIdx] : infoVersion;
                    }
                    return assembly.GetName().Version?.ToString() ?? "10.0";
                }
                catch
                {
                    return "10.0";
                }
            }
        }

        public static string DeviceModel => $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}";
        public static string OsVersion => $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}";

        public static string MinSupportedAndroid
        {
            get
            {
#if ANDROID
                try
                {
                    var context = Android.App.Application.Context;
                    var minSdk = context.ApplicationInfo?.MinSdkVersion ?? 26;
                    return $"\u200Fاندروید {AndroidVersionName(minSdk)} \u202A(API {minSdk})\u202C به بالا\u200F";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DynamicBuildInfo] Failed to read min SDK info: {ex.Message}");
                }
#endif
                return "\u200Fاندروید ۸.۰ \u202A(API 26)\u202C به بالا\u200F";
            }
        }

        private static string AndroidVersionName(int apiLevel) => apiLevel switch
        {
            26 => "۸.۰",
            27 => "۸.۱",
            28 => "۹",
            29 => "۱۰",
            30 => "۱۱",
            31 or 32 => "۱۲",
            33 => "۱۳",
            34 => "۱۴",
            35 => "۱۵",
            36 => "۱۶",
            37 => "۱۷",
            _ => apiLevel.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        public static string TargetAndroidApi
        {
            get
            {
#if ANDROID
                try
                {
                    var context = Android.App.Application.Context;
                    var targetSdk = (int)(context.ApplicationInfo?.TargetSdkVersion ?? (Android.OS.BuildVersionCodes)37);
                    return $"API {targetSdk}";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DynamicBuildInfo] Failed to read target SDK info: {ex.Message}");
                }
#endif
                return "API 37";
            }
        }
    }
}
