using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.ViewModels
{
    public partial class SettingsViewModel
    {
        private async Task PinWidgetAsync()
        {
            if (_homeScreenService == null || !_homeScreenService.IsPinWidgetSupported())
            {
                await _customAlertService.ShowAlertAsync(
                    "ویجت کپل‌آرت",
                    "لانچر دستگاه شما از افزودن خودکار ویجت از داخل برنامه پشتیبانی نمی‌کند. لطفاً در صفحه اصلی گوشی انگشت خود را نگه داشته و از بخش Widgets، ویجت کپل‌آرت را اضافه کنید.",
                    "متوجه شدم",
                    AlertType.Info);
                return;
            }

            var opacityOptions = new List<AlertOptionItem>
            {
                new("۱۰۰٪ (مات)", returnValue: "100"),
                new("۷۵٪ (ملایم)", returnValue: "75"),
                new("۵۰٪ (متوسط)", returnValue: "50"),
                new("۰٪ (شیشه‌ای)", returnValue: "0")
            };

            var choice = await _customAlertService.ShowGridOptionsAsync(
                "تنظیم شفافیت ویجت",
                "میزان شفافیت پس‌زمینه ویجت را برای نمایش روی صفحه اصلی گوشی انتخاب کنید:",
                opacityOptions,
                "انصراف",
                AlertType.Question,
                columns: 2);

            if (string.IsNullOrWhiteSpace(choice) || choice == "انصراف")
            {
                return; // User cancelled
            }

            if (int.TryParse(choice, out int selectedOpacity))
            {
                SelectedWidgetOpacity = selectedOpacity;
            }

            var success = await _homeScreenService.RequestPinWidgetAsync();
            if (!success)
            {
                await _customAlertService.ShowAlertAsync(
                    "ویجت کپل‌آرت",
                    "امکان ارسال درخواست افزودن ویجت فراهم نشد. لطفاً از طریق منوی ویجت‌های صفحه اصلی گوشی اقدام کنید.",
                    "باشه",
                    AlertType.Warning);
            }
        }

        private async Task PinShortcutAsync()
        {
            if (_homeScreenService == null || !_homeScreenService.IsPinShortcutSupported())
            {
                await _customAlertService.ShowAlertAsync(
                    "میانبر کپل‌آرت",
                    "لانچر دستگاه شما از افزودن میانبر پشتیبانی نمی‌کند.",
                    "متوجه شدم",
                    AlertType.Info);
                return;
            }

            var success = await _homeScreenService.RequestPinShortcutAsync("shortcut_kopolart_pinned", "کپل‌آرت", "browse");
            if (!success)
            {
                await _customAlertService.ShowAlertAsync(
                    "میانبر کپل‌آرت",
                    "امکان ارسال درخواست افزودن میانبر فراهم نشد.",
                    "باشه",
                    AlertType.Warning);
            }
        }

        private async Task CheckAndRequestNotificationPermissionAsync()
        {
            await EnsureNotificationPermissionAsync();
        }

        /// <summary>
        /// Makes sure the app may post notifications and explains how to enable them otherwise.
        /// </summary>
        private async Task<bool> EnsureNotificationPermissionAsync()
        {
            if (_notificationPermissionService == null)
                return true;

            try
            {
                if (await _notificationPermissionService.IsNotificationPermissionGrantedAsync())
                    return true;

                if (await _notificationPermissionService.RequestNotificationPermissionAsync())
                    return true;

                await _customAlertService.ShowAlertAsync(
                    "دسترسی اعلان‌ها",
                    "برای دریافت اعلان‌های مطالب جدید، لطفاً مجوز اعلان برنامه را در تنظیمات گوشی فعال کنید.",
                    "متوجه شدم",
                    AlertType.Info);

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Notification permission error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Hidden diagnostics action (5 taps on the version label): shows a sample digest notification
        /// so the layout can be verified without waiting for a real post or the 3 day throttle.
        /// </summary>
        private async Task SendTestPostNotificationAsync()
        {
            if (_newContentNotifier == null)
            {
                await _customAlertService.ShowAlertAsync(
                    "اعلان تستی",
                    "این قابلیت روی این دستگاه پشتیبانی نمی‌شود.",
                    "تأیید",
                    AlertType.Warning);
                return;
            }

            if (!await EnsureNotificationPermissionAsync())
                return;

            try
            {
                bool shown = await _newContentNotifier.ShowTestAsync();

                await _customAlertService.ShowAlertAsync(
                    shown ? "اعلان تستی ارسال شد" : "اعلان نمایش داده نشد",
                    shown
                        ? "اعلان نمونه در نوار اعلان‌ها نمایش داده شد. اگر آن را نمی‌بینید، سطح اهمیت کانال «مطالب و طرح‌های تازه» را در تنظیمات اعلان‌های گوشی بررسی کنید."
                        : "بررسی کنید اعلان‌های برنامه در تنظیمات سیستم فعال باشند.",
                    "تأیید",
                    shown ? AlertType.Success : AlertType.Warning);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Test notification error: {ex.Message}");
                await _customAlertService.ShowAlertAsync(
                    "خطا",
                    "ارسال اعلان تستی با خطا مواجه شد.",
                    "تأیید",
                    AlertType.Error);
            }
        }
    }
}
