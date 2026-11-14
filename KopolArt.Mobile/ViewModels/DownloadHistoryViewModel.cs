using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Presentation;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public class DownloadHistoryViewModel : FilterableListViewModelBase
    {
        private const int WallpaperProbeConcurrency = 3;

        private readonly IDownloadHistoryService _downloadHistoryService;
        private readonly ICustomAlertService _customAlertService;
        private readonly IDownloadService _downloadService;
        private readonly IWallpaperService _wallpaperService;
        private readonly ConcurrentDictionary<string, bool> _wallpaperEligibility = new(StringComparer.Ordinal);
        private DownloadSortOrder _currentSort = DownloadSortOrder.Newest;
        private string _totalDownloadCount = "...";
        private string _totalDownloadVolume = "...";

        public ObservableCollection<DownloadDateGroup> DownloadGroups { get; } = [];

        public string TotalDownloadCount
        {
            get => _totalDownloadCount;
            set => SetProperty(ref _totalDownloadCount, value);
        }

        public string TotalDownloadVolume
        {
            get => _totalDownloadVolume;
            set => SetProperty(ref _totalDownloadVolume, value);
        }

        public DownloadSortOrder CurrentSort
        {
            get => _currentSort;
            set
            {
                if (SetProperty(ref _currentSort, value))
                {
                    OnPropertyChanged(nameof(CurrentSortTitle));
                    _ = LoadDownloadsAsync();
                }
            }
        }

        public string CurrentSortTitle => CurrentSort switch
        {
            DownloadSortOrder.Oldest => "قدیمی‌ترین",
            DownloadSortOrder.SizeDesc => "بیشترین حجم",
            DownloadSortOrder.SizeAsc => "کمترین حجم",
            DownloadSortOrder.NameAsc => "نام فایل (الف - ی)",
            _ => "جدیدترین"
        };

        public ICommand OpenFileCommand { get; }
        public ICommand RedownloadCommand { get; }
        public ICommand CopyPathCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand ShareItemCommand { get; }
        public ICommand SetWallpaperCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand ChangeSortCommand { get; }

        public DownloadHistoryViewModel(
            IDownloadHistoryService downloadHistoryService,
            ICustomAlertService customAlertService,
            IDownloadService downloadService,
            IWallpaperService wallpaperService)
        {
            _downloadHistoryService = downloadHistoryService;
            _customAlertService = customAlertService;
            _downloadService = downloadService;
            _wallpaperService = wallpaperService;

            OpenFileCommand = new Command<DownloadDisplayItem>(async item => await OpenFileAsync(item));
            RedownloadCommand = new Command<DownloadDisplayItem>(async item => await RedownloadAsync(item));
            CopyPathCommand = new Command<DownloadDisplayItem>(async item => await CopyPathAsync(item));
            DeleteItemCommand = new Command<DownloadDisplayItem>(async item => await DeleteItemAsync(item));
            ShareItemCommand = new Command<DownloadDisplayItem>(async item => await ShareItemAsync(item));
            SetWallpaperCommand = new Command<DownloadDisplayItem>(async item => await OpenWallpaperPreviewAsync(item));
            ClearAllCommand = new Command(async () => await ClearAllAsync());
            ChangeSortCommand = new Command(async () => await ShowSortMenuAsync());

            InitCategoryFilters(CategoryBadgeHelper.DownloadFilterCategories);
        }

        protected override async Task OnFilterOrSearchChangedAsync()
        {
            await LoadDownloadsAsync();
        }

        private async Task ShowSortMenuAsync()
        {
            var choice = await _customAlertService.ShowOptionsAsync(
                "مرتب‌سازی دانلودها",
                "ترتیب نمایش دانلودها را انتخاب نمایید:",
                [
                    "جدیدترین‌ها (پیش‌فرض)",
                    "قدیمی‌ترین‌ها",
                    "بیشترین حجم",
                    "کمترین حجم",
                    "نام فایل (الف تا ی)"
                ],
                "انصراف",
                AlertType.Info);

            if (choice == "جدیدترین‌ها (پیش‌فرض)")
                CurrentSort = DownloadSortOrder.Newest;
            else if (choice == "قدیمی‌ترین‌ها")
                CurrentSort = DownloadSortOrder.Oldest;
            else if (choice == "بیشترین حجم")
                CurrentSort = DownloadSortOrder.SizeDesc;
            else if (choice == "کمترین حجم")
                CurrentSort = DownloadSortOrder.SizeAsc;
            else if (choice == "نام فایل (الف تا ی)")
                CurrentSort = DownloadSortOrder.NameAsc;
        }

        public async Task LoadDownloadsAsync()
        {
            var statsTask = _downloadHistoryService.GetDownloadStatsAsync();
            var itemsTask = _downloadHistoryService.GetDownloadsAsync(SearchText, CurrentSort);
            await Task.WhenAll(statsTask, itemsTask);

            var (totalCount, totalBytes) = await statsTask;
            var items = await itemsTask;
            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalDownloadCount = $"{totalCount.ToPersianDigits()} فایل";
                TotalDownloadVolume = PersianFormatHelper.FormatBytes(totalBytes);
                DownloadGroups.Clear();

                IEnumerable<DownloadItem> query = items;
                if (IsCategoryFilterActive)
                {
                    query = query.Where(x =>
                    {
                        return string.Equals(CategoryBadgeHelper.GetDownloadCategory(x.DownloadUrl, x.FileName), SelectedCategory, StringComparison.OrdinalIgnoreCase);
                    });
                }

                var displayItems = new List<DownloadDisplayItem>();
                foreach (var item in query)
                {
                    bool exists = _downloadHistoryService.DoesLocalFileExist(item);
                    var cat = CategoryBadgeHelper.GetDownloadCategory(item.DownloadUrl, item.FileName);
                    bool isWallpaperCandidate = WallpaperEligibility.IsWallpaperCandidate(
                        item.DownloadUrl,
                        item.FileName,
                        item.MimeType,
                        exists);

                    displayItems.Add(new DownloadDisplayItem
                    {
                        Id = item.Id,
                        FileName = item.FileName,
                        DownloadUrl = item.DownloadUrl,
                        MimeType = item.MimeType,
                        LocalFilePath = item.LocalFilePath,
                        FormattedSize = PersianFormatHelper.FormatBytes(item.FileSizeBytes),
                        FormattedDate = PersianFormatHelper.FormatPersianDate(item.DownloadedAtUtc),
                        DownloadedAtUtc = item.DownloadedAtUtc,
                        FileExistsLocally = exists,
                        StatusText = exists ? "موجود در دستگاه" : "فایل در دستگاه نیست",
                        StatusColor = exists ? AppColors.StatusGreen : AppColors.TextSubtleLight,
                        CategoryName = cat,
                        BadgeTextColor = CategoryBadgeHelper.GetBadgeTextColor(cat, isDark),
                        BadgeBackgroundColor = CategoryBadgeHelper.GetBadgeBackgroundColor(cat, isDark),
                        BadgeBorderColor = CategoryBadgeHelper.GetBadgeBorderColor(cat, isDark),
                        StorageLocationDescription = _downloadHistoryService.ResolveDisplayPath(item.LocalFilePath, item.FileName),
                        IsWallpaperCandidate = isWallpaperCandidate,
                        CanSetAsWallpaper = isWallpaperCandidate
                            && _wallpaperEligibility.TryGetValue(item.Id, out bool canSetWallpaper)
                            && canSetWallpaper
                    });
                }

                AddDateGroups(
                    DownloadGroups,
                    displayItems,
                    x => x.DownloadedAtUtc,
                    ascending: CurrentSort == DownloadSortOrder.Oldest,
                    (title, icon, groupItems) => new DownloadDateGroup(title, icon, groupItems));

                IsEmpty = DownloadGroups.Count == 0;

                _ = ProbeWallpaperCandidatesAsync(displayItems);
            });
        }

        /// <summary>
        /// Reads image bounds for wallpaper candidates off the UI thread (max 3 at a time)
        /// and reveals the "set as wallpaper" action only for usable portrait images.
        /// </summary>
        private async Task ProbeWallpaperCandidatesAsync(IReadOnlyList<DownloadDisplayItem> items)
        {
            var pending = items
                .Where(item => item.IsWallpaperCandidate && _wallpaperEligibility.TryAdd(item.Id, false))
                .ToList();

            if (pending.Count == 0)
                return;

            using var gate = new SemaphoreSlim(WallpaperProbeConcurrency);
            var probes = pending.Select(async item =>
            {
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    var info = await _wallpaperService.InspectAsync(item.LocalFilePath).ConfigureAwait(false);
                    bool eligible = WallpaperEligibility.IsUsablePortrait(info);
                    _wallpaperEligibility[item.Id] = eligible;

                    if (eligible)
                        MainThread.BeginInvokeOnMainThread(() => ApplyWallpaperEligibility(item.Id));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DownloadHistoryViewModel] Wallpaper probe failed: {ex.Message}");
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(probes);
        }

        private void ApplyWallpaperEligibility(string itemId)
        {
            foreach (var group in DownloadGroups)
            {
                foreach (var item in group)
                {
                    if (item.Id == itemId)
                        item.CanSetAsWallpaper = true;
                }
            }
        }

        private async Task OpenWallpaperPreviewAsync(DownloadDisplayItem? item)
        {
            if (item == null || !item.CanSetAsWallpaper)
                return;

            if (!item.FileExistsLocally)
            {
                await _customAlertService.ShowAlertAsync(
                    "فایل یافت نشد",
                    "فایل اصلی از حافظه دستگاه حذف شده است. می‌توانید آن را دوباره دانلود کنید.",
                    "تأیید",
                    AlertType.Warning);
                return;
            }

            if (!_wallpaperService.IsSupported)
            {
                await _customAlertService.ShowAlertAsync(
                    "تنظیم پس‌زمینه",
                    "این دستگاه از تنظیم پس‌زمینه تصویر پشتیبانی نمی‌کند.",
                    "تأیید",
                    AlertType.Warning);
                return;
            }

            try
            {
                await Shell.Current.GoToAsync("WallpaperPreviewPage", new Dictionary<string, object>
                {
                    [WallpaperPreviewViewModel.ItemQueryKey] = item
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DownloadHistoryViewModel] Wallpaper preview navigation failed: {ex.Message}");
                await _customAlertService.ShowAlertAsync(
                    "خطا",
                    "باز کردن پیش‌نمایش پس‌زمینه ممکن نشد. لطفاً دوباره تلاش کنید.",
                    "تأیید",
                    AlertType.Error);
            }
        }

        private async Task OpenFileAsync(DownloadDisplayItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.LocalFilePath)) return;

            if (!item.FileExistsLocally)
            {
                await _customAlertService.ShowAlertAsync(
                    "فایل یافت نشد",
                    "فایل اصلی از حافظه دستگاه حذف شده است. می‌توانید آن را دوباره دانلود کنید.",
                    "تأیید",
                    AlertType.Warning);
                return;
            }

            try
            {
                bool success = await _downloadHistoryService.OpenDownloadedFileAsync(item.LocalFilePath, item.MimeType);
                if (!success)
                {
                    await _customAlertService.ShowAlertAsync(
                        "خطا",
                        "برنامه مناسبی برای باز کردن این فایل در دستگاه یافت نشد.",
                        "تأیید",
                        AlertType.Warning);
                }
            }
            catch (Exception ex)
            {
                await _customAlertService.ShowAlertAsync(
                    "خطا",
                    $"خطا در باز کردن فایل: {ex.Message}",
                    "تأیید",
                    AlertType.Error);
            }
        }

        private async Task RedownloadAsync(DownloadDisplayItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.DownloadUrl)) return;

            if (item.DownloadUrl.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
            {
                await _customAlertService.ShowAlertAsync(
                    "توجه",
                    "امکان دریافت مجدد فایل‌های موقت مرورگر (Blob) وجود ندارد.",
                    "تأیید",
                    AlertType.Info);
                return;
            }

            bool confirm = await _customAlertService.ShowConfirmAsync(
                "دانلود مجدد",
                $"فایل «{item.FileName}» دوباره دانلود شود؟",
                "دانلود",
                "انصراف",
                AlertType.Question);

            if (!confirm) return;

            _ = Task.Run(async () =>
            {
                bool success = await _downloadService.DownloadFileAsync(item.DownloadUrl, item.FileName, item.MimeType);
                if (success)
                {
                    await LoadDownloadsAsync();
                }
            });
        }

        private async Task DeleteItemAsync(DownloadDisplayItem? item)
        {
            if (item == null) return;

            bool confirm = await _customAlertService.ShowConfirmAsync(
                "حذف از سوابق دانلود",
                $"«{item.FileName}» از سوابق دانلود حذف شود؟ (فایل در حافظه دستگاه باقی می‌ماند)",
                "حذف",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _downloadHistoryService.DeleteItemAsync(item.Id);
            RemoveFromDateGroups(DownloadGroups, item);
            IsEmpty = DownloadGroups.Count == 0;
        }

        private async Task ClearAllAsync()
        {
            if (DownloadGroups.Count == 0) return;

            bool confirm = await _customAlertService.ShowConfirmAsync(
                "پاک‌سازی سوابق دانلود",
                "آیا مطمئن هستید تمام سوابق دانلود پاک شوند؟ (فایل‌ها در حافظه دستگاه باقی می‌مانند)",
                "بله، پاک شود",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _downloadHistoryService.ClearAllAsync();
            DownloadGroups.Clear();
            IsEmpty = true;
        }

        private async Task CopyPathAsync(DownloadDisplayItem? item)
        {
            if (item == null) return;

            try
            {
                string textToCopy = !string.IsNullOrWhiteSpace(item.StorageLocationDescription)
                    ? item.StorageLocationDescription
                    : _downloadHistoryService.ResolveDisplayPath(item.LocalFilePath, item.FileName);

                if (string.IsNullOrWhiteSpace(textToCopy))
                {
                    textToCopy = item.LocalFilePath ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(textToCopy)) return;

                await Clipboard.Default.SetTextAsync(textToCopy);
                await _customAlertService.ShowAlertAsync(
                    "کپی مسیر پوشه",
                    $"مسیر فایل در کلیپ‌بورد کپی شد:\n{textToCopy}",
                    "تأیید",
                    AlertType.Info);
            }
            catch (Exception ex)
            {
                await _customAlertService.ShowAlertAsync(
                    "خطا",
                    $"خطا در کپی مسیر: {ex.Message}",
                    "تأیید",
                    AlertType.Error);
            }
        }

        private async Task ShareItemAsync(DownloadDisplayItem? item)
        {
            if (item == null) return;

            try
            {
                if (item.FileExistsLocally && !string.IsNullOrWhiteSpace(item.LocalFilePath) && File.Exists(item.LocalFilePath))
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = item.FileName,
                        File = new ShareFile(item.LocalFilePath, string.IsNullOrWhiteSpace(item.MimeType) ? "application/octet-stream" : item.MimeType)
                    });
                }
                else if (!string.IsNullOrWhiteSpace(item.DownloadUrl))
                {
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Title = item.FileName,
                        Uri = item.DownloadUrl
                    });
                }
                else
                {
                    await _customAlertService.ShowAlertAsync(
                        "اشتراک‌گذاری",
                        "اطلاعاتی برای اشتراک‌گذاری این فایل یافت نشد.",
                        "تأیید",
                        AlertType.Warning);
                }
            }
            catch (Exception ex)
            {
                await _customAlertService.ShowAlertAsync(
                    "خطا",
                    $"خطا در اشتراک‌گذاری: {ex.Message}",
                    "تأیید",
                    AlertType.Error);
            }
        }
    }
}
