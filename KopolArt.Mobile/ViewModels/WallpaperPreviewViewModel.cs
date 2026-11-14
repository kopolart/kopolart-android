using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Presentation;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    /// <summary>
    /// Drives the wallpaper preview sheet: prepares a screen-aspect preview, tracks
    /// pinch/pan in the phone frame and converts the visible region into a normalized
    /// crop that the platform service can apply without decoding the full image again.
    /// </summary>
    public class WallpaperPreviewViewModel : BaseViewModel
    {
        public const string ItemQueryKey = "wallpaperItem";

        private const double MinimumScale = 1.0;
        private const double MaximumScale = 4.0;
        private const double MinimumFrameAspect = 0.25;

        private readonly IWallpaperService _wallpaperService;
        private readonly ICustomAlertService _customAlertService;

        private DownloadDisplayItem? _item;
        private WallpaperPreview? _preview;
        private WallpaperTarget _selectedTarget = WallpaperTarget.Both;

        private double _scale = 1.0;
        private double _translationX;
        private double _translationY;
        private double _pinchStartScale = 1.0;
        private double _pinchStartTranslationX;
        private double _pinchStartTranslationY;
        private double _panStartTranslationX;
        private double _panStartTranslationY;
        private double _viewportWidth;
        private double _viewportHeight;
        private double _hostWidth;
        private double _hostHeight;
        private double _frameWidth;
        private double _frameHeight;
        private double _frameAspect = 2.0;
        private bool _isLoading;
        private bool _isApplying;
        private bool _hasError;
        private string _errorMessage = string.Empty;
        private string _previewImagePath = string.Empty;
        private string _fileSummary = string.Empty;
        private string _resolutionSummary = string.Empty;

        public ObservableCollection<WallpaperTargetOption> TargetOptions { get; } = [];

        public string PreviewImagePath
        {
            get => _previewImagePath;
            private set => SetProperty(ref _previewImagePath, value);
        }

        public string FileSummary
        {
            get => _fileSummary;
            private set => SetProperty(ref _fileSummary, value);
        }

        public string ResolutionSummary
        {
            get => _resolutionSummary;
            private set => SetProperty(ref _resolutionSummary, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public double FrameWidth
        {
            get => _frameWidth;
            private set => SetProperty(ref _frameWidth, value);
        }

        public double FrameHeight
        {
            get => _frameHeight;
            private set => SetProperty(ref _frameHeight, value);
        }

        public double ImageScale => _scale;
        public double ImageTranslationX => _translationX;
        public double ImageTranslationY => _translationY;

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsReady));
                    OnPropertyChanged(nameof(IsApplyEnabled));
                }
            }
        }

        public bool IsApplying
        {
            get => _isApplying;
            private set
            {
                if (SetProperty(ref _isApplying, value))
                {
                    OnPropertyChanged(nameof(IsApplyEnabled));
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                if (SetProperty(ref _hasError, value))
                {
                    OnPropertyChanged(nameof(IsReady));
                    OnPropertyChanged(nameof(IsApplyEnabled));
                }
            }
        }

        public bool IsReady => !IsLoading && !HasError && _preview != null;
        public bool IsApplyEnabled => IsReady && !IsApplying;

        public ICommand SelectTargetCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand CloseCommand { get; }

        public WallpaperPreviewViewModel(IWallpaperService wallpaperService, ICustomAlertService customAlertService)
        {
            _wallpaperService = wallpaperService;
            _customAlertService = customAlertService;

            TargetOptions.Add(new WallpaperTargetOption { Target = WallpaperTarget.HomeScreen, Title = "صفحه اصلی" });
            TargetOptions.Add(new WallpaperTargetOption { Target = WallpaperTarget.LockScreen, Title = "صفحه قفل" });
            TargetOptions.Add(new WallpaperTargetOption { Target = WallpaperTarget.Both, Title = "هر دو", IsSelected = true });

            SelectTargetCommand = new Command<WallpaperTargetOption>(SelectTarget);
            ApplyCommand = new Command(async () => await ApplyAsync());
            CloseCommand = new Command(async () => await CloseAsync());
        }

        public async Task InitializeAsync(DownloadDisplayItem item)
        {
            if (item == null)
                return;

            _item = item;
            FileSummary = item.FileName;
            HasError = false;
            IsLoading = true;
            OnPropertyChanged(nameof(IsReady));

            try
            {
                var preview = await _wallpaperService.CreatePreviewAsync(item.LocalFilePath);
                if (preview == null)
                {
                    SetError("پیش‌نمایش این تصویر آماده نشد. ممکن است فایل خراب باشد یا از حافظه دستگاه حذف شده باشد.");
                    return;
                }

                _preview = preview;
                _frameAspect = preview.FrameAspect;
                PreviewImagePath = preview.PreviewFilePath;
                ResolutionSummary = $"{preview.SourceWidth.ToPersianDigits()} × {preview.SourceHeight.ToPersianDigits()} پیکسل";
                UpdateFrameSize();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperPreviewViewModel] Initialize failed: {ex.Message}");
                SetError("آماده‌سازی پیش‌نمایش ممکن نشد. لطفاً دوباره تلاش کنید.");
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(IsReady));
                OnPropertyChanged(nameof(IsApplyEnabled));
            }
        }

        public void SetViewportHost(double width, double height)
        {
            if (width <= 0 || height <= 0)
                return;

            if (Math.Abs(width - _hostWidth) < 0.5 && Math.Abs(height - _hostHeight) < 0.5)
                return;

            _hostWidth = width;
            _hostHeight = height;
            UpdateFrameSize();
        }

        public void BeginPinch()
        {
            _pinchStartScale = _scale;
            _pinchStartTranslationX = _translationX;
            _pinchStartTranslationY = _translationY;
        }

        public void UpdatePinch(double relativeScale, Point origin)
        {
            if (_viewportWidth <= 0 || _viewportHeight <= 0 || _pinchStartScale <= 0)
                return;

            double newScale = Math.Clamp(_pinchStartScale * relativeScale, MinimumScale, MaximumScale);
            double factor = newScale / _pinchStartScale;

            double originX = (origin.X * _viewportWidth) - (_viewportWidth / 2) - _pinchStartTranslationX;
            double originY = (origin.Y * _viewportHeight) - (_viewportHeight / 2) - _pinchStartTranslationY;

            _scale = newScale;
            _translationX = _pinchStartTranslationX - (originX * (factor - 1));
            _translationY = _pinchStartTranslationY - (originY * (factor - 1));

            ClampTranslation();
            RaiseTransformChanged();
        }

        public void BeginPan()
        {
            _panStartTranslationX = _translationX;
            _panStartTranslationY = _translationY;
        }

        public void UpdatePan(double totalX, double totalY)
        {
            if (_viewportWidth <= 0 || _viewportHeight <= 0)
                return;

            _translationX = _panStartTranslationX + totalX;
            _translationY = _panStartTranslationY + totalY;

            ClampTranslation();
            RaiseTransformChanged();
        }

        public void ResetView()
        {
            _scale = MinimumScale;
            _translationX = 0;
            _translationY = 0;
            RaiseTransformChanged();
        }

        public void Release()
        {
            if (_preview == null)
                return;

            _wallpaperService.DeletePreviewFile(_preview.PreviewFilePath);
            _preview = null;
        }

        private void SelectTarget(WallpaperTargetOption? option)
        {
            if (option == null)
                return;

            _selectedTarget = option.Target;
            foreach (var targetOption in TargetOptions)
            {
                targetOption.IsSelected = targetOption == option;
            }
        }

        private async Task ApplyAsync()
        {
            if (_item == null || _preview == null || IsApplying)
                return;

            IsApplying = true;
            try
            {
                var result = await _wallpaperService.SetAsync(_item.LocalFilePath, CalculateCrop(), _selectedTarget);
                await ShowResultAsync(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperPreviewViewModel] Apply failed: {ex.Message}");
                await _customAlertService.ShowAlertAsync(
                    "خطا",
                    "تنظیم پس‌زمینه انجام نشد. لطفاً دوباره تلاش کنید.",
                    "تأیید",
                    AlertType.Error);
            }
            finally
            {
                IsApplying = false;
            }
        }

        private async Task ShowResultAsync(WallpaperSetResult result)
        {
            switch (result.Status)
            {
                case WallpaperSetStatus.Success:
                    TryHapticFeedback();
                    await _customAlertService.ShowAlertAsync(
                        "تنظیم پس‌زمینه",
                        "پس‌زمینه گوشی با موفقیت تنظیم شد.",
                        "عالی",
                        AlertType.Success);
                    await CloseAsync();
                    break;

                case WallpaperSetStatus.HomeScreenOnly:
                    await _customAlertService.ShowAlertAsync(
                        "تنظیم پس‌زمینه",
                        "پس‌زمینه صفحه اصلی تنظیم شد، اما این دستگاه اجازه تغییر پس‌زمینه صفحه قفل را نداد.",
                        "تأیید",
                        AlertType.Warning);
                    await CloseAsync();
                    break;

                case WallpaperSetStatus.NotSupported:
                    await ShowFailureAsync("این دستگاه از تنظیم پس‌زمینه تصویر پشتیبانی نمی‌کند.");
                    break;

                case WallpaperSetStatus.NotAllowed:
                    await ShowFailureAsync("تنظیم پس‌زمینه توسط سیستم یا مدیریت دستگاه محدود شده است.");
                    break;

                case WallpaperSetStatus.FileMissing:
                    await ShowFailureAsync("فایل تصویر در حافظه دستگاه پیدا نشد. لطفاً آن را دوباره دانلود کنید.");
                    break;

                case WallpaperSetStatus.DecodeFailed:
                    await ShowFailureAsync("خواندن تصویر ممکن نشد. ممکن است فایل خراب باشد.");
                    break;

                default:
                    await ShowFailureAsync("تنظیم پس‌زمینه انجام نشد. لطفاً دوباره تلاش کنید.");
                    break;
            }
        }

        private Task ShowFailureAsync(string message) =>
            _customAlertService.ShowAlertAsync("تنظیم پس‌زمینه", message, "تأیید", AlertType.Warning);

        private async Task CloseAsync()
        {
            Release();

            try
            {
                if (Shell.Current is { } shell && shell.Navigation.ModalStack.Count > 0)
                {
                    await shell.Navigation.PopModalAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WallpaperPreviewViewModel] PopModal failed: {ex.Message}");
            }

            await NavigationHelper.GoBackSafelyAsync();
        }

        private WallpaperCrop CalculateCrop()
        {
            var baseCrop = _preview!.BaseCrop;
            if (_viewportWidth <= 0 || _viewportHeight <= 0)
                return baseCrop;

            double scale = Math.Max(MinimumScale, _scale);

            double cropWidth = Math.Clamp(baseCrop.Width / scale, 0.01, 1.0);
            double cropHeight = Math.Clamp(baseCrop.Height / scale, 0.01, 1.0);

            double half = 0.5 / scale;
            double centerX = 0.5 - (_translationX / (_viewportWidth * scale));
            double centerY = 0.5 - (_translationY / (_viewportHeight * scale));

            double x = baseCrop.X + ((centerX - half) * baseCrop.Width);
            double y = baseCrop.Y + ((centerY - half) * baseCrop.Height);

            x = Math.Clamp(x, 0.0, Math.Max(0.0, 1.0 - cropWidth));
            y = Math.Clamp(y, 0.0, Math.Max(0.0, 1.0 - cropHeight));

            return new WallpaperCrop(x, y, cropWidth, cropHeight);
        }

        private void UpdateFrameSize()
        {
            if (_hostWidth <= 0 || _hostHeight <= 0)
                return;

            double aspect = Math.Max(MinimumFrameAspect, _frameAspect);
            double availableWidth = Math.Max(1, _hostWidth - 24);
            double availableHeight = Math.Max(1, _hostHeight - 16);
            double frameWidth = Math.Min(availableWidth, availableHeight / aspect);

            FrameWidth = frameWidth;
            FrameHeight = frameWidth * aspect;

            _viewportWidth = FrameWidth;
            _viewportHeight = FrameHeight;

            ResetView();
        }

        private void ClampTranslation()
        {
            double maxX = Math.Max(0, _viewportWidth * (_scale - 1) / 2);
            double maxY = Math.Max(0, _viewportHeight * (_scale - 1) / 2);

            _translationX = Math.Clamp(_translationX, -maxX, maxX);
            _translationY = Math.Clamp(_translationY, -maxY, maxY);
        }

        private void RaiseTransformChanged()
        {
            OnPropertyChanged(nameof(ImageScale));
            OnPropertyChanged(nameof(ImageTranslationX));
            OnPropertyChanged(nameof(ImageTranslationY));
        }

        private void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
            OnPropertyChanged(nameof(IsReady));
            OnPropertyChanged(nameof(IsApplyEnabled));
        }

        private static void TryHapticFeedback()
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
                // Haptics are best-effort; unsupported devices ignore the call.
            }
        }
    }
}
