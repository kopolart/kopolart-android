using KopolArt.Mobile.Services;

namespace KopolArt.Mobile
{
    public partial class App : Application
    {
        private readonly IUsageTrackerService _usageTrackerService;
        private readonly ISettingsService _settingsService;
        private readonly IServiceProvider _serviceProvider;

        public App(IUsageTrackerService usageTrackerService, ISettingsService settingsService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _usageTrackerService = usageTrackerService;
            _settingsService = settingsService;
            _serviceProvider = serviceProvider;

            // Synchronously apply saved theme before window or pages are instantiated
            var savedTheme = Preferences.Get(Constants.Keys.AppThemeMode, "System");
            UserAppTheme = savedTheme switch
            {
                "Dark" => AppTheme.Dark,
                "Light" => AppTheme.Light,
                _ => AppTheme.Unspecified
            };

            // Initialize settings & theme on launch
            _ = _settingsService.InitializeAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            bool hasCompletedOnboarding = Preferences.Get(Constants.Keys.HasCompletedOnboarding, false) || _settingsService.Settings.HasCompletedOnboarding;
            Page rootPage = hasCompletedOnboarding
                ? new AppShell()
                : _serviceProvider.GetRequiredService<Views.OnboardingPage>();

            var window = new Window(rootPage);
            window.Deactivated += (s, e) =>
            {
                _usageTrackerService.PauseBrowsingTracking();
                _usageTrackerService.RecordVisit();
            };
            window.Stopped += (s, e) =>
            {
                _usageTrackerService.PauseBrowsingTracking();
                _usageTrackerService.RecordVisit();
                if (_settingsService.Settings.AutoClearCacheOnExit)
                {
                    _ = _settingsService.ClearCacheAsync();
                }
            };
            window.Activated += (s, e) =>
            {
                if (Shell.Current?.CurrentPage is Views.BrowserPage)
                {
                    _usageTrackerService.StartBrowsingTracking();
                }
                else if (Shell.Current?.CurrentPage is Views.DashboardPage dashboardPage)
                {
                    (dashboardPage.BindingContext as ViewModels.DashboardViewModel)?.RefreshDashboardInfo();
                }
            };
            return window;
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            _usageTrackerService.PauseBrowsingTracking();
            _usageTrackerService.RecordVisit();
            if (_settingsService.Settings.AutoClearCacheOnExit)
            {
                _ = _settingsService.ClearCacheAsync();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (Shell.Current?.CurrentPage is Views.BrowserPage)
            {
                _usageTrackerService.StartBrowsingTracking();
            }
        }
    }
}