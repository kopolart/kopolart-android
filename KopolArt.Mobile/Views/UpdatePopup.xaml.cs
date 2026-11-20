using KopolArt.Mobile.Services;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Helpers;

namespace KopolArt.Mobile.Views
{
    public partial class UpdatePopup : ContentView
    {
        private UpdateService? _updateService;
        private UpdateInfo? _updateInfo;
        private CancellationTokenSource? _downloadCts;

        public UpdatePopup()
        {
            try
            {
                InitializeComponent();
                SizeChanged += OnPopupSizeChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdatePopup] InitializeComponent Error: {ex}");
                // Never swallow this: a half-initialized view would fail later
                // with much harder-to-diagnose NullReferenceExceptions.
                throw;
            }
        }

        private void OnPopupSizeChanged(object? sender, EventArgs e)
        {
            if (Height > 0)
            {
                bool isLandscape = Width > Height;
                UpdateCard.MaximumHeightRequest = isLandscape
                    ? Math.Max(200, Height - 32)
                    : Math.Min(480, Height - 48);
            }
        }

        public void Show(UpdateService updateService, UpdateInfo updateInfo, List<UpdateRelease> newReleases)
        {
            _updateService = updateService;
            _updateInfo = updateInfo;

            ReleaseNotesContainer.Children.Clear();

            foreach (var release in newReleases)
            {
                string versionHeader = $"نسخه {release.Version}".ToPersianDigits();
                if (!string.IsNullOrWhiteSpace(release.Date))
                {
                    string pDate = release.PersianDate;
                    if (!string.IsNullOrWhiteSpace(pDate))
                    {
                        versionHeader = $"{versionHeader} - {pDate}";
                    }
                }

                var versionLabel = new Label
                {
                    Text = versionHeader,
                    FontFamily = Constants.Fonts.UiFontBlackAlias,
                    FontSize = 15,
                    HorizontalOptions = LayoutOptions.Start,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                versionLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.BrandPrimaryText, AppColors.BrandPrimaryDark);

                string notesText = string.IsNullOrWhiteSpace(release.ReleaseNotes) ? "- تغییرات جزئی" : release.ReleaseNotes;
                notesText = notesText.Replace("\\n", "\n");

                var notesLabel = new Label
                {
                    Text = notesText,
                    FontFamily = Constants.Fonts.UiFontBoldAlias,
                    FontSize = 13,
                    LineHeight = 1.35,
                    HorizontalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Start,
                    Margin = new Thickness(0, 0, 0, 12)
                };
                notesLabel.SetAppThemeColor(Label.TextColorProperty, AppColors.PopupSubtitleLight, AppColors.PopupSubtitleDark);

                ReleaseNotesContainer.Children.Add(versionLabel);
                ReleaseNotesContainer.Children.Add(notesLabel);
            }

            ProgressSection.IsVisible = false;
            ButtonsSection.IsVisible = true;
            DownloadProgressBar.Progress = 0;

            // Store builds cannot install APKs, so the action reads as a store hand-off.
            UpdateButton.Text = updateService.IsStoreManagedUpdate ? "به‌روزرسانی از بازار" : "به‌روزرسانی";

            IsVisible = true;
            Opacity = 0;
            _ = this.FadeToAsync(1, 350, Easing.CubicOut);
        }

        public void Hide()
        {
            CancelDownload();
            this.FadeToAsync(0, 250, Easing.CubicIn).ContinueWith(t => MainThread.BeginInvokeOnMainThread(() => IsVisible = false));
        }

        private void CancelDownload()
        {
            try
            {
                if (_downloadCts != null)
                {
                    _downloadCts.Cancel();
                    _downloadCts.Dispose();
                }
            }
            catch { }
            finally
            {
                _downloadCts = null;
            }
        }

        private void OnRemindLaterClicked(object sender, EventArgs e)
        {
            _updateService?.MarkRemindedLater();
            Hide();
        }

        private void OnCancelDownloadClicked(object sender, EventArgs e)
        {
            // Cancel only; the download's OperationCanceledException handler
            // resets the UI and CancelDownload() disposes the CTS.
            try
            {
                _downloadCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Download already finished/cleaned up - nothing to cancel
            }
        }

