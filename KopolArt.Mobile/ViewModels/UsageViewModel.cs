﻿using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public class UsageViewModel : BaseViewModel
    {
        private readonly IUsageTrackerService _usageTrackerService;
        private readonly INetworkStatsService _networkStatsService;
        private readonly ICustomAlertService _customAlertService;
        private readonly IUsageAnalyticsService _analyticsService;

        // Tab state
        private bool _isSessionsTabSelected = true;

        // Sessions tab state
        private string _totalUsageTime = "...";
        private string _totalDataUsed = "...";
        private string _currentSessionIp = "...";
        private string _searchText = string.Empty;
        private bool _isEmpty;

        // Analytics tab state
        private UsageAnalyticsBarItem? _selectedBar;
        private string _totalDurationText = "...";
        private string _averageDurationText = "...";
        private string _peakDurationText = "-";
        private string _peakDateText = "-";
        private string _totalDataText = "...";
        private string _smartInsightText = string.Empty;
        private bool _isAnalyticsEmpty;
        private bool _hasPeak;
        private bool _isAnalyticsLoading;
        private int _currentPeriodDays = 7;

        public ObservableCollection<UsageDisplayRecord> Sessions { get; } = [];
        public ObservableCollection<UsageAnalyticsPeriodOption> PeriodOptions { get; } = [];
        public ObservableCollection<UsageAnalyticsBarItem> ChartBars { get; } = [];

        // Tab selection properties
        public bool IsSessionsTabSelected
        {
            get => _isSessionsTabSelected;
            set
            {
                if (SetProperty(ref _isSessionsTabSelected, value))
                {
                    OnPropertyChanged(nameof(IsAnalyticsTabSelected));
                }
            }
        }

        public bool IsAnalyticsTabSelected
        {
            get => !_isSessionsTabSelected;
            set => IsSessionsTabSelected = !value;
        }

        // Sessions Tab Properties
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

        public string CurrentSessionIp
        {
            get => _currentSessionIp;
            set => SetProperty(ref _currentSessionIp, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadSessionsAsync();
                }
            }
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                if (SetProperty(ref _isEmpty, value))
                {
                    OnPropertyChanged(nameof(IsNotEmpty));
                }
            }
        }

        public bool IsNotEmpty => !IsEmpty;

        // Analytics Tab Properties
        public UsageAnalyticsBarItem? SelectedBar
        {
            get => _selectedBar;
            set => SetProperty(ref _selectedBar, value);
        }

        public string TotalDurationText
        {
            get => _totalDurationText;
            set => SetProperty(ref _totalDurationText, value);
        }

        public string AverageDurationText
        {
            get => _averageDurationText;
            set => SetProperty(ref _averageDurationText, value);
        }

        public string PeakDurationText
        {
            get => _peakDurationText;
            set => SetProperty(ref _peakDurationText, value);
        }

        public string PeakDateText
        {
            get => _peakDateText;
            set => SetProperty(ref _peakDateText, value);
        }

        public string TotalDataText
        {
            get => _totalDataText;
            set => SetProperty(ref _totalDataText, value);
        }

        public string SmartInsightText
        {
            get => _smartInsightText;
            set => SetProperty(ref _smartInsightText, value);
        }

        public bool IsAnalyticsEmpty
        {
            get => _isAnalyticsEmpty;
            set
            {
                if (SetProperty(ref _isAnalyticsEmpty, value))
                {
                    OnPropertyChanged(nameof(HasAnalyticsData));
                }
            }
        }

        public bool HasAnalyticsData => !_isAnalyticsEmpty;

        public bool HasPeak
        {
            get => _hasPeak;
            set => SetProperty(ref _hasPeak, value);
        }

        public bool IsAnalyticsLoading
        {
            get => _isAnalyticsLoading;
            set => SetProperty(ref _isAnalyticsLoading, value);
        }

        public event Action? RequestScrollToEnd;

        // Commands
        public ICommand SelectSessionsTabCommand { get; }
        public ICommand SelectAnalyticsTabCommand { get; }
        public ICommand ResetStatsCommand { get; }
        public ICommand DeleteSessionCommand { get; }
        public ICommand RemoveIpCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand StartBrowsingCommand { get; }
        public ICommand OpenAnalyticsCommand { get; }
        public ICommand SelectPeriodCommand { get; }
        public ICommand SelectBarCommand { get; }
        public ICommand RefreshAnalyticsCommand { get; }

        public UsageViewModel(
            IUsageTrackerService usageTrackerService,
            INetworkStatsService networkStatsService,
            ICustomAlertService customAlertService,
            IUsageAnalyticsService analyticsService)
        {
            _usageTrackerService = usageTrackerService;
            _networkStatsService = networkStatsService;
            _customAlertService = customAlertService;
            _analyticsService = analyticsService;

            SelectSessionsTabCommand = new Command(() => IsSessionsTabSelected = true);
            SelectAnalyticsTabCommand = new Command(async () =>
            {
                IsSessionsTabSelected = false;
                if (ChartBars.Count == 0)
                {
                    await LoadAnalyticsAsync(_currentPeriodDays);
                }
                else
                {
                    RequestScrollToEnd?.Invoke();
                }
            });

            ResetStatsCommand = new Command(async () => await ResetStatsAsync());
            DeleteSessionCommand = new Command<UsageDisplayRecord>(async record => await DeleteSessionAsync(record));
            RemoveIpCommand = new Command<UsageDisplayRecord>(async record => await RemoveIpAsync(record));
            BackCommand = new Command(async () => await NavigationHelper.GoBackSafelyAsync());
            StartBrowsingCommand = new Command(async () =>
            {
                await NavigationHelper.NavigateToBrowserAsync(Constants.Urls.Home);
            });
            OpenAnalyticsCommand = SelectAnalyticsTabCommand;

            SelectPeriodCommand = new Command<UsageAnalyticsPeriodOption>(async p => await OnSelectPeriodAsync(p));
            SelectBarCommand = new Command<UsageAnalyticsBarItem>(OnSelectBar);
            RefreshAnalyticsCommand = new Command(async () =>
            {
                await LoadAnalyticsAsync(_currentPeriodDays);
                MainThread.BeginInvokeOnMainThread(async () =>
                {
#if ANDROID
                    Android.Widget.Toast.MakeText(Android.App.Application.Context, "اطلاعات نمودار با موفقیت به‌روزرسانی شد", Android.Widget.ToastLength.Short)?.Show();
#else
                    await _customAlertService.ShowAlertAsync("به‌روزرسانی", "اطلاعات نمودار با موفقیت به‌روزرسانی شد.", "تأیید", AlertType.Success);
#endif
                });
            });

            InitializePeriods();
            RefreshSummary();
        }

        private void InitializePeriods()
        {
            PeriodOptions.Clear();
            PeriodOptions.Add(new UsageAnalyticsPeriodOption { DaysCount = 7, Title = "۷ روز اخیر", IsSelected = true });
            PeriodOptions.Add(new UsageAnalyticsPeriodOption { DaysCount = 14, Title = "۱۴ روز", IsSelected = false });
            PeriodOptions.Add(new UsageAnalyticsPeriodOption { DaysCount = 30, Title = "۳۰ روز", IsSelected = false });
            PeriodOptions.Add(new UsageAnalyticsPeriodOption { DaysCount = 365, Title = "۱ سال اخیر", IsSelected = false });
        }

        public void RefreshSummary()
        {
            TotalUsageTime = _usageTrackerService.GetFormattedUsageTime();
            TotalDataUsed = _networkStatsService.FormattedTotalBytes;
            CurrentSessionIp = _usageTrackerService.CurrentSessionIp;
        }

        public async Task LoadSessionsAsync()
        {
            RefreshSummary();
            var sessions = await _usageTrackerService.GetUsageSessionsAsync(SearchText);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Sessions.Clear();
                foreach (var s in sessions)
                {
                    Sessions.Add(new UsageDisplayRecord
                    {
                        SessionId = s.SessionId,
                        FormattedDate = PersianFormatHelper.FormatPersianDate(s.StartTimeUtc),
                        FormattedDuration = PersianFormatHelper.FormatUsageTime(TimeSpan.FromSeconds(s.DurationSeconds)),
                        FormattedData = PersianFormatHelper.FormatBytes(s.DataBytesUsed),
                        IpAddress = string.IsNullOrWhiteSpace(s.IpAddress) ? "آفلاین / بدون اتصال" : s.IpAddress
                    });
                }
                IsEmpty = Sessions.Count == 0;
            });
        }

        private async Task DeleteSessionAsync(UsageDisplayRecord? record)
        {
            if (record == null) return;

            bool confirm = await _customAlertService.ShowConfirmAsync(
                "حذف سابقه",
                $"این سابقه ({record.FormattedDate}) حذف شود؟",
                "حذف",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _usageTrackerService.DeleteSessionAsync(record.SessionId);
            Sessions.Remove(record);
            IsEmpty = Sessions.Count == 0;
            RefreshSummary();
        }

        private async Task RemoveIpAsync(UsageDisplayRecord? record)
        {
            if (record == null) return;

            if (string.IsNullOrWhiteSpace(record.IpAddress) || record.IpAddress == "بدون IP" || record.IpAddress == "آفلاین / بدون اتصال")
            {
                await _customAlertService.ShowAlertAsync(
                    "اطلاع",
                    "آدرس IP برای این سابقه وجود ندارد یا قبلاً حذف شده است.",
                    "متوجه شدم",
                    AlertType.Info);
                return;
            }

            bool confirm = await _customAlertService.ShowConfirmAsync(
                "حذف آدرس IP",
                $"آدرس IP ({record.IpAddress}) از این سابقه حذف شود؟",
                "حذف IP",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _usageTrackerService.ClearSessionIpAsync(record.SessionId);
            record.IpAddress = "بدون IP";
            RefreshSummary();

            await _customAlertService.ShowAlertAsync(
                "موفق",
                "آدرس IP با موفقیت از سابقه حذف شد.",
                "تأیید",
                AlertType.Success);
        }

        private async Task ResetStatsAsync()
        {
            bool confirm = await _customAlertService.ShowConfirmAsync(
                "بازنشانی آمار وب‌گردی",
                "تمام سوابق وب‌گردی، زمان مصرف و ترافیک اینترنت صفر خواهند شد. آیا مطمئن هستید؟",
                "بله، بازنشانی شود",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _usageTrackerService.ResetAllUsageAsync();
            RefreshSummary();
            await LoadSessionsAsync();
            if (ChartBars.Count > 0)
            {
                await LoadAnalyticsAsync(_currentPeriodDays);
            }
            await _customAlertService.ShowAlertAsync("موفق", "آمار با موفقیت بازنشانی شد.", "تأیید", AlertType.Success);
        }

        private async Task OnSelectPeriodAsync(UsageAnalyticsPeriodOption? option)
        {
            if (option == null || option.DaysCount == _currentPeriodDays) return;

            foreach (var opt in PeriodOptions)
            {
                opt.IsSelected = (opt.DaysCount == option.DaysCount);
            }

            _currentPeriodDays = option.DaysCount;
            await LoadAnalyticsAsync(_currentPeriodDays);
        }

        private void OnSelectBar(UsageAnalyticsBarItem? bar)
        {
            if (bar == null) return;

            foreach (var b in ChartBars)
            {
                b.IsSelected = (b == bar);
            }

            SelectedBar = bar;
        }

        public async Task LoadAnalyticsAsync(int daysCount)
        {
            if (IsAnalyticsLoading) return;
            IsAnalyticsLoading = true;

            try
            {
                var summary = await _analyticsService.BuildAnalyticsAsync(daysCount);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ChartBars.Clear();
                    // Chronological LTR chart layout: start of period on left, today on right
                    foreach (var bar in summary.Bars)
                    {
                        ChartBars.Add(bar);
                    }

                    TotalDurationText = summary.FormattedTotalDuration;
                    AverageDurationText = summary.FormattedAverageDuration;
                    TotalDataText = summary.FormattedTotalData;
                    SmartInsightText = summary.SmartInsight;
                    IsAnalyticsEmpty = summary.IsEmpty;

                    if (summary.PeakBar != null && summary.PeakBar.DurationSeconds > 0)
                    {
                        HasPeak = true;
                        PeakDurationText = summary.PeakBar.FormattedDuration;
                        PeakDateText = summary.PeakBar.FullDateString;
                    }
                    else
                    {
                        HasPeak = false;
                        PeakDurationText = "-";
                        PeakDateText = "-";
                    }

                    SelectedBar = summary.Bars.FirstOrDefault(b => b.IsSelected)
                        ?? summary.PeakBar
                        ?? summary.Bars.LastOrDefault();

                    RequestScrollToEnd?.Invoke();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UsageViewModel] Analytics Load error: {ex.Message}");
            }
            finally
            {
                IsAnalyticsLoading = false;
            }
        }
    }
}
