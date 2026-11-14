using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

using KopolArt.Mobile.Helpers;

namespace KopolArt.Mobile.ViewModels
{
    public class OnboardingViewModel : BaseViewModel
    {
        private readonly INotificationPermissionService _notificationPermissionService;
        private readonly ICustomAlertService _customAlertService;
        private readonly ISettingsService _settingsService;

        private int _currentPosition;
        private bool _isFirstSlide = true;
        private bool _isLastSlide;
        private string _nextButtonText = "بعدی";
        private bool _isNotificationPermissionGranted;
        private string _notificationButtonText = "فعال‌سازی اعلان‌ها";
        private string _notificationStatusMessage = "با فعال‌سازی این مجوز، اعلان دانلودها، آثار جدید و بروزرسانی‌ها برای شما ارسال خواهد شد.";
        private bool _isRequestingPermission;

        public ObservableCollection<OnboardingSlideModel> Slides { get; } = [];

        public int CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (SetProperty(ref _currentPosition, value))
                {
                    UpdateNavigationState();
                }
            }
        }

        public bool IsFirstSlide
        {
            get => _isFirstSlide;
            private set
            {
                if (SetProperty(ref _isFirstSlide, value))
                {
                    OnPropertyChanged(nameof(CanGoBack));
                }
            }
        }

        public bool CanGoBack => !IsFirstSlide;

        public bool IsLastSlide
        {
            get => _isLastSlide;
            private set => SetProperty(ref _isLastSlide, value);
        }

        public string NextButtonText
        {
            get => _nextButtonText;
            private set => SetProperty(ref _nextButtonText, value);
        }

        public bool IsNotificationPermissionGranted
        {
            get => _isNotificationPermissionGranted;
            private set => SetProperty(ref _isNotificationPermissionGranted, value);
        }

        public string NotificationButtonText
        {
            get => _notificationButtonText;
            private set => SetProperty(ref _notificationButtonText, value);
        }

        public string NotificationStatusMessage
        {
            get => _notificationStatusMessage;
            private set => SetProperty(ref _notificationStatusMessage, value);
        }

        public bool IsRequestingPermission
        {
            get => _isRequestingPermission;
            private set => SetProperty(ref _isRequestingPermission, value);
        }

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand SkipCommand { get; }
        public ICommand RequestNotificationPermissionCommand { get; }
        public ICommand FinishOnboardingCommand { get; }

        public event EventHandler? OnboardingCompleted;

        public OnboardingViewModel(
            INotificationPermissionService notificationPermissionService,
            ICustomAlertService customAlertService,
            ISettingsService settingsService)
        {
            _notificationPermissionService = notificationPermissionService;
            _customAlertService = customAlertService;
            _settingsService = settingsService;

            InitializeSlides();

            NextCommand = new Command(async () => await HandleNextAsync());
            PreviousCommand = new Command(HandlePrevious);
            SkipCommand = new Command(async () => await FinishOnboardingAsync());
            RequestNotificationPermissionCommand = new Command(async () => await RequestNotificationPermissionAsync());
            FinishOnboardingCommand = new Command(async () => await FinishOnboardingAsync());

            _ = CheckInitialPermissionStatusAsync();
        }

        private void InitializeSlides()
        {
            Slides.Clear();

            Slides.Add(new OnboardingSlideModel
            {
                SlideIndex = 0,
                BadgeText = "کپل‌آرت؛ مرجع هنر و طراحی",
                Title = "دنیای بی‌پایان خلاقیت و گرافیک",
                Subtitle = "به اپلیکیشن رسمی کپل‌آرت خوش آمدید",
                Description = "دسترسی مستقیم و پرسرعت به صدها اثر گرافیکی برتر، فایل‌های اختصاصی، وکتورها و ابزارهای تخصصی طراحی در محیطی زیبا، یکپارچه و مدرن.",
                IconGlyph = KopolArtFaIcons.Globe,

                IsNotificationSlide = false
            });

            Slides.Add(new OnboardingSlideModel
            {
                SlideIndex = 1,
                BadgeText = "سرعت بالا و دانلود منیجر",
                Title = "کاوش پرسرعت و دانلود هوشمند",
                Subtitle = "مرور وبسایت بدون افت سرعت",
                Description = "آثار دلخواه خود را به لیست نشان‌ها (Bookmarks) بیفزایید، دانلودها را با دانلود منیجر اختصاصی با نهایت سرعت دریافت کنید و سوابق را مدیریت نمایید.",
                IconGlyph = KopolArtFaIcons.DownloadCloud,

                IsNotificationSlide = false
            });

            Slides.Add(new OnboardingSlideModel
            {
                SlideIndex = 2,
                BadgeText = "شخصی‌سازی و ژست‌ها",
                Title = "ژست‌های حرکتی و تم لوکس",
                Subtitle = "تجربه‌ای کاملاً سازگار با سلیقه شما",
                Description = "با کشیدن انگشت یا دابل تپ میان صفحات به سرعت جابجا شوید، آمار مصرف اینترنت را بررسی کنید و با حالت تاریک اختصاصی از چشمان خود در شب مراقبت کنید.",
                IconGlyph = KopolArtFaIcons.SwipeLeftHand,

                IsNotificationSlide = false
            });

            Slides.Add(new OnboardingSlideModel
            {
                SlideIndex = 3,
                BadgeText = "اطلاع‌رسانی لحظه‌ای",
                Title = "از هیچ فرصت و پیامی جا نمانید!",
                Subtitle = "فعال‌سازی دریافت اعلان‌های کپل‌آرت",
                Description = "برای آگاهی فوری از انتشار جدیدترین طرح‌ها، پیام‌های مهم سامانه و همچنین پیگیری آسان وضعیت دانلودها، دسترسی اعلان را فعال کنید.",
                IconGlyph = KopolArtFaIcons.Notification,

                IsNotificationSlide = true
            });

            UpdateNavigationState();
        }


        private async Task CheckInitialPermissionStatusAsync()
        {
            try
            {
                var granted = await _notificationPermissionService.IsNotificationPermissionGrantedAsync();
                UpdatePermissionState(granted);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnboardingViewModel] CheckPermission error: {ex.Message}");
            }
        }

        public async Task RequestNotificationPermissionAsync()
        {
            if (IsRequestingPermission) return;

            try
            {
                IsRequestingPermission = true;
                var granted = await _notificationPermissionService.RequestNotificationPermissionAsync();
                UpdatePermissionState(granted);

                NotificationStatusMessage = granted ? "دسترسی با موفقیت تایید شد. از این پس اعلان‌های مهم را دریافت خواهید کرد." : "مجوز اعلان داده نشد. در صورت تمایل می‌توانید بعداً از بخش تنظیمات گوشی آن را فعال نمایید.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnboardingViewModel] RequestPermission error: {ex.Message}");
            }
            finally
            {
                IsRequestingPermission = false;
            }
        }

        private void UpdatePermissionState(bool granted)
        {
            IsNotificationPermissionGranted = granted;
            if (granted)
            {
                NotificationButtonText = "مجوز اعلان‌ها فعال است";
                NotificationStatusMessage = "اعلان‌ها فعال هستند و پیام‌های مهم را دریافت خواهید کرد.";
            }
            else
            {
                NotificationButtonText = "فعال‌سازی اعلان‌ها";
            }
        }

        private async Task HandleNextAsync()
        {
            if (CurrentPosition < Slides.Count - 1)
            {
                CurrentPosition++;
            }
            else
            {
                await FinishOnboardingAsync();
            }
        }

        private void HandlePrevious()
        {
            if (CurrentPosition > 0)
            {
                CurrentPosition--;
            }
        }

        public async Task FinishOnboardingAsync()
        {
            try
            {
                Preferences.Set(Constants.Keys.HasCompletedOnboarding, true);
                _settingsService.Settings.HasCompletedOnboarding = true;
                await _settingsService.SaveSettingsAsync(_settingsService.Settings);

                OnboardingCompleted?.Invoke(this, EventArgs.Empty);

                // If currently hosted in a Shell route (e.g. opened from Settings), navigate back
                if (Shell.Current != null && Shell.Current.CurrentPage is Views.OnboardingPage)
                {
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                // If launched at first start as root window page, transition window to AppShell
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (Application.Current != null && Application.Current.Windows.Count > 0)
                    {
                        Application.Current.Windows[0].Page = new AppShell();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnboardingViewModel] FinishOnboarding error: {ex.Message}");
            }
        }

        private void UpdateNavigationState()
        {
            IsFirstSlide = CurrentPosition == 0;
            IsLastSlide = CurrentPosition >= Slides.Count - 1;
            NextButtonText = IsLastSlide ? "شروع و ورود" : "بعدی";
        }
    }
}
