using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Services;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _viewModel;
        private readonly UpdateService _updateService;

        public SettingsPage(SettingsViewModel viewModel, UpdateService updateService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _updateService = updateService;
            BindingContext = _viewModel;

            LandscapeSafeAreaHelper.Attach(this);

            _viewModel.RequestShowUpdatePopup += OnRequestShowUpdatePopup;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.RefreshStorageMetrics();
            _ = _viewModel.CheckUpdateSilentlyAsync();

            if (_viewModel.HasUpdateBadge)
            {
                StartGlowAnimation();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.HasUpdateBadge))
            {
                if (_viewModel.HasUpdateBadge)
                    StartGlowAnimation();
                else
                    StopGlowAnimation();
            }
        }

        private void StartGlowAnimation()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (GlowingBadge == null) return;
                GlowingBadge.CancelAnimations();

                var animation = new Animation();
                animation.Add(0, 0.5, new Animation(v => GlowingBadge.Opacity = v, 1.0, 0.35, Easing.CubicInOut));
                animation.Add(0.5, 1.0, new Animation(v => GlowingBadge.Opacity = v, 0.35, 1.0, Easing.CubicInOut));

                animation.Commit(GlowingBadge, "GlowAnim", length: 1400, repeat: () => true);
            });
        }

        private void StopGlowAnimation()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (GlowingBadge == null) return;
                GlowingBadge.CancelAnimations();
                GlowingBadge.Opacity = 1.0;
            });
        }

        private void OnRequestShowUpdatePopup(object? sender, (Models.UpdateInfo Info, List<Models.UpdateRelease> Releases) e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    UpdatePopupView.Show(_updateService, e.Info, e.Releases);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsPage] Show update popup error: {ex.Message}");
                }
            });
        }
    }
}
