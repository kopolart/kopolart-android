using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android
{
    /// <summary>
    /// Installer used by the Cafe Bazaar build (BAZAAR_BUILD).
    ///
    /// Android 8.0 (API 26) and newer only let an app launch the package installer when it
    /// declares android.permission.REQUEST_INSTALL_PACKAGES, and Cafe Bazaar rejects that
    /// permission unless installing APKs is the app's core function. This build therefore
    /// never downloads or installs an APK: it opens the app's Bazaar page and lets the store
    /// deliver the update (Bazaar updates installed apps automatically).
    /// </summary>
    public class AndroidBazaarAppInstaller : IAppInstaller
    {
        public bool IsStoreManagedUpdate => true;

        // Nothing to grant: the permission is intentionally absent from this build.
        public bool CanInstallDirectly() => false;

        public void RequestInstallPermission()
        {
            // No permission exists in this build; the store page is the only path.
            OpenStorePage();
        }

        // Safety net: if any caller still reaches Install(), never attempt an APK install.
        public void Install(string localFilePath) => OpenStorePage();

        public bool OpenStorePage() => AndroidStorePageLauncher.TryOpenAppPage();
    }
}
