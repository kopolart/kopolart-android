using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.ViewModels
{
    public partial class SettingsViewModel
    {
        public async Task CheckUpdateSilentlyAsync()
        {
            try
            {
                var updateInfo = await _updateService.FetchUpdateInfoAsync();
                if (updateInfo != null)
                {
                    var releases = _updateService.GetNewReleases(updateInfo, AppInfo.Current.VersionString);
                    if (releases.Count > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            HasUpdateBadge = true;
                            NewVersionNumber = "آپدیت جدید";
                            UpdateStatusText = $"نسخه جدید ({releases[0].Version}) در دسترس است".ToPersianDigits();
                        });
                        return;
                    }
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HasUpdateBadge = false;
                    NewVersionNumber = string.Empty;
                    UpdateStatusText = "برنامه شما به‌روز است";
                });
            }
            catch
            {
                // Silent check failure
            }
        }

        public async Task CheckUpdateAsync()
        {
            if (IsCheckingUpdate) return;
            IsCheckingUpdate = true;

            try
            {
                var updateInfo = await _updateService.FetchUpdateInfoAsync();
                if (updateInfo == null)
                {
                    await _customAlertService.ShowAlertAsync("خطا", "ارتباط با سرور به‌روزرسانی برقرار نشد. لطفاً اتصال اینترنت خود را بررسی کنید.", "تأیید", AlertType.Error);
                    return;
                }

                var releases = _updateService.GetNewReleases(updateInfo, AppInfo.Current.VersionString);
                if (releases.Count > 0)
                {
                    HasUpdateBadge = true;
                    NewVersionNumber = "آپدیت جدید";
                    UpdateStatusText = $"نسخه جدید ({releases[0].Version}) در دسترس است".ToPersianDigits();
                    RequestShowUpdatePopup?.Invoke(this, (updateInfo, releases));
                }
                else
                {
                    HasUpdateBadge = false;
                    NewVersionNumber = string.Empty;
                    UpdateStatusText = "برنامه شما به‌روز است";
                    await _customAlertService.ShowAlertAsync("به‌روز هستید", "شما در حال استفاده از جدیدترین نسخه کپل‌آرت هستید.", "عالیه", AlertType.Success);
                }
            }
            catch (Exception ex)
            {
                await _customAlertService.ShowAlertAsync("خطا", $"خطا در بررسی به‌روزرسانی: {ex.Message}", "تأیید", AlertType.Error);
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        }

    }
}
