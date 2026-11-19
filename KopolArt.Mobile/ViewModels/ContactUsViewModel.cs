using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public class ContactUsViewModel : BaseViewModel
    {
        private readonly ICustomAlertService _customAlertService;

        public const string PhoneNumber = "09135942903";
        public const string PrimaryEmail = "info@kopolart.ir";
        public const string ContactFormUrl = "https://kopolart.ir/contact-us";

        public event EventHandler? RequestShowSocialPromo;

        public ICommand CallPhoneCommand { get; }
        public ICommand SendEmailCommand { get; }
        public ICommand OpenContactFormCommand { get; }
        public ICommand OpenFaqCommand { get; }
        public ICommand OpenGithubCommand { get; }
        public ICommand ShowSocialPromoCommand { get; }
        public ICommand BackCommand { get; }

        public ContactUsViewModel(ICustomAlertService customAlertService)
        {
            _customAlertService = customAlertService;
            CallPhoneCommand = new Command(OnCallPhone);
            SendEmailCommand = new Command<string>(OnSendEmail);
            ShowSocialPromoCommand = new Command(() => RequestShowSocialPromo?.Invoke(this, EventArgs.Empty));
            OpenContactFormCommand = new Command(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    { "targetUrl", ContactFormUrl }
                };
                await Shell.Current.GoToAsync("BrowserPage", parameters);
            });
            OpenFaqCommand = new Command(async () =>
            {
                var parameters = new Dictionary<string, object>
                {
                    { "targetUrl", Constants.Urls.Faq }
                };
                await Shell.Current.GoToAsync("BrowserPage", parameters);
            });
            OpenGithubCommand = new Command(async () =>
                await NavigationHelper.OpenExternalAsync(Constants.Urls.Github));
            BackCommand = new Command(async () => await NavigationHelper.GoBackSafelyAsync());
        }

        private async void OnCallPhone()
        {
            try
            {
                if (PhoneDialer.Default.IsSupported)
                {
                    PhoneDialer.Default.Open(PhoneNumber);
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContactUsViewModel] Dialer error: {ex.Message}");
            }

            try
            {
                var uri = new Uri($"tel:{PhoneNumber}");
                if (await Launcher.Default.OpenAsync(uri))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContactUsViewModel] Launcher dialer error: {ex.Message}");
            }

            try
            {
                await Clipboard.Default.SetTextAsync(PhoneNumber);
                await _customAlertService.ShowAlertAsync(
                    "تماس تلفنی",
                    $"شماره تماس ({PhoneNumber}) در کلیپ‌بورد کپی شد.",
                    "تأیید",
                    AlertType.Info);
            }
            catch { }
        }

        private async void OnSendEmail(string? email)
        {
            var targetEmail = string.IsNullOrWhiteSpace(email) ? PrimaryEmail : email;
            bool emailOpened = false;

            try
            {
                if (Email.Default.IsComposeSupported)
                {
                    await Email.Default.ComposeAsync(new EmailMessage
                    {
                        Subject = "پیام از طریق اپلیکیشن کپل‌آرت",
                        To = [targetEmail]
                    });
                    emailOpened = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContactUsViewModel] Email compose error: {ex.Message}");
            }

            if (!emailOpened)
            {
                try
                {
                    var uri = new Uri($"mailto:{targetEmail}?subject={Uri.EscapeDataString("پیام از طریق اپلیکیشن کپل‌آرت")}");
                    emailOpened = await Launcher.Default.OpenAsync(uri);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ContactUsViewModel] Launcher mailto error: {ex.Message}");
                }
            }

            if (!emailOpened)
            {
                try
                {
                    await Clipboard.Default.SetTextAsync(targetEmail);
                    await _customAlertService.ShowAlertAsync(
                        "ارسال ایمیل",
                        $"برنامه‌ای برای ارسال مستقیم ایمیل در دستگاه شما یافت نشد. آدرس ایمیل ({targetEmail}) در کلیپ‌بورد کپی شد.",
                        "متوجه شدم",
                        AlertType.Info);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ContactUsViewModel] Clipboard fallback error: {ex.Message}");
                }
            }
        }
    }
}
