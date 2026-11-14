using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public class OfflinePagesViewModel : FilterableListViewModelBase
    {
        private readonly IOfflinePageService _offlinePageService;
        private readonly ICustomAlertService _customAlertService;
        private OfflinePageSortOrder _currentSort = OfflinePageSortOrder.Newest;
        private string _totalOfflinePagesCount = "...";

        public ObservableCollection<OfflinePageDisplayItem> Pages { get; } = [];

        public string TotalOfflinePagesCount
        {
            get => _totalOfflinePagesCount;
            set => SetProperty(ref _totalOfflinePagesCount, value);
        }

        public OfflinePageSortOrder CurrentSort
        {
            get => _currentSort;
            set
            {
                if (SetProperty(ref _currentSort, value))
                {
                    OnPropertyChanged(nameof(CurrentSortTitle));
                    _ = LoadPagesAsync();
                }
            }
        }

        public string CurrentSortTitle => CurrentSort switch
        {
            OfflinePageSortOrder.Oldest => "قدیمی‌ترین",
            OfflinePageSortOrder.TitleAsc => "نام (الف - ی)",
            OfflinePageSortOrder.SizeDesc => "بیشترین حجم",
            OfflinePageSortOrder.SizeAsc => "کمترین حجم",
            _ => "جدیدترین"
        };

        public ICommand OpenPageCommand { get; }
        public ICommand DeletePageCommand { get; }
        public ICommand SharePageCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand ChangeSortCommand { get; }

        public OfflinePagesViewModel(IOfflinePageService offlinePageService, ICustomAlertService customAlertService)
        {
            _offlinePageService = offlinePageService;
            _customAlertService = customAlertService;

            OpenPageCommand = new Command<OfflinePageDisplayItem>(async item => await OpenPageAsync(item));
            DeletePageCommand = new Command<OfflinePageDisplayItem>(async item => await DeletePageAsync(item));
            SharePageCommand = new Command<OfflinePageDisplayItem>(async item => await SharePageAsync(item));
            ClearAllCommand = new Command(async () => await ClearAllPagesAsync());
            ChangeSortCommand = new Command(async () => await ShowSortMenuAsync());
        }

        protected override async Task OnFilterOrSearchChangedAsync()
        {
            await LoadPagesAsync();
        }

        private async Task ShowSortMenuAsync()
        {
            var choice = await _customAlertService.ShowOptionsAsync(
                "مرتب‌سازی صفحات آفلاین",
                "ترتیب نمایش صفحات ذخیره‌شده را انتخاب نمایید:",
                [
                    "جدیدترین‌ها (پیش‌فرض)",
                    "قدیمی‌ترین‌ها",
                    "بیشترین حجم",
                    "کمترین حجم",
                    "نام صفحه (الف تا ی)"
                ],
                "انصراف",
                AlertType.Info);

            if (choice == "جدیدترین‌ها (پیش‌فرض)")
                CurrentSort = OfflinePageSortOrder.Newest;
            else if (choice == "قدیمی‌ترین‌ها")
                CurrentSort = OfflinePageSortOrder.Oldest;
            else if (choice == "بیشترین حجم")
                CurrentSort = OfflinePageSortOrder.SizeDesc;
            else if (choice == "کمترین حجم")
                CurrentSort = OfflinePageSortOrder.SizeAsc;
            else if (choice == "نام صفحه (الف تا ی)")
                CurrentSort = OfflinePageSortOrder.TitleAsc;
        }

        public async Task LoadPagesAsync()
        {
            try
            {
                var countTask = _offlinePageService.GetOfflinePagesCountAsync();
                var itemsTask = _offlinePageService.GetOfflinePagesAsync(SearchText, SelectedCategory, CurrentSort);
                await Task.WhenAll(countTask, itemsTask);

                var totalCount = await countTask;
                var items = await itemsTask;
                var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TotalOfflinePagesCount = $"{totalCount.ToPersianDigits()} صفحه";
                    Pages.Clear();
                    foreach (var item in items)
                    {
                        var cat = CategoryBadgeHelper.GetCategory(item.OriginalUrl);
                        Pages.Add(new OfflinePageDisplayItem
                        {
                            Item = item,
                            FormattedDate = PersianFormatHelper.FormatPersianDate(item.SavedAtUtc),
                            FormattedSize = PersianFormatHelper.FormatBytes(item.FileSizeBytes),
                            Category = cat,
                            CategoryTextColor = CategoryBadgeHelper.GetBadgeTextColor(cat, isDark),
                            CategoryBgColor = CategoryBadgeHelper.GetBadgeBackgroundColor(cat, isDark),
                            CategoryBorderColor = CategoryBadgeHelper.GetBadgeBorderColor(cat, isDark)
                        });
                    }

                    IsEmpty = Pages.Count == 0;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflinePagesViewModel] Load error: {ex.Message}");
            }
        }

        private async Task OpenPageAsync(OfflinePageDisplayItem? displayItem)
        {
            if (displayItem == null || string.IsNullOrWhiteSpace(displayItem.FilePath))
                return;

            string targetPath = displayItem.FilePath;

            // If this is a legacy .mht file, upgrade it on the fly to self-contained .html so JS and fonts execute
            if (targetPath.EndsWith(".mht", StringComparison.OrdinalIgnoreCase) && File.Exists(targetPath))
            {
                string htmlPath = Path.ChangeExtension(targetPath, ".html");
                if (!File.Exists(htmlPath))
                {
                    bool ok = await Helpers.OfflineHtmlConverter.ConvertMhtmlToHtmlAsync(
                        targetPath,
                        htmlPath,
                        displayItem.Item.OriginalUrl,
                        displayItem.Title);

                    if (ok && File.Exists(htmlPath))
                    {
                        targetPath = htmlPath;
                        displayItem.Item.FilePath = htmlPath;
                        try
                        {
                            displayItem.Item.FileSizeBytes = new FileInfo(htmlPath).Length;
                        }
                        catch { }
                        await _offlinePageService.AddOfflinePageAsync(displayItem.Item);
                    }
                }
                else
                {
                    targetPath = htmlPath;
                }
            }

            if (!File.Exists(targetPath))
            {
                await _customAlertService.ShowAlertAsync(
                    "فایل یافت نشد",
                    "فایل ذخیره‌شده این صفحه در حافظه دستگاه یافت نشد. ممکن است توسط کاربر یا پاک‌کننده‌ها حذف شده باشد.",
                    "متوجه شدم",
                    AlertType.Warning);
                return;
            }

            await NavigationHelper.NavigateToBrowserAsync("file://" + targetPath);
        }

        private async Task DeletePageAsync(OfflinePageDisplayItem? displayItem)
        {
            if (displayItem == null) return;

            bool confirm = await _customAlertService.ShowConfirmAsync(
                "حذف صفحه آفلاین",
                $"آیا از حذف صفحه «{displayItem.Title}» از حافظه مطمئن هستید؟",
                "بله، حذف شود",
                "انصراف",
                AlertType.Question,
                isDestructive: true);

            if (!confirm) return;

            await _offlinePageService.DeleteOfflinePageAsync(displayItem.Item.Id);
            await LoadPagesAsync();
        }

        private async Task SharePageAsync(OfflinePageDisplayItem? displayItem)
        {
            if (displayItem == null) return;

            try
            {
                if (File.Exists(displayItem.FilePath))
                {
                    string contentType = displayItem.FilePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                        ? "text/html"
                        : "multipart/related";

                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = displayItem.Title,
                        File = new ShareFile(displayItem.FilePath, contentType)
                    });
                }
                else if (!string.IsNullOrWhiteSpace(displayItem.OriginalUrl))
                {
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Title = displayItem.Title,
                        Text = displayItem.Title,
                        Uri = displayItem.OriginalUrl
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflinePagesViewModel] Share error: {ex.Message}");
            }
        }

        private async Task ClearAllPagesAsync()
        {
            if (Pages.Count == 0) return;

            bool confirm = await _customAlertService.ShowConfirmAsync(
                "پاک‌سازی تمام صفحات آفلاین",
                "تمام صفحات ذخیره‌شده آفلاین و فایل‌های آرشیو آن‌ها به طور کامل حذف خواهند شد. ادامه می‌دهید؟",
                "بله، حذف همه",
                "انصراف",
                AlertType.Question,
                isDestructive: true);

            if (!confirm) return;

            await _offlinePageService.ClearAllOfflinePagesAsync();
            await LoadPagesAsync();
        }
    }
}
