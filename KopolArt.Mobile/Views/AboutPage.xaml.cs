using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Services;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class AboutPage : ContentPage
    {
        private readonly UpdateService _updateService;

        public AboutPage(AboutViewModel viewModel, UpdateService updateService)
        {
            InitializeComponent();
            _updateService = updateService;
            BindingContext = viewModel;

            viewModel.RequestShowSocialPromo += (_, _) => SocialPromo.Show();
            viewModel.RequestShowUpdatePopup += OnRequestShowUpdatePopup;
            LandscapeSafeAreaHelper.Attach(this);
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
                    System.Diagnostics.Debug.WriteLine($"[AboutPage] Error showing UpdatePopup: {ex}");
                }
            });
        }
    }
}
