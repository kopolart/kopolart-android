using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;
using KopolArt.Mobile.Views;

namespace KopolArt.Mobile.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        private readonly UpdateService _updateService;
        private readonly ICustomAlertService _customAlertService;

        private bool _isCheckingUpdate;
        public bool IsCheckingUpdate
        {
            get => _isCheckingUpdate;
            set => SetProperty(ref _isCheckingUpdate, value);
        }

        public string AppVersion => $"نسخه {DynamicBuildInfo.AppVersion}".ToPersianDigits();
        public string BuildNumber => $"بیلد {DynamicBuildInfo.BuildNumber}".ToPersianDigits();
        public string PackageName => DynamicBuildInfo.PackageName;
        public string MinSupportedAndroid => DynamicBuildInfo.MinSupportedAndroid;
        public string DeviceModel => DynamicBuildInfo.DeviceModel;
        public string OsVersion => DynamicBuildInfo.OsVersion;

        public event EventHandler? RequestShowSocialPromo;
        public event EventHandler<(UpdateInfo Info, List<UpdateRelease> Releases)>? RequestShowUpdatePopup;

        public ICommand BackCommand { get; }
        public ICommand OpenWebsiteCommand { get; }
        public ICommand OpenDonateCommand { get; }
        public ICommand OpenTermsCommand { get; }
        public ICommand OpenLicenseCommand { get; }
        public ICommand OpenCcLicenseCommand { get; }
        public ICommand OpenContactUsCommand { get; }
        public ICommand ShowSocialPromoCommand { get; }
        public ICommand OpenUpdatesHistoryCommand { get; }
        public ICommand CheckForUpdateCommand { get; }
        public ICommand CopyDiagnosticsCommand { get; }

        public AboutViewModel(UpdateService updateService, ICustomAlertService customAlertService)
        {
            _updateService = updateService;
            _customAlertService = customAlertService;

            BackCommand = new Command(async () => await NavigationHelper.GoBackSafelyAsync());
            OpenUpdatesHistoryCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(UpdatesHistoryPage)));
            ShowSocialPromoCommand = new Command(() => RequestShowSocialPromo?.Invoke(this, EventArgs.Empty));
            OpenContactUsCommand = new Command(async () => await Shell.Current.GoToAsync("ContactUsPage"));

            CheckForUpdateCommand = new Command(async () => await CheckForUpdateAsync());

            CopyDiagnosticsCommand = new Command(async () =>
            {
                try
                {
                    var diagnostics = $"کپل‌آرت ({DynamicBuildInfo.AppVersion} - بیلد {DynamicBuildInfo.BuildNumber})\nدستگاه: {DynamicBuildInfo.DeviceModel}\nسیستم‌عامل: {DynamicBuildInfo.OsVersion}\nحداقل اندروید: {DynamicBuildInfo.MinSupportedAndroid}";
                    await Clipboard.Default.SetTextAsync(diagnostics);
                    await _customAlertService.ShowAlertAsync("کپی شد", "مشخصات فنی دستگاه و برنامه در کلیپ‌بورد کپی شد. می‌توانید آن را هنگام پیام به پشتیبانی ارسال فرمایید.", "تأیید", AlertType.Success);
                }
                catch (Exception ex)
                {
                    await _customAlertService.ShowAlertAsync("خطا", $"خطا در کپی مشخصات: {ex.Message}", "تأیید", AlertType.Error);
                }
            });

            OpenWebsiteCommand = new Command(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    { "targetUrl", Constants.Urls.AboutUs }
                };
                await Shell.Current.GoToAsync("BrowserPage", parameters);
            });

            OpenDonateCommand = new Command(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    { "targetUrl", Constants.Urls.Donate }
                };
                await Shell.Current.GoToAsync("BrowserPage", parameters);
            });

            OpenTermsCommand = new Command(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    { "targetUrl", Constants.Urls.Terms }
                };
                await Shell.Current.GoToAsync("BrowserPage", parameters);
            });

            OpenLicenseCommand = new Command(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    { "targetUrl", Constants.Urls.License }
                };
                await Shell.Current.GoToAsync("BrowserPage", parameters);
            });

            OpenCcLicenseCommand = new Command(async () =>
            {
                try
                {
                    await Launcher.Default.OpenAsync(new Uri(Constants.Urls.CcByLicense));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AboutViewModel] Error opening CC license in browser: {ex.Message}");
                }
            });
        }

        private async Task CheckForUpdateAsync()
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
                    RequestShowUpdatePopup?.Invoke(this, (updateInfo, releases));
                }
                else
                {
                    await _customAlertService.ShowAlertAsync("به‌روز هستید", "شما در حال حاضر از آخرین نسخه کپل‌آرت استفاده می‌کنید.", "عالیه", AlertType.Success);
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
