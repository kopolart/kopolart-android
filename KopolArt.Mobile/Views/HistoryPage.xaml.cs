using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class HistoryPage : ContentPage
    {
        private readonly HistoryViewModel _viewModel;

        public HistoryPage(HistoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            LandscapeSafeAreaHelper.Attach(this);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await _viewModel.LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryPage] Error loading history: {ex}");
            }
        }
    }
}
