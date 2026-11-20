namespace KopolArt.Mobile.Services
{
    public interface IAppInstaller
    {
        /// <summary>
        /// True when the distribution store owns updates and the app must never install an
        /// APK itself (Cafe Bazaar builds). The update UI then redirects to the store page
        /// instead of downloading and launching the package installer.
        /// </summary>
        bool IsStoreManagedUpdate { get; }

        bool CanInstallDirectly();
        void RequestInstallPermission();
        void Install(string localFilePath);

        /// <summary>
        /// Opens this app's page in the store so the user can install the update there.
        /// Returns false when neither the store client nor a browser could be launched.
        /// </summary>
        bool OpenStorePage();
    }
}
