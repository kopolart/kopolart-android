using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class OnboardingPage : ContentPage
    {
        private readonly OnboardingViewModel _viewModel;

        public OnboardingPage(OnboardingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
            LandscapeSafeAreaHelper.Attach(this);
        }

        protected override bool OnBackButtonPressed()
        {
            if (_viewModel.CurrentPosition > 0)
            {
                _viewModel.CurrentPosition--;
                return true; // Handled internally
            }

            // Allow normal system back button behavior on first slide
            return base.OnBackButtonPressed();
        }
    }
}
