using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class UpdatesHistoryPage : ContentPage
    {
        public UpdatesHistoryPage(UpdatesHistoryViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            LandscapeSafeAreaHelper.Attach(this);
        }
    }
}
