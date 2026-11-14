using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Presentation;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public class HistoryViewModel : FilterableListViewModelBase
    {
        private readonly IHistoryService _historyService;
        private readonly IBookmarkService _bookmarkService;
        private readonly ICustomAlertService _customAlertService;
        private HistorySortOrder _currentSort = HistorySortOrder.Newest;
        private string _totalPagesCount = "...";

        public ObservableCollection<HistoryDateGroup> HistoryGroups { get; } = [];

        public string TotalPagesCount
        {
            get => _totalPagesCount;
            set => SetProperty(ref _totalPagesCount, value);
        }

        public HistorySortOrder CurrentSort
        {
            get => _currentSort;
            set
            {
                if (SetProperty(ref _currentSort, value))
                {
                    OnPropertyChanged(nameof(CurrentSortTitle));
                    _ = LoadHistoryAsync();
                }
            }
        }

        public string CurrentSortTitle => CurrentSort switch
        {
            HistorySortOrder.Oldest => "قدیمی‌ترین",
            HistorySortOrder.TitleAsc => "نام صفحه (الف - ی)",
            _ => "جدیدترین"
        };

        public ICommand DeleteItemCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand OpenItemCommand { get; }
        public ICommand ChangeSortCommand { get; }

        public HistoryViewModel(
            IHistoryService historyService,
            IBookmarkService bookmarkService,
            ICustomAlertService customAlertService)
        {
            _historyService = historyService;
            _bookmarkService = bookmarkService;
            _customAlertService = customAlertService;

            DeleteItemCommand = new Command<HistoryDisplayItem>(async item => await DeleteItemAsync(item));
            ToggleFavoriteCommand = new Command<HistoryDisplayItem>(async item => await ToggleFavoriteAsync(item));
            ClearAllCommand = new Command(async () => await ClearAllHistoryAsync());
            OpenItemCommand = new Command<HistoryDisplayItem>(async item => await OpenItemAsync(item));
            ChangeSortCommand = new Command(async () => await ShowSortMenuAsync());
        }

        protected override async Task OnFilterOrSearchChangedAsync()
        {
            await LoadHistoryAsync();
        }

        private async Task ShowSortMenuAsync()
        {
            var choice = await _customAlertService.ShowOptionsAsync(
                "مرتب‌سازی تاریخچه",
                "ترتیب نمایش تاریخچه مرور را انتخاب نمایید:",
                [
                    "جدیدترین‌ها (پیش‌فرض)",
                    "قدیمی‌ترین‌ها",
                    "نام صفحه (الف تا ی)"
                ],
                "انصراف",
                AlertType.Info);

            if (choice == "جدیدترین‌ها (پیش‌فرض)")
                CurrentSort = HistorySortOrder.Newest;
            else if (choice == "قدیمی‌ترین‌ها")
                CurrentSort = HistorySortOrder.Oldest;
            else if (choice == "نام صفحه (الف تا ی)")
                CurrentSort = HistorySortOrder.TitleAsc;
        }

        public async Task LoadHistoryAsync()
        {
            try
            {
                var totalCount = await _historyService.GetTotalVisitsCountAsync();
                var items = await _historyService.GetHistoryAsync(SearchText, CurrentSort);
                var bookmarks = await _bookmarkService.GetBookmarksAsync();
                var bookmarkedUrls = bookmarks?
                    .Where(b => !string.IsNullOrWhiteSpace(b?.Url))
                    .Select(b => b.Url)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

                bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        TotalPagesCount = $"{totalCount.ToPersianDigits()} صفحه";
                        HistoryGroups.Clear();

                        IEnumerable<HistoryItem> query = items ?? [];
                        if (IsCategoryFilterActive)
                        {
                            query = query.Where(x => string.Equals(CategoryBadgeHelper.GetCategory(x.Url), SelectedCategory, StringComparison.OrdinalIgnoreCase));
                        }

                        var displayItems = new List<HistoryDisplayItem>();
                        foreach (var item in query)
                        {
                            if (item == null) continue;
                            var cat = CategoryBadgeHelper.GetCategory(item.Url);
                            displayItems.Add(new HistoryDisplayItem
                            {
                                Id = item.Id ?? string.Empty,
                                Title = string.IsNullOrWhiteSpace(item.Title) ? (item.Url ?? "بدون عنوان") : item.Title,
                                Url = item.Url ?? string.Empty,
                                CategoryName = cat,
                                BadgeTextColor = CategoryBadgeHelper.GetBadgeTextColor(cat, isDark),
                                BadgeBackgroundColor = CategoryBadgeHelper.GetBadgeBackgroundColor(cat, isDark),
                                BadgeBorderColor = CategoryBadgeHelper.GetBadgeBorderColor(cat, isDark),
                                RelativeTime = PersianFormatHelper.FormatRelativeDate(item.VisitedAtUtc),
                                FullPersianDate = PersianFormatHelper.FormatPersianDate(item.VisitedAtUtc),
                                VisitedAtUtc = item.VisitedAtUtc,
                                IsBookmarked = !string.IsNullOrWhiteSpace(item.Url) && bookmarkedUrls.Contains(item.Url)
                            });
                        }

                        AddDateGroups(
                            HistoryGroups,
                            displayItems,
                            x => x.VisitedAtUtc,
                            ascending: CurrentSort == HistorySortOrder.Oldest,
                            (title, icon, groupItems) => new HistoryDateGroup(title, icon, groupItems));

                        IsEmpty = HistoryGroups.Count == 0;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] Error populating HistoryGroups: {ex}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] LoadHistoryAsync error: {ex}");
            }
        }

        private async Task ToggleFavoriteAsync(HistoryDisplayItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Url)) return;

            bool isNowBookmarked = await _bookmarkService.ToggleBookmarkAsync(item.Url, item.Title);
            item.IsBookmarked = isNowBookmarked;

            if (isNowBookmarked)
            {
                await _customAlertService.ShowAlertAsync(
                    "نشان‌گذاری صفحه",
                    $"صفحه «{item.Title}» به نشان‌شده‌ها اضافه شد.",
                    "متوجه شدم",
                    AlertType.Success);
            }
            else
            {
                await _customAlertService.ShowAlertAsync(
                    "حذف از نشان‌شده‌ها",
                    $"صفحه «{item.Title}» از نشان‌شده‌ها حذف شد.",
                    "متوجه شدم",
                    AlertType.Info);
            }
        }

        private async Task DeleteItemAsync(HistoryDisplayItem? item)
        {
            if (item == null) return;

            try
            {
                bool confirm = await _customAlertService.ShowConfirmAsync(
                    "حذف از تاریخچه",
                    $"«{item.Title}» از تاریخچه حذف شود؟",
                    "حذف",
                    "انصراف",
                    AlertType.Warning,
                    isDestructive: true);

                if (!confirm) return;

                await _historyService.DeleteItemAsync(item.Id);
                RemoveFromDateGroups(HistoryGroups, item);
                IsEmpty = HistoryGroups.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryViewModel] DeleteItemAsync error: {ex}");
            }
        }

        private async Task ClearAllHistoryAsync()
        {
            if (HistoryGroups.Count == 0) return;

            bool confirm = await _customAlertService.ShowConfirmAsync(
                "پاک‌سازی تاریخچه",
                "آیا مطمئن هستید تمام صفحات بازدیدشده پاک شوند؟",
                "بله، پاک شود",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _historyService.ClearHistoryAsync();
            HistoryGroups.Clear();
            IsEmpty = true;
        }

        private async Task OpenItemAsync(HistoryDisplayItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Url)) return;

            await NavigationHelper.NavigateToBrowserAsync(item.Url);
        }
    }
}
