using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class BookmarksPage : ContentPage
    {
        private readonly BookmarksViewModel _viewModel;

        public BookmarksPage(BookmarksViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;

            LandscapeSafeAreaHelper.Attach(this);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadBookmarksAsync();
        }
    }
}
