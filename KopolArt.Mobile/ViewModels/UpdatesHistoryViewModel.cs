using System.Collections.ObjectModel;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.ViewModels
{
    public class UpdatesHistoryViewModel : BaseViewModel
    {
        private readonly UpdateService _updateService;

        private bool _isLoading = true;
        private bool _hasError;
        private string _errorMessage = string.Empty;

        public ObservableCollection<UpdateReleaseDisplayItem> Releases { get; } = [];

        public string CurrentAppVersion => $"نسخه فعلی شما: {AppInfo.Current.VersionString}".ToPersianDigits();

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand BackCommand { get; }
        public ICommand RetryCommand { get; }

        public UpdatesHistoryViewModel(UpdateService updateService)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));

            BackCommand = new Command(async () => await NavigationHelper.GoBackSafelyAsync());
            RetryCommand = new Command(async () => await LoadHistoryAsync());

            _ = LoadHistoryAsync();
        }

        public async Task LoadHistoryAsync()
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                var releases = await _updateService.GetAllReleasesAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Releases.Clear();

                    if (releases == null || releases.Count == 0)
                    {
                        HasError = true;
                        ErrorMessage = "امکان بارگذاری تغییرات نسخه‌ها وجود ندارد. لطفاً اتصال اینترنت خود را بررسی کنید.";
                        IsLoading = false;
                        return;
                    }

                    string currentVersion = AppInfo.Current.VersionString;

                    foreach (var release in releases)
                    {
                        bool isCurrent = string.Equals(release.Version?.Trim(), currentVersion?.Trim(), StringComparison.OrdinalIgnoreCase);

                        string notes = release.ReleaseNotes ?? string.Empty;
                        notes = notes.Replace("\\r\\n", "\n").Replace("\\n", "\n");

                        Releases.Add(new UpdateReleaseDisplayItem
                        {
                            Version = release.Version ?? string.Empty,
                            VersionDisplay = $"نسخه {release.Version}".ToPersianDigits(),
                            Date = release.Date ?? string.Empty,
                            PersianDate = release.PersianDate,
                            ReleaseNotes = notes,
                            IsCurrentVersion = isCurrent
                        });
                    }

                    IsLoading = false;
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    HasError = true;
                    ErrorMessage = $"خطا در دریافت اطلاعات: {ex.Message}";
                    IsLoading = false;
                });
            }
        }
    }
}
