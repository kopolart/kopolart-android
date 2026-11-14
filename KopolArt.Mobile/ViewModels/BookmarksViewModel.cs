using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public class BookmarksViewModel : FilterableListViewModelBase
    {
        private readonly IBookmarkService _bookmarkService;
        private readonly ICustomAlertService _customAlertService;
        private BookmarkSortOrder _currentSort = BookmarkSortOrder.Newest;
        private string _totalBookmarksCount = "...";

        public ObservableCollection<BookmarkDateGroup> BookmarkGroups { get; } = [];

        public string TotalBookmarksCount
        {
            get => _totalBookmarksCount;
            set => SetProperty(ref _totalBookmarksCount, value);
        }

        public BookmarkSortOrder CurrentSort
        {
            get => _currentSort;
            set
            {
                if (SetProperty(ref _currentSort, value))
                {
                    OnPropertyChanged(nameof(CurrentSortTitle));
                    _ = LoadBookmarksAsync();
                }
            }
        }

        public string CurrentSortTitle => CurrentSort switch
        {
            BookmarkSortOrder.Oldest => "قدیمی‌ترین",
            BookmarkSortOrder.TitleAsc => "نام (الف - ی)",
            BookmarkSortOrder.TitleDesc => "نام (ی - الف)",
            _ => "جدیدترین"
        };

        public ICommand OpenBookmarkCommand { get; }
        public ICommand DeleteBookmarkCommand { get; }
        public ICommand ShareBookmarkCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand ChangeSortCommand { get; }
        public ICommand OpenOfflinePagesCommand { get; }

        public BookmarksViewModel(IBookmarkService bookmarkService, ICustomAlertService customAlertService)
        {
            _bookmarkService = bookmarkService;
            _customAlertService = customAlertService;

            OpenBookmarkCommand = new Command<BookmarkDisplayItem>(async item => await OpenBookmarkAsync(item));
            DeleteBookmarkCommand = new Command<BookmarkDisplayItem>(async item => await DeleteBookmarkAsync(item));
            ShareBookmarkCommand = new Command<BookmarkDisplayItem>(async item => await ShareBookmarkAsync(item));
            ClearAllCommand = new Command(async () => await ClearAllBookmarksAsync());
            ChangeSortCommand = new Command(async () => await ShowSortMenuAsync());
            OpenOfflinePagesCommand = new Command(async () => await Shell.Current.GoToAsync("OfflinePagesPage"));
        }

        protected override async Task OnFilterOrSearchChangedAsync()
        {
            await LoadBookmarksAsync();
        }

        private async Task ShowSortMenuAsync()
        {
            var options = new[]
            {
                "جدیدترین‌ها (پیش‌فرض)",
                "قدیمی‌ترین‌ها",
                "نام (الف تا ی)",
                "نام (ی تا الف)"
            };

            var selected = await _customAlertService.ShowOptionsAsync(
                "مرتب‌سازی نشان‌شده‌ها",
                "ترتیب نمایش نشان‌شده‌ها را انتخاب نمایید:",
                options);

            switch (selected)
            {
                case "جدیدترین‌ها (پیش‌فرض)":
                    CurrentSort = BookmarkSortOrder.Newest;
                    break;
                case "قدیمی‌ترین‌ها":
                    CurrentSort = BookmarkSortOrder.Oldest;
                    break;
                case "نام (الف تا ی)":
                    CurrentSort = BookmarkSortOrder.TitleAsc;
                    break;
                case "نام (ی تا الف)":
                    CurrentSort = BookmarkSortOrder.TitleDesc;
                    break;
            }
        }

        public async Task LoadBookmarksAsync()
        {
            var countTask = _bookmarkService.GetBookmarksCountAsync();
            var itemsTask = _bookmarkService.GetBookmarksAsync(SearchText, SelectedCategory, CurrentSort);
            await Task.WhenAll(countTask, itemsTask);

            var totalCount = await countTask;
            var items = await itemsTask;
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalBookmarksCount = $"{totalCount.ToPersianDigits()} صفحه";
                BookmarkGroups.Clear();

                var displayItems = new List<BookmarkDisplayItem>();
                foreach (var item in items)
                {
                    var cat = CategoryBadgeHelper.GetCategory(item.Url);
                    displayItems.Add(new BookmarkDisplayItem
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Url = item.Url,
                        CategoryName = cat,
                        BadgeTextColor = CategoryBadgeHelper.GetBadgeTextColor(cat, isDark),
                        BadgeBackgroundColor = CategoryBadgeHelper.GetBadgeBackgroundColor(cat, isDark),
                        BadgeBorderColor = CategoryBadgeHelper.GetBadgeBorderColor(cat, isDark),
                        RelativeTime = PersianFormatHelper.FormatRelativeDate(item.SavedAtUtc),
                        FullPersianDate = PersianFormatHelper.FormatPersianDate(item.SavedAtUtc),
                        SavedAtUtc = item.SavedAtUtc
                    });
                }

                AddDateGroups(
                    BookmarkGroups,
                    displayItems,
                    x => x.SavedAtUtc,
                    ascending: CurrentSort == BookmarkSortOrder.Oldest,
                    (title, icon, groupItems) => new BookmarkDateGroup(title, icon, groupItems));

                IsEmpty = BookmarkGroups.Count == 0;
            });
        }

        private async Task OpenBookmarkAsync(BookmarkDisplayItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Url))
                return;

            await NavigationHelper.NavigateToBrowserAsync(item.Url);
        }

        private async Task DeleteBookmarkAsync(BookmarkDisplayItem? item)
        {
            if (item == null) return;

            var confirm = await _customAlertService.ShowConfirmAsync(
                "حذف نشان‌شده",
                $"صفحه «{item.Title}» از نشان‌شده‌ها حذف شود؟",
                "حذف",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _bookmarkService.DeleteItemAsync(item.Id);
            RemoveFromDateGroups(BookmarkGroups, item);
            IsEmpty = BookmarkGroups.Count == 0;
        }

        private async Task ShareBookmarkAsync(BookmarkDisplayItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Url))
                return;

            try
            {
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Uri = item.Url,
                    Title = item.Title
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookmarksViewModel] Share error: {ex.Message}");
            }
        }

        private async Task ClearAllBookmarksAsync()
        {
            if (BookmarkGroups.Count == 0) return;

            var confirm = await _customAlertService.ShowConfirmAsync(
                "پاک‌سازی همه نشان‌شده‌ها",
                "آیا مطمئن هستید که تمام نشان‌شده‌ها پاک شوند؟",
                "بله، پاک شود",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _bookmarkService.ClearAllAsync();
            BookmarkGroups.Clear();
            IsEmpty = true;
        }
    }

}
