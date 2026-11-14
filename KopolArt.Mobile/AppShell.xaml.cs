using KopolArt.Mobile.Views;

namespace KopolArt.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(BrowserPage), typeof(BrowserPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
            Routing.RegisterRoute(nameof(DownloadHistoryPage), typeof(DownloadHistoryPage));
            Routing.RegisterRoute(nameof(UsagePage), typeof(UsagePage));
            Routing.RegisterRoute(nameof(BookmarksPage), typeof(BookmarksPage));
            Routing.RegisterRoute(nameof(GesturesPage), typeof(GesturesPage));
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
            Routing.RegisterRoute(nameof(ContactUsPage), typeof(ContactUsPage));
            Routing.RegisterRoute(nameof(OnboardingPage), typeof(OnboardingPage));
            Routing.RegisterRoute(nameof(UpdatesHistoryPage), typeof(UpdatesHistoryPage));
            Routing.RegisterRoute(nameof(OfflinePagesPage), typeof(OfflinePagesPage));
            Routing.RegisterRoute(nameof(WallpaperPreviewPage), typeof(WallpaperPreviewPage));
        }
    }
}
