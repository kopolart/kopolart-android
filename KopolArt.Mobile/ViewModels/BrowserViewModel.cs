using System.Windows.Input;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;
namespace KopolArt.Mobile.ViewModels
{
    public partial class BrowserViewModel : BaseViewModel, IDisposable
    {
        private const int MaxRetries = 3;
        private const int BaseRetryDelayMs = 1000;
        private readonly SocialPromoService _socialPromoService;
        private readonly UpdateService _updateService;
        private readonly IUsageTrackerService _usageTrackerService;
        private readonly ISettingsService _settingsService;
        private readonly IHistoryService _historyService;
        private readonly INetworkStatsService _networkStatsService;
        private readonly IBookmarkService _bookmarkService;
        private readonly ICustomAlertService _customAlertService;
        private readonly IUserService _userService;
        private readonly IOfflinePageService? _offlinePageService;
        private CancellationTokenSource? _retryCts;
        private int _retryCount;
        private bool _disposed;
        private bool _promoChecked;
        private bool _welcomeToastShown;
        private bool _isInitialLoadDone;
        private bool _isPullRefreshing;

        public UpdateService UpdateService => _updateService;
        public IUsageTrackerService UsageTrackerService => _usageTrackerService;
        public ISettingsService SettingsService => _settingsService;
        public IBookmarkService BookmarkService => _bookmarkService;
        public ICustomAlertService CustomAlertService => _customAlertService;
        public IUserService UserService => _userService;
        public IOfflinePageService? OfflinePageService => _offlinePageService;

        public event EventHandler? RequestReload;
        public event EventHandler? RequestShowPromo;
        public event EventHandler<(UpdateInfo UpdateInfo, List<UpdateRelease> Releases)>? RequestShowUpdate;
        public event EventHandler<string>? RequestShowWelcomeToast;
        public event EventHandler? RequestActivateFindInPage;
        public event EventHandler<(string Url, string Title)>? RequestSavePageForOffline;

        private bool _hasSuccessfullyLoadedPage;

        public bool IsPullRefreshing
        {
            get => _isPullRefreshing;
            set => SetProperty(ref _isPullRefreshing, value);
        }

        public ICommand PullRefreshCommand { get; }

        public BrowserViewModel(
            SocialPromoService socialPromoService,
            UpdateService updateService,
            IUsageTrackerService usageTrackerService,
            ISettingsService settingsService,
            IHistoryService historyService,
            INetworkStatsService networkStatsService,
            IBookmarkService bookmarkService,
            ICustomAlertService customAlertService,
            IUserService userService,
            IOfflinePageService? offlinePageService = null)
        {
            _socialPromoService = socialPromoService;
            _updateService = updateService;
            _usageTrackerService = usageTrackerService;
            _settingsService = settingsService;
            _historyService = historyService;
            _networkStatsService = networkStatsService;
            _bookmarkService = bookmarkService;
            _customAlertService = customAlertService;
            _userService = userService;
            _offlinePageService = offlinePageService;

            InitializeWelcomeInfo();
            RetryCommand = new Command(OnRetry);
            PullRefreshCommand = new Command(() =>
            {
                if (!IsLoading)
                {
                    RaiseReloadRequest();
                }
                else
                {
                    IsPullRefreshing = false;
                }
            });
            SetLoadingState();
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }
        public void InitializeWelcomeInfo()
        {
            var welcomeInfo = _usageTrackerService.GetWelcomeInfo();
            var currentUser = _userService.GetCurrentUser();
            if (currentUser != null && !string.IsNullOrWhiteSpace(currentUser.Name))
            {
                var displayName = currentUser.Name.Trim();
                if (displayName.Length > 20)
                {
                    displayName = displayName.Substring(0, 18) + "...";
                }
                WelcomeUserName = $"{displayName} عزیز!";
                HasWelcomeUserName = true;
                WelcomeTitle = welcomeInfo.Title;
                WelcomeSubtitle = welcomeInfo.Subtitle ?? string.Empty;
                WelcomeFullMessage = $"{displayName} عزیز، {welcomeInfo.FullMessage}";
            }
            else
            {
                WelcomeUserName = string.Empty;
                HasWelcomeUserName = false;
                WelcomeTitle = welcomeInfo.Title;
                WelcomeSubtitle = welcomeInfo.Subtitle ?? string.Empty;
                WelcomeFullMessage = welcomeInfo.FullMessage;
            }
            HasWelcomeMessage = !string.IsNullOrWhiteSpace(welcomeInfo.Title);
            HasWelcomeSubtitle = !string.IsNullOrWhiteSpace(welcomeInfo.Subtitle);
            ShowWelcomeOnLoading = !_usageTrackerService.HasShownWelcomeOnLoading && !_isInitialLoadDone && HasWelcomeMessage;
        }
        public bool IsLoading
        {
            get;
            set => SetProperty(ref field, value);
        }
        public bool IsError
        {
            get;
            set => SetProperty(ref field, value);
        }
        public bool IsBrowserVisible
        {
            get;
            set => SetProperty(ref field, value);
        }
        public string LoadingText
        {
            get;
            set => SetProperty(ref field, value);
        } = "...";
        public string RetryText
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;
        public bool ShowRetryText
        {
            get;
            set => SetProperty(ref field, value);
        }
        public string WelcomeUserName
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;
        public bool HasWelcomeUserName
        {
            get;
            set => SetProperty(ref field, value);
        }
        public string WelcomeTitle
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;
        public string WelcomeSubtitle
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;
        public string WelcomeFullMessage
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;
        public bool HasWelcomeMessage
        {
            get;
            set => SetProperty(ref field, value);
        }
        public bool HasWelcomeSubtitle
        {
            get;
            set => SetProperty(ref field, value);
        }
        public bool ShowWelcomeOnLoading
        {
            get;
            set => SetProperty(ref field, value);
        }
        public ICommand RetryCommand { get; }
        public string AppVersion => $"v{AppInfo.Current.VersionString}";
        public bool ShowVersion
        {
            get;
            set => SetProperty(ref field, value);
        } = true;
        private void OnRetry()
        {
            CancelRetry();
            _retryCount = 0;
            SetLoadingState();
            RaiseReloadRequest();
        }
        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (_disposed)
                return;
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                if (IsError)
                {
                    MainThread.BeginInvokeOnMainThread(OnRetry);
                }
            }
        }
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
            CancelRetry();
        }
    }
}
