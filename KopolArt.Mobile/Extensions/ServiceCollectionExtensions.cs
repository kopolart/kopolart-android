using KopolArt.Mobile.Services;
using KopolArt.Mobile.ViewModels;
using KopolArt.Mobile.Views;

namespace KopolArt.Mobile.Extensions
{
    public static class ServiceCollectionExtensions
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddCoreServices()
            {
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IHistoryService, HistoryService>();
                services.AddSingleton<IDownloadHistoryService, DownloadHistoryService>();
                services.AddSingleton<IBookmarkService, BookmarkService>();
                services.AddSingleton<INetworkStatsService, NetworkStatsService>();
                services.AddSingleton<ICustomAlertService, CustomAlertService>();
                services.AddSingleton<IUsageMilestoneService, UsageMilestoneService>();

                services.AddSingleton<UsageTrackerService>();
                services.AddSingleton<IUsageTrackerService>(sp => sp.GetRequiredService<UsageTrackerService>());
                services.AddSingleton<IWelcomeTrackerService>(sp => sp.GetRequiredService<UsageTrackerService>());

                services.AddSingleton<SocialPromoService>();
                services.AddSingleton<UpdateService>();
                services.AddSingleton<IBackupService, BackupService>();
                services.AddSingleton<IUserService, UserService>();
                services.AddSingleton<IAppVisitTrackerService, AppVisitTrackerService>();
                services.AddSingleton<IOnlineUsersService, OnlineUsersService>();
                services.AddSingleton<IOfflinePageService, OfflinePageService>();
                services.AddSingleton<IRssFeedService, RssFeedService>();
                services.AddSingleton<IUsageAnalyticsService, UsageAnalyticsService>();
                services.AddSingleton<ISearchSuggestionService, SearchSuggestionService>();
                services.AddSingleton<ISearchHistoryService, SearchHistoryService>();

                return services;
            }

            public IServiceCollection AddViewModels()
            {
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<BrowserViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<HistoryViewModel>();
                services.AddTransient<DownloadHistoryViewModel>();
                services.AddTransient<BookmarksViewModel>();
                services.AddTransient<OfflinePagesViewModel>();
                services.AddTransient<UsageViewModel>();
                services.AddTransient<AboutViewModel>();
                services.AddTransient<ContactUsViewModel>();
                services.AddTransient<OnboardingViewModel>();
                services.AddTransient<UpdatesHistoryViewModel>();
                services.AddTransient<GesturesViewModel>();
                services.AddTransient<WallpaperPreviewViewModel>();


                return services;
            }

            public IServiceCollection AddViews()
            {
                services.AddTransient<DashboardPage>();
                services.AddTransient<BrowserPage>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<HistoryPage>();
                services.AddTransient<DownloadHistoryPage>();
                services.AddTransient<BookmarksPage>();
                services.AddTransient<OfflinePagesPage>();
                services.AddTransient<GesturesPage>();
                services.AddTransient<UsagePage>();
                services.AddTransient<AboutPage>();
                services.AddTransient<ContactUsPage>();
                services.AddTransient<OnboardingPage>();
                services.AddTransient<UpdatesHistoryPage>();
                services.AddTransient<WallpaperPreviewPage>();


                return services;
            }
        }
    }
}
