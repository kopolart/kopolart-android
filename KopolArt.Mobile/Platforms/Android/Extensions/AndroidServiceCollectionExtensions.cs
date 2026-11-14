using KopolArt.Mobile.Platforms.Android.Services;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android.Extensions
{
    public static class AndroidServiceCollectionExtensions
    {
        public static IServiceCollection AddPlatformServices(this IServiceCollection services)
        {
            services.AddSingleton<IAppInstaller, AndroidAppInstaller>();
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
