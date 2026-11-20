using Android.Content;

namespace KopolArt.Mobile.Platforms.Android
{
    /// <summary>
    /// Opens this app's page in the Cafe Bazaar client (deep link) or, when Bazaar is not
    /// installed, on cafebazaar.ir in a browser.
    ///
    /// Store builds use this instead of an in-app APK install: Android 8.0 (API 26) and newer
    /// refuse any app-initiated install unless the app declares
    /// android.permission.REQUEST_INSTALL_PACKAGES, and Cafe Bazaar rejects that permission
    /// for apps whose core function is not installing APKs.
    /// </summary>
    internal static class AndroidStorePageLauncher
    {
        private const string BazaarPackageName = "com.farsitel.bazaar";
        private const string BazaarDeepLinkFormat = "bazaar://details?id={0}";
        private const string BazaarWebUrlFormat = "https://cafebazaar.ir/app/{0}";

        /// <summary>
        /// Tries the Bazaar deep link first, then the web page.
        /// Returns false when neither could be opened.
        /// </summary>
        public static bool TryOpenAppPage()
        {
            var context = global::Android.App.Application.Context;
            var packageName = context.PackageName;
            if (string.IsNullOrWhiteSpace(packageName))
                return false;

            var deepLink = new Intent(Intent.ActionView,
                global::Android.Net.Uri.Parse(string.Format(BazaarDeepLinkFormat, packageName)));
            deepLink.SetPackage(BazaarPackageName);
            deepLink.AddFlags(ActivityFlags.NewTask);

            if (TryStart(context, deepLink))
                return true;

            var webPage = new Intent(Intent.ActionView,
                global::Android.Net.Uri.Parse(string.Format(BazaarWebUrlFormat, packageName)));
            webPage.AddFlags(ActivityFlags.NewTask);

            return TryStart(context, webPage);
        }

        private static bool TryStart(Context context, Intent intent)
        {
            try
            {
                if (Platform.CurrentActivity != null)
                    Platform.CurrentActivity.StartActivity(intent);
                else
                    context.StartActivity(intent);

                return true;
            }
            catch (Exception ex)
            {
                // ActivityNotFoundException when neither Bazaar nor a browser handles the URI.
                System.Diagnostics.Debug.WriteLine($"[AndroidStorePageLauncher] Could not open store page: {ex.Message}");
                return false;
            }
        }
    }
}
