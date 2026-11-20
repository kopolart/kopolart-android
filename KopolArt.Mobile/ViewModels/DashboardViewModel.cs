using System.Collections.ObjectModel;

using System.Windows.Input;

using KopolArt.Mobile.Helpers;

using KopolArt.Mobile.Models;

using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        private readonly IUsageTrackerService _usageTrackerService;

        private readonly INetworkStatsService _networkStatsService;

        private readonly UpdateService _updateService;

        private readonly ICustomAlertService _customAlertService;

        private readonly IUserService _userService;

        private readonly IAppVisitTrackerService _visitTrackerService;

        private readonly IOnlineUsersService _onlineUsersService;

        private string _greeting = "روزت بخیر";

        private string _welcomeSubtitle = "به نرم‌افزار کپل‌آرت خوش آمدید";

        private string _totalUsageTime = "...";

        private string _totalDataUsed = "...";

        private string _onlineUsersCount = "--";

        private bool _isOnlineUsersLive;

        private bool _hasUpdateBadge;

        private string _newVersionBadge = string.Empty;

        private bool _isUserLoggedIn;

        private bool _isUserNotLoggedIn = true;

        private string _userGreetingText = string.Empty;

        private long _userId;

        private string _persianTodayDate = string.Empty;

        private bool _isShowingDateCard;

        private bool _isShowingAccountCard = true;

        private CancellationTokenSource? _rotationCts;

        private readonly List<SearchCategoryOption> _searchCategoryOptions = new();

        private SearchCategoryOption _selectedSearchCategory = null!;

        private string _searchQuery = string.Empty;

        private bool _hasSearchQuery;

        private readonly ISearchSuggestionService _searchSuggestionService;

        private CancellationTokenSource? _searchDebounceCts;

        private bool _isSearchingSuggestions;

        private bool _showSuggestions;

        private bool _hasNoSuggestions;

        public bool IsUserLoggedIn
        {
            get => _isUserLoggedIn;

            set => SetProperty(ref _isUserLoggedIn, value);
        }

        public bool IsUserNotLoggedIn
        {
            get => _isUserNotLoggedIn;

            set => SetProperty(ref _isUserNotLoggedIn, value);
        }

        public string UserGreetingText
        {
            get => _userGreetingText;

            set => SetProperty(ref _userGreetingText, value);
        }

        public long UserId
        {
            get => _userId;

            set => SetProperty(ref _userId, value);
        }

        public string PersianTodayDate
        {
            get => _persianTodayDate;

            set => SetProperty(ref _persianTodayDate, value);
        }

        public bool IsShowingDateCard
        {
            get => _isShowingDateCard;

            set => SetProperty(ref _isShowingDateCard, value);
        }

        public bool IsShowingAccountCard
        {
            get => _isShowingAccountCard;

            set => SetProperty(ref _isShowingAccountCard, value);
        }

        public string Greeting
        {
            get => _greeting;

            set => SetProperty(ref _greeting, value);
        }

        public string WelcomeSubtitle
        {
            get => _welcomeSubtitle;

            set => SetProperty(ref _welcomeSubtitle, value);
        }

        public string TotalUsageTime
        {
            get => _totalUsageTime;

            set => SetProperty(ref _totalUsageTime, value);
        }

        public string TotalDataUsed
        {
            get => _totalDataUsed;

            set => SetProperty(ref _totalDataUsed, value);
        }

        public string OnlineUsersCount
        {
            get => _onlineUsersCount;

            set => SetProperty(ref _onlineUsersCount, value);
        }

        public bool IsOnlineUsersLive
        {
            get => _isOnlineUsersLive;

            set => SetProperty(ref _isOnlineUsersLive, value);
        }

        public bool HasUpdateBadge
        {
            get => _hasUpdateBadge;

            set => SetProperty(ref _hasUpdateBadge, value);
        }

        public string NewVersionBadge
        {
            get => _newVersionBadge;

            set => SetProperty(ref _newVersionBadge, value);
        }

        public bool OpenDashboardAtStartup => _settingsService.Settings.OpenDashboardAtStartup;

        public ObservableCollection<DashboardCategoryItem> QuickCategories { get; } = new();

        public ObservableCollection<DashboardCategorySlide> CategorySlides { get; } = new();

        public SearchCategoryOption SelectedSearchCategory
        {
            get => _selectedSearchCategory;
            set
            {
                if (SetProperty(ref _selectedSearchCategory, value))
                {
                    OnPropertyChanged(nameof(SelectedSearchCategoryTitle));
                    OnPropertyChanged(nameof(SelectedSearchCategoryIcon));
                    OnPropertyChanged(nameof(SelectedSearchCategoryAccent));
                    OnPropertyChanged(nameof(SearchPlaceholder));
                }
            }
        }

        public string SelectedSearchCategoryTitle => _selectedSearchCategory?.ShortTitle ?? "پست‌ها";

        public string SelectedSearchCategoryIcon => _selectedSearchCategory?.IconGlyph ?? KopolArtFaIcons.Newspaper;

        public Color SelectedSearchCategoryAccent
        {
            get
            {
                bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
                return _selectedSearchCategory?.GetAccentColor(isDark) ?? Colors.Transparent;
            }
        }

        public string SearchPlaceholder => _selectedSearchCategory?.Placeholder ?? "جستجو در مقالات و آموزش‌ها...";

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    HasSearchQuery = !string.IsNullOrWhiteSpace(value);
                    TriggerSearchSuggestionsDebounced();
                }
            }
        }

        public bool HasSearchQuery
        {
            get => _hasSearchQuery;
            set => SetProperty(ref _hasSearchQuery, value);
        }

        public ObservableCollection<SearchSuggestionItem> Suggestions { get; } = new();

        public bool IsSearchingSuggestions
        {
            get => _isSearchingSuggestions;
            set => SetProperty(ref _isSearchingSuggestions, value);
        }

        public bool ShowSuggestions
        {
            get => _showSuggestions;
            set
            {
                if (SetProperty(ref _showSuggestions, value))
                {
                    OnPropertyChanged(nameof(ShowRecentSearches));
                }
            }
        }

        public bool HasNoSuggestions
        {
            get => _hasNoSuggestions;
            set => SetProperty(ref _hasNoSuggestions, value);
        }

        public ObservableCollection<RecentSearchChipItem> RecentSearches { get; } = new();

        public bool HasRecentSearches => RecentSearches.Count > 0;

        public bool ShowRecentSearches => HasRecentSearches && !ShowSuggestions;

        public ICommand SelectRecentSearchCommand { get; }

        public ICommand RemoveRecentSearchCommand { get; }

        public ICommand ClearAllRecentSearchesCommand { get; }

        public ICommand SelectSearchCategoryCommand { get; }

        public ICommand ExecuteSearchCommand { get; }

        public ICommand ClearSearchCommand { get; }

        public ICommand OpenSuggestionCommand { get; }

        public ICommand DismissSuggestionsCommand { get; }

        public ICommand OpenCategoryCommand { get; }

        public ICommand OpenBrowserCommand { get; }

        public ICommand OpenPersonalWebsiteCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand OpenHistoryCommand { get; }

        public ICommand OpenDownloadsCommand { get; }

        public ICommand OpenUsageCommand { get; }

        public ICommand OpenBookmarksCommand { get; }

        public ICommand OpenGesturesCommand { get; }

        public ICommand OpenContactCommand { get; }

        public ICommand OpenAboutCommand { get; }

        public ICommand ExitCommand { get; }

        public ICommand OpenUserProfileCommand { get; }

        public ICommand OpenLoginCommand { get; }

        public ICommand OpenRegisterCommand { get; }

        public ICommand DateCardTappedCommand { get; }

        private readonly IRssFeedService? _rssFeedService;
        private readonly ISearchHistoryService _searchHistoryService;

        public DashboardViewModel(

            ISettingsService settingsService,

            IUsageTrackerService usageTrackerService,

            INetworkStatsService networkStatsService,

            UpdateService updateService,

            ICustomAlertService customAlertService,

            IUserService userService,

            IAppVisitTrackerService visitTrackerService,

            IOnlineUsersService onlineUsersService,

            ISearchSuggestionService searchSuggestionService,

            IRssFeedService? rssFeedService = null,
            ISearchHistoryService? searchHistoryService = null)
        {
            _settingsService = settingsService;

            _usageTrackerService = usageTrackerService;

            _networkStatsService = networkStatsService;

            _updateService = updateService;

            _customAlertService = customAlertService;

            _userService = userService;

            _visitTrackerService = visitTrackerService;

            _onlineUsersService = onlineUsersService;

            _searchSuggestionService = searchSuggestionService;

            _rssFeedService = rssFeedService;
            _searchHistoryService = searchHistoryService ?? new SearchHistoryService();

            OpenBrowserCommand = new Command(async () => await NavigateToBrowserAsync(null));

            OpenUserProfileCommand = new Command(async () =>
            {
                if (UserId > 0)
                {
                    await NavigateToBrowserAsync($"{Constants.Urls.UserProfileBase}{UserId}");
                }

            });

            OpenLoginCommand = new Command(async () => await NavigateToBrowserAsync(Constants.Urls.UserLogin));

            OpenRegisterCommand = new Command(async () => await NavigateToBrowserAsync(Constants.Urls.UserRegister));

            DateCardTappedCommand = new Command(async () =>
            {
                if (IsUserLoggedIn && UserId > 0)
                {
                    await NavigateToBrowserAsync($"{Constants.Urls.UserProfileBase}{UserId}");
                }

                else
                {
                    await NavigateToBrowserAsync(Constants.Urls.UserLogin);
                }

            });

            OpenPersonalWebsiteCommand = new Command(async () =>
            {
                try
                {
                    _ = _visitTrackerService.TrackVisitAsync(Constants.PartialVisitKeys.JosephyBrowse);

                    await Launcher.OpenAsync(new Uri("https://josephy.ir/"));
                }

                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Dashboard] Failed to open external browser: {ex.Message}");
                }

            });

            OpenSettingsCommand = new Command(async () => await Shell.Current.GoToAsync("SettingsPage"));

            OpenHistoryCommand = new Command(async () => await Shell.Current.GoToAsync("HistoryPage"));

            OpenDownloadsCommand = new Command(async () => await Shell.Current.GoToAsync("DownloadHistoryPage"));

            OpenUsageCommand = new Command(async () => await Shell.Current.GoToAsync("UsagePage"));

            OpenBookmarksCommand = new Command(async () => await Shell.Current.GoToAsync("BookmarksPage"));

            OpenGesturesCommand = new Command(async () => await Shell.Current.GoToAsync("GesturesPage"));

            OpenContactCommand = new Command(async () => await Shell.Current.GoToAsync("ContactUsPage"));

            OpenAboutCommand = new Command(async () => await Shell.Current.GoToAsync("AboutPage"));

            ExitCommand = new Command(async () => await ConfirmAndExitAppAsync());

            OpenCategoryCommand = new Command<object>(async param =>
            {
                if (param is DashboardCategoryItem item && !string.IsNullOrWhiteSpace(item.Url))
                {
                    await NavigateToBrowserAsync(item.Url);
                }

                else if (param is string url && !string.IsNullOrWhiteSpace(url))
                {
                    await NavigateToBrowserAsync(url);
                }

            });

            SelectRecentSearchCommand = new Command<object>(async param =>
            {
                var query = (param as RecentSearchChipItem)?.Query ?? param as string;
                if (string.IsNullOrWhiteSpace(query)) return;
                SearchQuery = query;
                await ExecuteSearchAsync();
            });

            RemoveRecentSearchCommand = new Command<object>(param =>
            {
                var query = (param as RecentSearchChipItem)?.Query ?? param as string;
                if (string.IsNullOrWhiteSpace(query)) return;
                RemoveRecentSearch(query);
            });

            ClearAllRecentSearchesCommand = new Command(() =>
            {
                ClearAllRecentSearches();
            });

            LoadRecentSearches();

            SelectSearchCategoryCommand = new Command(async () => await SelectSearchCategoryAsync());

            ExecuteSearchCommand = new Command(async () => await ExecuteSearchAsync());

            ClearSearchCommand = new Command(() =>
            {
                CancelSearchDebounce();
                SearchQuery = string.Empty;
                ShowSuggestions = false;
                Suggestions.Clear();
            });

            OpenSuggestionCommand = new Command<SearchSuggestionItem>(async item => await OpenSuggestionAsync(item));

            DismissSuggestionsCommand = new Command(() =>
            {
                ShowSuggestions = false;
                KeyboardHelper.HideKeyboard();
            });

            InitializeSearchCategories();

            InitializeQuickCategories();

            RefreshDashboardInfo();
        }

        private bool _dashboardEventsAttached;

        public void AttachDashboardEvents()
        {
            if (_dashboardEventsAttached) return;
            _dashboardEventsAttached = true;
            Connectivity.ConnectivityChanged += OnConnectivityChanged;
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
            }
        }

        public void DetachDashboardEvents()
        {
            if (!_dashboardEventsAttached) return;
            _dashboardEventsAttached = false;
            Connectivity.ConnectivityChanged -= OnConnectivityChanged;
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
            }
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess != NetworkAccess.Internet) return;
            _ = LoadOnlineUsersAsync(forceRefresh: true);
            _ = LoadCategoryBadgesAsync(forceRefresh: true);
        }

        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(UpdateQuickCategoriesTheme);
        }

        public void StartCardRotation()
        {
            StopCardRotation();

            PersianTodayDate = PersianFormatHelper.GetPersianTodayDateString();

            IsShowingDateCard = false;

            IsShowingAccountCard = true;

            _rotationCts = new CancellationTokenSource();

            var token = _rotationCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), token);

                        if (token.IsCancellationRequested) break;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            PersianTodayDate = PersianFormatHelper.GetPersianTodayDateString();

                            IsShowingDateCard = !IsShowingDateCard;

                            IsShowingAccountCard = !IsShowingDateCard;

                        });
                    }

                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] Rotation error: {ex.Message}");
                    }
                }

            }, token);
        }

        public void StopCardRotation()
        {
            if (_rotationCts != null)
            {
                try
                {
                    _rotationCts.Cancel();

                    _rotationCts.Dispose();
                }

                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] Rotation cancellation cleanup failed: {ex.Message}");
                }

                _rotationCts = null;
            }

            IsShowingDateCard = false;

            IsShowingAccountCard = true;
        }

        public async Task NavigateShortcutActionAsync(string action)
        {
            switch (action?.ToLowerInvariant())
            {
                case "browse":
                    OpenBrowserCommand.Execute(null);
                    break;
                case "settings":
                    await Shell.Current.GoToAsync("SettingsPage");
                    break;
                case "contact":
                    await Shell.Current.GoToAsync("ContactUsPage");
                    break;
                case "bookmarks":
                    await Shell.Current.GoToAsync("BookmarksPage");
                    break;
                case "downloads":
                    await Shell.Current.GoToAsync("DownloadHistoryPage");
                    break;
                case "history":
                    await Shell.Current.GoToAsync("HistoryPage");
                    break;
                case "updates":
                    await Shell.Current.GoToAsync("UpdatesHistoryPage");
                    break;
                case "about":
                    await Shell.Current.GoToAsync("AboutPage");
                    break;
            }
        }

        public async Task NavigateDeepLinkAsync(string url)
        {
            if (!string.IsNullOrWhiteSpace(url) && Shell.Current != null)
            {
                await Shell.Current.GoToAsync("BrowserPage", new Dictionary<string, object>
                {
                    { "targetUrl", url }
                });
            }
        }

        public void RefreshDashboardInfo()
        {
            var welcome = _usageTrackerService.GetWelcomeInfo();

            Greeting = PersianFormatHelper.GetTimeOfDayGreeting();

            WelcomeSubtitle = !string.IsNullOrWhiteSpace(welcome.Subtitle)

                ? welcome.Subtitle

                : "به برنامه کپل‌آرت خوش آمدید";

            TotalUsageTime = _usageTrackerService.GetFormattedUsageTime();

            TotalDataUsed = _networkStatsService.FormattedTotalBytes;

            PersianTodayDate = PersianFormatHelper.GetPersianTodayDateString();

            RefreshUserInfo();

            // Fetch online users count non-blockingly; the service serves cached data when fresh

            _ = LoadOnlineUsersAsync();

            // Check for updates silently

            _ = CheckUpdateStatusAsync();

            // Load category badges from RSS non-blockingly (throttled)

            _ = LoadCategoryBadgesAsync();

            LoadRecentSearches();

            UpdateQuickCategoriesTheme();
        }

        private DateTime _lastCategoryBadgesFetchUtc = DateTime.MinValue;

        private static readonly TimeSpan CategoryBadgesCacheDuration = TimeSpan.FromMinutes(5);

        public async Task LoadCategoryBadgesAsync(bool forceRefresh = false)
        {
            if (_rssFeedService == null) return;

            if (!forceRefresh && (DateTime.UtcNow - _lastCategoryBadgesFetchUtc) < CategoryBadgesCacheDuration)
            {
                return;
            }

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    // Offline: do not attempt network; category badges stay cleanly 0 / hidden
                    return;
                }

                var counts = await _rssFeedService.GetRecentCategoryCountsAsync();

                _lastCategoryBadgesFetchUtc = DateTime.UtcNow;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var item in QuickCategories)
                    {
                        var key = RssFeedService.MatchCategoryKey(item.Url);

                        if (key != null && counts.TryGetValue(key, out var count))
                        {
                            item.BadgeCount = count;
                        }

                        else
                        {
                            item.BadgeCount = 0;
                        }
                    }

                });
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] LoadCategoryBadgesAsync error: {ex.Message}");
            }
        }

        public async Task<bool> LoadOnlineUsersAsync(bool forceRefresh = false)
        {
            try
            {
                var count = await _onlineUsersService.GetOnlineUsersCountAsync(forceRefresh).ConfigureAwait(false);

                return await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var previousValue = OnlineUsersCount;

                    if (count.HasValue)
                    {
                        var formatted = count.Value.ToPersianDigits();

                        OnlineUsersCount = $"{formatted} نفر";

                        IsOnlineUsersLive = true;
                    }

                    else if (string.IsNullOrEmpty(OnlineUsersCount) || OnlineUsersCount == "--")
                    {
                        OnlineUsersCount = "--";

                        IsOnlineUsersLive = false;
                    }

                    return previousValue != OnlineUsersCount;
                });
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] Online users load error: {ex.Message}");

                return await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (string.IsNullOrEmpty(OnlineUsersCount))
                    {
                        OnlineUsersCount = "--";
                    }

                    IsOnlineUsersLive = false;

                    return false;
                });
            }
        }

        private void RefreshUserInfo()
        {
            try
            {
                var currentUser = _userService.GetCurrentUser();

                if (currentUser != null && currentUser.UserId > 0)
                {
                    UserId = currentUser.UserId;

                    var displayName = !string.IsNullOrWhiteSpace(currentUser.Name)

                        ? currentUser.Name.Trim()

                        : "کاربر";

                    if (displayName.Length > 20)
                    {
                        displayName = displayName.Substring(0, 18) + "...";
                    }

                    UserGreetingText = $"{displayName} عزیز خوش آمدی!";

                    IsUserLoggedIn = true;

                    IsUserNotLoggedIn = false;
                }

                else
                {
                    UserId = 0;

                    UserGreetingText = string.Empty;

                    IsUserLoggedIn = false;

                    IsUserNotLoggedIn = true;
                }
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] RefreshUserInfo error: {ex.Message}");

                UserId = 0;

                UserGreetingText = string.Empty;

                IsUserLoggedIn = false;

                IsUserNotLoggedIn = true;
            }
        }

        private async Task NavigateToBrowserAsync(string? explicitUrl)
        {
            string urlToLoad = explicitUrl ?? string.Empty;

            if (string.IsNullOrWhiteSpace(urlToLoad))
            {
                // 1. If user previously browsed during this app session, return to that last visited page

                if (!string.IsNullOrWhiteSpace(_settingsService.SessionLastVisitedUrl))
                {
                    urlToLoad = _settingsService.SessionLastVisitedUrl;
                }

                // 2. Else if user explicitly configured "Restore last page at startup" in settings

                else if (_settingsService.Settings.RestoreLastPageAtStartup && !string.IsNullOrWhiteSpace(_settingsService.Settings.LastVisitedUrl))
                {
                    urlToLoad = _settingsService.Settings.LastVisitedUrl;
                }

                // 3. First time app open: start at root page "https://kopolart.ir/"

                else
                {
                    urlToLoad = Constants.Urls.Home;
                }
            }

            var parameters = new Dictionary<string, object>
            {
                { "targetUrl", urlToLoad }

            };

            await Shell.Current.GoToAsync("BrowserPage", parameters);
        }

        private async Task CheckUpdateStatusAsync()
        {
            try
            {
                var updateInfo = await _updateService.FetchUpdateInfoAsync();

                if (updateInfo != null)
                {
                    var newReleases = _updateService.GetNewReleases(updateInfo, AppInfo.Current.VersionString);

                    if (newReleases.Count > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            HasUpdateBadge = true;

                            NewVersionBadge = "آپدیت جدید";

                        });

                        return;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HasUpdateBadge = false;

                    NewVersionBadge = string.Empty;

                });
            }

            catch
            {
                // Silent
            }
        }

        public async Task CheckAndShowPostUpdateMessageAsync()
        {
            if (_settingsService.HasJustUpdated)
            {
                await _customAlertService.ShowAlertAsync(

                    "به‌روزرسانی موفق کپل‌آرت 🎉",

                    $"کپل‌آرت به نسخه {AppInfo.Current.VersionString} به‌روزرسانی شد.\nتمام تنظیمات، سوابق و اطلاعات شما حفظ شده است.",

                    "متشکرم",

                    AlertType.Success);
            }
        }

        private async Task ConfirmAndExitAppAsync()
        {
            bool confirm = await _customAlertService.ShowConfirmAsync(

                "خروج از برنامه",

                "آیا مطمئن هستید که می‌خواهید از برنامه خارج شوید؟",

                "بله، خروج",

                "انصراف",

                AlertType.Question,

                isDestructive: true);

            if (!confirm) return;

#if ANDROID

            _usageTrackerService.EndCurrentSession();

            if (_settingsService.Settings.AutoClearCacheOnExit)
            {
                await _settingsService.ClearCacheAsync();
            }

            if (Platform.CurrentActivity != null)
            {
                Platform.CurrentActivity.FinishAffinity();
            }

            else
            {
                Application.Current?.Quit();
            }

#else

            Application.Current?.Quit();

#endif
        }

        private void InitializeQuickCategories()
        {
            QuickCategories.Clear();

            CategorySlides.Clear();

            var colorsItem = new DashboardCategoryItem
            {
                Title = "رنگ‌ها",

                Url = Constants.Urls.Colors,

                IconGlyph = KopolArtFaIcons.Sun,

                AccentColorLight = CategoryPalette.Colors.AccentLight,

                AccentColorDark = CategoryPalette.Colors.AccentDark,

                BadgeBgLight = CategoryPalette.Colors.BadgeBackgroundLight,

                BadgeBgDark = CategoryPalette.Colors.BadgeBackgroundDark,

                BorderLight = CategoryPalette.Colors.BorderLight,

                BorderDark = CategoryPalette.Colors.BorderDark

            };

            colorsItem.OpenCommand = new Command(async () => await NavigateToBrowserAsync(colorsItem.Url));

            var palettesItem = new DashboardCategoryItem
            {
                Title = "پالت‌ها",

                Url = Constants.Urls.ColorPalettes,

                IconGlyph = KopolArtFaIcons.Palette,

                AccentColorLight = CategoryPalette.Palettes.AccentLight,

                AccentColorDark = CategoryPalette.Palettes.AccentDark,

                BadgeBgLight = CategoryPalette.Palettes.BadgeBackgroundLight,

                BadgeBgDark = CategoryPalette.Palettes.BadgeBackgroundDark,

                BorderLight = CategoryPalette.Palettes.BorderLight,

                BorderDark = CategoryPalette.Palettes.BorderDark

            };

            palettesItem.OpenCommand = new Command(async () => await NavigateToBrowserAsync(palettesItem.Url));

            var postsItem = new DashboardCategoryItem
            {
                Title = "نگاره‌ها",

                Url = Constants.Urls.Posts,

                IconGlyph = KopolArtFaIcons.Newspaper,

                AccentColorLight = CategoryPalette.Posts.AccentLight,

                AccentColorDark = CategoryPalette.Posts.AccentDark,

                BadgeBgLight = CategoryPalette.Posts.BadgeBackgroundLight,

                BadgeBgDark = CategoryPalette.Posts.BadgeBackgroundDark,

                BorderLight = CategoryPalette.Posts.BorderLight,

                BorderDark = CategoryPalette.Posts.BorderDark

            };

            postsItem.OpenCommand = new Command(async () => await NavigateToBrowserAsync(postsItem.Url));

            var wallpapersItem = new DashboardCategoryItem
            {
                Title = "والپیپرها",

                Url = Constants.Urls.Wallpapers,

                IconGlyph = KopolArtFaIcons.Image,

                AccentColorLight = CategoryPalette.Wallpapers.AccentLight,

                AccentColorDark = CategoryPalette.Wallpapers.AccentDark,

                BadgeBgLight = CategoryPalette.Wallpapers.BadgeBackgroundLight,

                BadgeBgDark = CategoryPalette.Wallpapers.BadgeBackgroundDark,

                BorderLight = CategoryPalette.Wallpapers.BorderLight,

                BorderDark = CategoryPalette.Wallpapers.BorderDark

            };

            wallpapersItem.OpenCommand = new Command(async () => await NavigateToBrowserAsync(wallpapersItem.Url));

            var toolsItem = new DashboardCategoryItem
            {
                Title = "ابزارها",

                Url = Constants.Urls.Tools,

                IconGlyph = KopolArtFaIcons.Setting,

                AccentColorLight = CategoryPalette.Tools.AccentLight,

                AccentColorDark = CategoryPalette.Tools.AccentDark,

                BadgeBgLight = CategoryPalette.Tools.BadgeBackgroundLight,

                BadgeBgDark = CategoryPalette.Tools.BadgeBackgroundDark,

                BorderLight = CategoryPalette.Tools.BorderLight,

                BorderDark = CategoryPalette.Tools.BorderDark

            };

            toolsItem.OpenCommand = new Command(async () => await NavigateToBrowserAsync(toolsItem.Url));

            QuickCategories.Add(palettesItem);

            QuickCategories.Add(wallpapersItem);

            QuickCategories.Add(postsItem);

            QuickCategories.Add(colorsItem);

            QuickCategories.Add(toolsItem);

            // 2 Slides: Slide 1 with 3 items (1: پالت‌ها, 2: والپیپرها, 3: نگاره‌ها), Slide 2 with 2 items (1: رنگ‌ها, 2: ابزارها)

            CategorySlides.Add(new DashboardCategorySlide
            {
                Items = [palettesItem, wallpapersItem, postsItem]

            });

            CategorySlides.Add(new DashboardCategorySlide
            {
                Items = [colorsItem, toolsItem]

            });

            UpdateQuickCategoriesTheme();
        }

        private void UpdateQuickCategoriesTheme()
        {
            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            foreach (var category in QuickCategories)
            {
                category.ApplyTheme(isDark);
            }

            OnPropertyChanged(nameof(SelectedSearchCategoryAccent));
        }
    }
}
