using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IUsageTrackerService _usageTrackerService;
        private readonly UpdateService _updateService;
        private readonly ICustomAlertService _customAlertService;
        private readonly IBackupService _backupService;
        private readonly IHomeScreenService? _homeScreenService;
        private readonly INotificationPermissionService? _notificationPermissionService;
        private readonly INewContentNotifier? _newContentNotifier;

        private bool _showLoadingOnNavigation;
        private bool _openDashboardAtStartup;
        private bool _restoreLastPageAtStartup;
        private bool _autoClearCacheOnExit;
        private bool _notifyNewContent = true;
        private string _selectedTheme = "System";
        private int _selectedWidgetOpacity = 100;

        private readonly IDownloadFolderPickerService? _downloadFolderPickerService;
        private readonly IDownloadHistoryService? _downloadHistoryService;
        private string _downloadFolderDisplayName = StoragePathHelper.DefaultDownloadFolderDisplay;
        private bool _isCustomDownloadFolderSet;
        private bool _askBeforeDownload = true;

        private string _cacheSize = "در حال محاسبه...";
        private string _appDataSize = "در حال محاسبه...";
        private string _cookieCount = "۰ کوکی";

        private bool _hasUpdateBadge;
        private string _newVersionNumber = string.Empty;
        private string _updateStatusText = "برنامه شما به‌روز است";
        private bool _isCheckingUpdate;

        public bool ShowLoadingOnNavigation
        {
            get => _showLoadingOnNavigation;
            set
            {
                if (SetProperty(ref _showLoadingOnNavigation, value))
                {
                    _settingsService.Settings.ShowLoadingOnNavigation = value;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
                }
            }
        }

        public bool OpenDashboardAtStartup
        {
            get => _openDashboardAtStartup;
            set
            {
                if (SetProperty(ref _openDashboardAtStartup, value))
                {
                    _settingsService.Settings.OpenDashboardAtStartup = value;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
                }
            }
        }

        public bool RestoreLastPageAtStartup
        {
            get => _restoreLastPageAtStartup;
            set
            {
                if (SetProperty(ref _restoreLastPageAtStartup, value))
                {
                    _settingsService.Settings.RestoreLastPageAtStartup = value;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
                }
            }
        }

        public bool AutoClearCacheOnExit
        {
            get => _autoClearCacheOnExit;
            set
            {
                if (SetProperty(ref _autoClearCacheOnExit, value))
                {
                    _settingsService.Settings.AutoClearCacheOnExit = value;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
                }
            }
        }

        public bool IsSystemThemeSelected => string.Equals(SelectedTheme, "System", StringComparison.OrdinalIgnoreCase);
        public bool IsLightThemeSelected => string.Equals(SelectedTheme, "Light", StringComparison.OrdinalIgnoreCase);
        public bool IsDarkThemeSelected => string.Equals(SelectedTheme, "Dark", StringComparison.OrdinalIgnoreCase);

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (SetProperty(ref _selectedTheme, value))
                {
                    OnPropertyChanged(nameof(IsSystemThemeSelected));
                    OnPropertyChanged(nameof(IsLightThemeSelected));
                    OnPropertyChanged(nameof(IsDarkThemeSelected));

                    _settingsService.Settings.AppThemeMode = value;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
                    _settingsService.ApplyTheme(value);
                    _homeScreenService?.NotifyWidgetUpdate();
                }
            }
        }

        public int SelectedWidgetOpacity
        {
            get => _selectedWidgetOpacity;
            set
            {
                if (SetProperty(ref _selectedWidgetOpacity, value))
                {
                    _settingsService.Settings.WidgetOpacity = value;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
                    Preferences.Set("WidgetOpacity", value);
                    _homeScreenService?.NotifyWidgetUpdate();
                }
            }
        }

        public string CacheSize
        {
            get => _cacheSize;
            set => SetProperty(ref _cacheSize, value);
        }

        public string AppDataSize
        {
            get => _appDataSize;
            set => SetProperty(ref _appDataSize, value);
        }

        public string CookieCount
        {
            get => _cookieCount;
            set => SetProperty(ref _cookieCount, value);
        }

        public bool HasUpdateBadge
        {
            get => _hasUpdateBadge;
            set => SetProperty(ref _hasUpdateBadge, value);
        }

        public string NewVersionNumber
        {
            get => _newVersionNumber;
            set => SetProperty(ref _newVersionNumber, value);
        }

        public string UpdateStatusText
        {
            get => _updateStatusText;
            set => SetProperty(ref _updateStatusText, value);
        }

        public bool IsCheckingUpdate
        {
            get => _isCheckingUpdate;
            set => SetProperty(ref _isCheckingUpdate, value);
        }

        public bool NotifyNewContent
        {
            get => _notifyNewContent;
            set
            {
                if (SetProperty(ref _notifyNewContent, value))
                {
                    _settingsService.Settings.NotifyNewContent = value;
                    Preferences.Set(Constants.Keys.NotifyNewContent, value);
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);

                    if (value && _notificationPermissionService != null)
                    {
                        _ = CheckAndRequestNotificationPermissionAsync();
                    }
                }
            }
        }

        public string CurrentAppVersion => $"نسخه {AppInfo.Current.VersionString}".ToPersianDigits();

        public string DownloadFolderDisplayName
        {
            get => _downloadFolderDisplayName;
            set => SetProperty(ref _downloadFolderDisplayName, value);
        }

        public bool IsCustomDownloadFolderSet
        {
            get => _isCustomDownloadFolderSet;
            set => SetProperty(ref _isCustomDownloadFolderSet, value);
        }

        public bool AskBeforeDownload
        {
            get => _askBeforeDownload;
            set
            {
                if (SetProperty(ref _askBeforeDownload, value))
                {
                    _settingsService.Settings.DontShowDownloadPromptAgain = !value;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
                }
            }
        }

        public ICommand SetThemeCommand { get; }
        public ICommand ClearCacheCommand { get; }
        public ICommand ClearAppDataCommand { get; }
        public ICommand ClearCookiesCommand { get; }
        public ICommand ResetUsageStatsCommand { get; }
        public ICommand ResetDefaultsCommand { get; }
        public ICommand CheckUpdateCommand { get; }
        public ICommand OpenUpdatesHistoryCommand { get; }
        public ICommand OpenHistoryCommand { get; }
        public ICommand ExportBackupCommand { get; }
        public ICommand ImportBackupCommand { get; }
        public ICommand OpenOnboardingCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ChangeDownloadFolderCommand { get; }
        public ICommand ResetDownloadFolderCommand { get; }
        public ICommand PinWidgetCommand { get; }
        public ICommand PinShortcutCommand { get; }
        public ICommand SendTestPostNotificationCommand { get; }

        public bool IsPinWidgetSupported => _homeScreenService?.IsPinWidgetSupported() ?? false;
        public bool IsPinShortcutSupported => _homeScreenService?.IsPinShortcutSupported() ?? false;

        public event EventHandler<(UpdateInfo Info, List<UpdateRelease> Releases)>? RequestShowUpdatePopup;

        public SettingsViewModel(
            ISettingsService settingsService,
            IUsageTrackerService usageTrackerService,
            UpdateService updateService,
            ICustomAlertService customAlertService,
            IBackupService backupService,
            IDownloadFolderPickerService? downloadFolderPickerService = null,
            IDownloadHistoryService? downloadHistoryService = null,
            IHomeScreenService? homeScreenService = null,
            INotificationPermissionService? notificationPermissionService = null,
            INewContentNotifier? newContentNotifier = null)
        {
            _settingsService = settingsService;
            _downloadFolderPickerService = downloadFolderPickerService;
            _downloadHistoryService = downloadHistoryService;
            _usageTrackerService = usageTrackerService;
            _updateService = updateService;
            _customAlertService = customAlertService;
            _backupService = backupService;
            _homeScreenService = homeScreenService;
            _notificationPermissionService = notificationPermissionService;
            _newContentNotifier = newContentNotifier;

            _showLoadingOnNavigation = _settingsService.Settings.ShowLoadingOnNavigation;
            _openDashboardAtStartup = _settingsService.Settings.OpenDashboardAtStartup;
            _restoreLastPageAtStartup = _settingsService.Settings.RestoreLastPageAtStartup;
            _autoClearCacheOnExit = _settingsService.Settings.AutoClearCacheOnExit;
            _notifyNewContent = _settingsService.Settings.NotifyNewContent;
            _selectedTheme = _settingsService.Settings.AppThemeMode;
            _selectedWidgetOpacity = _settingsService.Settings.WidgetOpacity;

            SetThemeCommand = new Command<string>(theme =>
            {
                if (!string.IsNullOrEmpty(theme))
                    SelectedTheme = theme;
            });

            ClearCacheCommand = new Command(async () => await ClearCacheAsync());
            ClearAppDataCommand = new Command(async () => await ClearAppDataAsync());
            ClearCookiesCommand = new Command(async () => await ClearCookiesAsync());
            ResetUsageStatsCommand = new Command(async () => await ResetUsageStatsAsync());
            ResetDefaultsCommand = new Command(async () => await ResetDefaultsAsync());
            CheckUpdateCommand = new Command(async () => await CheckUpdateAsync());
            OpenUpdatesHistoryCommand = new Command(async () => await Shell.Current.GoToAsync("UpdatesHistoryPage"));
            OpenHistoryCommand = new Command(async () => await Shell.Current.GoToAsync("HistoryPage"));
            ExportBackupCommand = new Command(async () => await _backupService.ExportBackupAsync());
            ImportBackupCommand = new Command(async () =>
            {
                var success = await _backupService.ImportBackupAsync();
                if (success)
                {
                    ShowLoadingOnNavigation = _settingsService.Settings.ShowLoadingOnNavigation;
                    OpenDashboardAtStartup = _settingsService.Settings.OpenDashboardAtStartup;
                    RestoreLastPageAtStartup = _settingsService.Settings.RestoreLastPageAtStartup;
                    AutoClearCacheOnExit = _settingsService.Settings.AutoClearCacheOnExit;
                    NotifyNewContent = _settingsService.Settings.NotifyNewContent;
                    SelectedTheme = _settingsService.Settings.AppThemeMode;
                    SelectedWidgetOpacity = _settingsService.Settings.WidgetOpacity;
                    UpdateDownloadFolderState();
                    RefreshStorageMetrics();
                }
            });
            OpenOnboardingCommand = new Command(async () => await Shell.Current.GoToAsync("OnboardingPage"));
            BackCommand = new Command(async () => await NavigationHelper.GoBackSafelyAsync());
            ChangeDownloadFolderCommand = new Command(async () => await ChangeDownloadFolderAsync());
            ResetDownloadFolderCommand = new Command(async () => await ResetDownloadFolderAsync());
            PinWidgetCommand = new Command(async () => await PinWidgetAsync());
            PinShortcutCommand = new Command(async () => await PinShortcutAsync());
            SendTestPostNotificationCommand = new Command(async () => await SendTestPostNotificationAsync());

            UpdateDownloadFolderState();
            RefreshStorageMetrics();
            _ = CheckUpdateSilentlyAsync();
        }

        public void RefreshStorageMetrics()
        {
            Task.Run(async () =>
            {
                var cacheBytes = await _settingsService.GetCacheSizeBytesAsync();
                var dataBytes = await _settingsService.GetAppDataSizeBytesAsync();
                var cookies = _settingsService.GetCookieCount();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CacheSize = PersianFormatHelper.FormatBytes(cacheBytes);
                    AppDataSize = PersianFormatHelper.FormatBytes(dataBytes);
                    CookieCount = $"{cookies} کوکی".ToPersianDigits();
                });
            });
        }

    }
}
