using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class OfflinePagesPage : ContentPage
    {
        private readonly OfflinePagesViewModel _viewModel;

        public OfflinePagesPage(OfflinePagesViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
            LandscapeSafeAreaHelper.Attach(this);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadPagesAsync();
        }
    }
}
