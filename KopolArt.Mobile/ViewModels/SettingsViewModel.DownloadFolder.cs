using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.ViewModels
{
    public partial class SettingsViewModel
    {
        private void UpdateDownloadFolderState()
        {
            bool isCustom = !string.IsNullOrWhiteSpace(_settingsService.Settings.CustomDownloadFolderUri);
            IsCustomDownloadFolderSet = isCustom;

            if (isCustom)
            {
                string name = _settingsService.Settings.CustomDownloadFolderName ?? string.Empty;
                string friendly = StoragePathHelper.FormatFriendlyPath(name, folderOnly: true);

                if (friendly.Equals(StoragePathHelper.DefaultDownloadFolderDisplay, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(_settingsService.Settings.CustomDownloadFolderUri) &&
                    _downloadHistoryService != null)
                {
                    string resolved = _downloadHistoryService.ResolveDisplayPath(_settingsService.Settings.CustomDownloadFolderUri, null);
                    if (!string.IsNullOrWhiteSpace(resolved))
                    {
                        friendly = resolved;
                    }
                }

                if (!string.IsNullOrWhiteSpace(friendly) && !friendly.Equals(name, StringComparison.Ordinal))
                {
                    _settingsService.Settings.CustomDownloadFolderName = friendly;
                    _ = _settingsService.SaveSettingsAsync(_settingsService.Settings);
                }

                DownloadFolderDisplayName = !string.IsNullOrWhiteSpace(friendly)
                    ? friendly
                    : StoragePathHelper.DefaultDownloadFolderDisplay;
            }
            else
            {
                DownloadFolderDisplayName = StoragePathHelper.DefaultDownloadFolderDisplay;
            }

            _askBeforeDownload = !_settingsService.Settings.DontShowDownloadPromptAgain;
            OnPropertyChanged(nameof(AskBeforeDownload));
        }

        private async Task ChangeDownloadFolderAsync()
        {
            if (_downloadFolderPickerService == null)
            {
                await _customAlertService.ShowAlertAsync(
                    "خطا",
                    "امکان انتخاب پوشه در این دستگاه فراهم نیست.",
                    "تأیید",
                    AlertType.Warning);
                return;
            }

            try
            {
                var picked = await _downloadFolderPickerService.PickFolderAsync();
                if (picked.HasValue && !string.IsNullOrWhiteSpace(picked.Value.UriString))
                {
                    _settingsService.Settings.CustomDownloadFolderUri = picked.Value.UriString;
                    string friendlyName = StoragePathHelper.FormatFriendlyPath(picked.Value.DisplayName, folderOnly: true);
                    _settingsService.Settings.CustomDownloadFolderName = !string.IsNullOrWhiteSpace(friendlyName)
                        ? friendlyName
                        : StoragePathHelper.DefaultDownloadFolderDisplay;

                    await _settingsService.SaveSettingsAsync(_settingsService.Settings);

                    UpdateDownloadFolderState();

                    await _customAlertService.ShowAlertAsync(
                        "مسیر ذخیره تنظیم شد",
                        $"از این پس فایل‌های دانلودی در پوشه «{_settingsService.Settings.CustomDownloadFolderName}» ذخیره می‌شوند.",
                        "تأیید",
                        AlertType.Success);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Change folder error: {ex.Message}");
                await _customAlertService.ShowAlertAsync(
                    "خطا",
                    "هنگام انتخاب پوشه خطایی رخ داد.",
                    "تأیید",
                    AlertType.Error);
            }
        }

        private async Task ResetDownloadFolderAsync()
        {
            bool confirm = await _customAlertService.ShowConfirmAsync(
                "بازنشانی مسیر دانلود",
                "آیا می‌خواهید مسیر ذخیره‌سازی به پوشه پیش‌فرض «دانلودها» سیستم بازگردد؟",
                "بله، بازنشانی شود",
                "انصراف",
                AlertType.Question);

            if (!confirm) return;

            _settingsService.Settings.CustomDownloadFolderUri = null;
            _settingsService.Settings.CustomDownloadFolderName = null;
            await _settingsService.SaveSettingsAsync(_settingsService.Settings);

            UpdateDownloadFolderState();

            await _customAlertService.ShowAlertAsync(
                "بازنشانی انجام شد",
                "مسیر ذخیره فایل‌ها به پوشه پیش‌فرض دانلودهای سیستم تغییر یافت.",
                "تأیید",
                AlertType.Success);
        }

    }
}
