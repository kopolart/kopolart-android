using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class DownloadHistoryPage : ContentPage
    {
        private readonly DownloadHistoryViewModel _viewModel;

        public DownloadHistoryPage(DownloadHistoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            LandscapeSafeAreaHelper.Attach(this);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = _viewModel.LoadDownloadsAsync();
        }
    }
}
