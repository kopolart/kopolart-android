namespace KopolArt.Mobile
{
    public static class Constants
    {
        public static class Urls
        {
            public const string Home = "https://kopolart.ir/";
            public const string AboutUs = "https://kopolart.ir/about-us";
            public const string Donate = "https://kopolart.ir/donate";
            public const string Faq = "https://kopolart.ir/faq";
            public const string Terms = "https://kopolart.ir/terms";
            public const string License = "https://kopolart.ir/license";
            public const string CcByLicense = "https://creativecommons.org/licenses/by/4.0/";
            public const string MainHost = "kopolart.ir";
            public const string WwwMainHost = "www.kopolart.ir";
            public const string UpdateJsonUrl = "https://kopolart.ir/json/app-updates-android.json";
            public const string UserLogin = "https://kopolart.ir/login";
            public const string UserRegister = "https://kopolart.ir/register";
            public const string UserProfileBase = "https://kopolart.ir/user/";
            public const string TopBarInfoUrl = "https://kopolart.ir/api/topbar-info";

            public const string Colors = "https://kopolart.ir/colors";
            public const string ColorPalettes = "https://kopolart.ir/color-palettes";
            public const string Posts = "https://kopolart.ir/posts";
            public const string Wallpapers = "https://kopolart.ir/wallpapers";
            public const string Tools = "https://kopolart.ir/tools";

            public const string PostsSearch = "https://kopolart.ir/posts/search";
            public const string ColorsSearch = "https://kopolart.ir/colors/search";
            public const string ColorPalettesSearch = "https://kopolart.ir/color-palettes/search";
            public const string WallpapersSearch = "https://kopolart.ir/wallpapers/search";

            public const string RssFeed = "https://kopolart.ir/feed.xml";

            public const string ShareScheme = "kopolart-share:";

            public const string Instagram = "https://instagram.com/ali.yousefi.ir";
            public const string Telegram = "https://t.me/kopolart";
            public const string Bale = "https://ble.ir/kopolart";
            public const string Eita = "https://eitaa.com/kopolart";
            public const string Github = "https://github.com/kopolart";

            public static readonly string[] AllowedSocialHosts =
            [
                "instagram.com",
                "www.instagram.com",
                "t.me",
                "telegram.me",
                "telegram.org",
                "api.whatsapp.com",
                "wa.me",
                "whatsapp.com",
                "www.whatsapp.com",
                "x.com",
                "www.x.com",
                "twitter.com",
                "www.twitter.com",
                "youtube.com",
                "www.youtube.com",
                "youtu.be",
                "linkedin.com",
                "www.linkedin.com",
                "pinterest.com",
                "www.pinterest.com",
                "ble.ir",
                "www.ble.ir",
                "aparat.com",
                "www.aparat.com",
                "eitaa.com",
                "www.eitaa.com"
            ];
        }

        public static class Paths
        {
            public const string OfflinePagesFolderName = "OfflinePages";
        }

        public static class Network
        {
            public static string AppUserAgent
            {
                get
                {
                    try
                    {
                        return $"KopolArtMobile/{AppInfo.Current.VersionString}";
                    }
                    catch
                    {
                        return "KopolArtMobile/1.0.0";
                    }
                }
            }
        }

        public static class Keys
        {
            public static readonly string UserAgentSuffix = $" KopolArtMobile/{AppInfo.Current.VersionString}";
            public const string SocialPromoLastShown = "SocialPromoLastShown";
            public const string AppUpdateLastReminded = "AppUpdateLastReminded";
            public const string AppThemeMode = "AppThemeMode";
            public const string AppThemeIsDark = "AppTheme_IsDark";
            public const string OpenDashboardAtStartup = "OpenDashboardAtStartup";
            public const string AppLastVisitUtc = "AppLastVisitUtc";
            public const string AppTotalUsageSeconds = "AppTotalUsageSeconds";
            public const string AppAwardedMilestones = "AppAwardedMilestones";
            public const string UserCookieName = "kopol_user";
            public const string HasCompletedOnboarding = "HasCompletedOnboarding";
            public const string NotifyNewContent = "NotifyNewContent";
            public const string RssLastNotifiedPostGuid = "Rss_LastNotifiedPostGuid";
            public const string RssLastNotifiedUtc = "Rss_LastNotifiedUtc";
            public const string RssBaselineInitialized = "Rss_BaselineInitialized";
            public const string RssLastCheckUtc = "Rss_LastCheckUtc";
            public const string LastSelectedSearchCategory = "LastSelectedSearchCategory";
            public const string RecentSearchQueries = "RecentSearchQueries";
        }

        public static class PartialVisitKeys
        {
            public const string AndroidAppBrowse = "app-android";
            public const string JosephyBrowse = "app-josephy";
        }

        public static class Fonts
        {
            // Local builds keep the licensed Abar family; the public build ships OFL Estedad
            // because Abar cannot be redistributed. The csproj defines KOPOLAR_FONT_ABAR
            // automatically whenever the Abar files are present in Resources/Fonts.
#if KOPOLAR_FONT_ABAR
            public const string UiFontMediumFile = "abar-medium.ttf";
            public const string UiFontBoldFile = "abar-bold.ttf";
            public const string UiFontBlackFile = "abar-black.ttf";
#else
            public const string UiFontMediumFile = "estedad-regular.ttf";
            public const string UiFontBoldFile = "estedad-extrabold.ttf";
            public const string UiFontBlackFile = "estedad-black.ttf";
#endif
            public const string KopolArtFaFont = "kopolart-fa.ttf";

            // Website web-font assets used by the offline renderer (premium family only).
            public const string AbarVarWoff2 = "abar-var.woff2";
            public const string AbarStaticWoff = "abar-static.woff";
            public const string AbarStaticBoldWoff = "abar-static-bold.woff";
            public const string KopolArtFaWoff2 = "kopolart-fa.woff2";
            public const string KopolArtFaWoff = "kopolart-fa.woff";
            public const string KopolArtFaWebWoff2 = "kopolart-fa-web.woff2";
            public const string KopolArtFaWebWoff = "kopolart-fa-web.woff";
            public const string KopolArtFaCss = "kopolart-fa.css";

            public const string UiFontBlackAlias = "KopolFontBlack";
            public const string UiFontBoldAlias = "KopolFontBold";
            public const string UiFontMediumAlias = "KopolFontMedium";
            public const string UiFontRegularAlias = "KopolFontRegular";
            public const string KopolArtFaAlias = "KopolArtFa";
        }

        public static class Theme
        {
            public const string DarkModeCookieName = "darkmode-kopolart";
            public const string LightColor = "#e2cdb4";
            public const string DarkColor = "#454340";
        }

        public static class SocialPromo
        {
            public const int IntervalDays = 5;
        }
    }
}
