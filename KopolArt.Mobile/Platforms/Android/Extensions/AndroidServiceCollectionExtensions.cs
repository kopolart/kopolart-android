using KopolArt.Mobile.Platforms.Android.Services;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android.Extensions
{
    public static class AndroidServiceCollectionExtensions
    {
        public static IServiceCollection AddPlatformServices(this IServiceCollection services)
        {
            // Cafe Bazaar build: android.permission.REQUEST_INSTALL_PACKAGES is absent, so the
            // app hands updates to the Bazaar client instead of installing APKs itself.
#if BAZAAR_BUILD
            services.AddSingleton<IAppInstaller, AndroidBazaarAppInstaller>();
#else
            services.AddSingleton<IAppInstaller, AndroidAppInstaller>();
#endif
            services.AddSingleton<IUpdateDownloader, AndroidUpdateDownloader>();
            services.AddSingleton<INotificationPermissionService, AndroidNotificationPermissionService>();
            services.AddSingleton<INewContentNotifier>(_ => new NewContentNotifier(global::Android.App.Application.Context));
            services.AddSingleton<IDownloadFolderPickerService, AndroidDownloadFolderPickerService>();
            services.AddSingleton<IDownloadService, AndroidDownloadService>();
            services.AddSingleton<IHomeScreenService, AndroidHomeScreenService>();
            services.AddSingleton<IWallpaperService, AndroidWallpaperService>();

            return services;
        }
    }
}