        private async void OnUpdateClicked(object sender, EventArgs e)
        {
            if (_updateService == null || _updateInfo == null) return;

            // Store-managed builds (Cafe Bazaar) never download or install APKs: Android 8.0+
            // would require REQUEST_INSTALL_PACKAGES, which the store rejects. Hand off instead.
            if (_updateService.IsStoreManagedUpdate)
            {
                bool storeOpened = _updateService.OpenStoreForUpdate();
                Hide();

                if (!storeOpened)
                {
                    await GetAlertService().ShowAlertAsync(
                        "بازار در دسترس نیست",
                        "برای دریافت نسخه جدید، کپل‌آرت را از بازار به‌روزرسانی کنید.",
                        "متوجه شدم",
                        AlertType.Warning);
                }
                return;
            }

            // Check for install permission on Android before downloading to avoid wasting data
            if (!_updateService.CanInstallApkDirectly())
            {
                _updateService.RequestInstallPermission();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await GetAlertService().ShowAlertAsync(
                        "مجوز نصب برنامه",
                        "لطفاً مجوز نصب برنامه‌های ناشناخته را برای کپل‌آرت فعال کنید و سپس مجدداً روی بروزرسانی ضربه بزنید.",
                        "متوجه شدم",
                        AlertType.Warning);
                });
                return;
            }

            ButtonsSection.IsVisible = false;
            ProgressSection.IsVisible = true;
            DownloadProgressBar.Progress = 0;
            ProgressLabel.Text = "در حال شروع دانلود... ۰٪".ToPersianDigits();

            _downloadCts = new CancellationTokenSource();

            try
            {
                var apkPath = await _updateService.DownloadApkAsync(
                    _updateInfo.Url,
                    (progress, bytesRead, totalBytes) => MainThread.BeginInvokeOnMainThread(() => UpdateProgress(progress, bytesRead, totalBytes)),
                    _downloadCts.Token);

                CancelDownload();
                Hide();
                _updateService.InstallApk(apkPath);
            }
            catch (OperationCanceledException)
            {
                ResetUi();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdatePopup] Download Error: {ex}");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    ResetUi();
                    await GetAlertService().ShowAlertAsync(
                        "خطا در دانلود",
                        "دانلود با خطا مواجه شد. لطفاً اتصال اینترنت خود را بررسی کرده و مجدداً تلاش کنید.",
                        "تأیید",
                        AlertType.Error);
                });
            }
        }

        private void UpdateProgress(double progress, long bytesRead, long totalBytes)
        {
            if (progress < 0)
            {
                DownloadProgressBar.Progress = 0;
                if (bytesRead > 0)
                {
                    double mb = bytesRead / (1024.0 * 1024.0);
                    ProgressLabel.Text = $"در حال دانلود... {mb:F1} مگابایت دریافت شد".ToPersianDigits();
                }
                else
                {
                    ProgressLabel.Text = "در حال دانلود...";
                }
            }
            else
            {
                DownloadProgressBar.Progress = Math.Clamp(progress, 0.0, 1.0);
                int percent = (int)Math.Round(progress * 100);
                if (totalBytes > 0)
                {
                    double curMb = bytesRead / (1024.0 * 1024.0);
                    double totalMb = totalBytes / (1024.0 * 1024.0);
                    ProgressLabel.Text = $"در حال دانلود... {curMb:F1} از {totalMb:F1} مگابایت ({percent}٪)".ToPersianDigits();
                }
                else
                {
                    ProgressLabel.Text = $"در حال دانلود... {percent}٪".ToPersianDigits();
                }
            }
        }

        private void UpdateProgress(double progress)
        {
            UpdateProgress(progress, 0, -1);
        }

        private void ResetUi()
        {
            ProgressSection.IsVisible = false;
            ButtonsSection.IsVisible = true;
            DownloadProgressBar.Progress = 0;
            CancelDownload();
        }

        private static ICustomAlertService GetAlertService()
        {
            return IPlatformApplication.Current?.Services.GetService<ICustomAlertService>()
                   ?? Application.Current?.Handler?.MauiContext?.Services.GetService<ICustomAlertService>()
                   ?? new CustomAlertService();
        }
    }
}
