using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.ViewModels
{
    public partial class SettingsViewModel
    {
        private async Task ClearCacheAsync()
        {
            bool confirm = await _customAlertService.ShowConfirmAsync(
                "پاک‌سازی حافظه کش",
                "آیا می‌خواهید حافظه موقت (کش) مرورگر پاک شود؟",
                "بله، پاک شود",
                "انصراف",
                AlertType.Warning);

            if (!confirm) return;

            await _settingsService.ClearCacheAsync();
            RefreshStorageMetrics();
            await _customAlertService.ShowAlertAsync("موفق", "حافظه موقت با موفقیت پاک شد.", "تأیید", AlertType.Success);
        }

        private async Task ClearAppDataAsync()
        {
            bool confirm = await _customAlertService.ShowConfirmAsync(
                "پاک‌سازی داده‌های مرورگر",
                "با این کار تمام حافظه موقت، اطلاعات ورود و داده‌های مرورگر پاک می‌شوند. مایل به ادامه هستید؟",
                "بله، پاک شود",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _settingsService.ClearAppDataAsync();
            RefreshStorageMetrics();
            await _customAlertService.ShowAlertAsync("موفق", "داده‌های مرورگر با موفقیت پاک شد.", "تأیید", AlertType.Success);
        }

        private async Task ClearCookiesAsync()
        {
            bool confirm = await _customAlertService.ShowConfirmAsync(
                "حذف کوکی‌ها",
                "با حذف کوکی‌ها، از حساب کاربری خود در سایت خارج می‌شوید. آیا ادامه می‌دهید؟",
                "بله، حذف شود",
                "انصراف",
                AlertType.Warning);

            if (!confirm) return;

            await _settingsService.ClearCookiesAsync();
            RefreshStorageMetrics();
            await _customAlertService.ShowAlertAsync("موفق", "کوکی‌ها با موفقیت حذف شدند.", "تأیید", AlertType.Success);
        }

        private async Task ResetUsageStatsAsync()
        {
            bool confirm = await _customAlertService.ShowConfirmAsync(
                "بازنشانی آمار استفاده",
                "تمام سوابق استفاده، مدت زمان و حجم اینترنت مصرفی پاک خواهند شد. این عملیات غیرقابل بازگشت است!",
                "بله، بازنشانی شود",
                "انصراف",
                AlertType.Warning,
                isDestructive: true);

            if (!confirm) return;

            await _usageTrackerService.ResetAllUsageAsync();
            await _customAlertService.ShowAlertAsync("موفق", "تمامی سوابق و آمار استفاده بازنشانی شد.", "تأیید", AlertType.Success);
        }

        private async Task ResetDefaultsAsync()
        {
            bool confirm = await _customAlertService.ShowConfirmAsync(
                "بازگردانی تنظیمات",
                "آیا می‌خواهید تمام تنظیمات برنامه به حالت اولیه بازگردد؟",
                "بله، بازگردانی شود",
                "انصراف",
                AlertType.Question);

            if (!confirm) return;

            await _settingsService.ResetToDefaultsAsync();
            ShowLoadingOnNavigation = _settingsService.Settings.ShowLoadingOnNavigation;
            OpenDashboardAtStartup = _settingsService.Settings.OpenDashboardAtStartup;
            RestoreLastPageAtStartup = _settingsService.Settings.RestoreLastPageAtStartup;
            AutoClearCacheOnExit = _settingsService.Settings.AutoClearCacheOnExit;
            NotifyNewContent = _settingsService.Settings.NotifyNewContent;
            SelectedTheme = _settingsService.Settings.AppThemeMode;
            SelectedWidgetOpacity = _settingsService.Settings.WidgetOpacity;
            UpdateDownloadFolderState();

            await _customAlertService.ShowAlertAsync("موفق", "تنظیمات به حالت اولیه بازگشت.", "تأیید", AlertType.Success);
        }

    }
}
