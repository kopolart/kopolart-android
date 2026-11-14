using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class GesturesPage : ContentPage
    {
        public GesturesPage(GesturesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            LandscapeSafeAreaHelper.Attach(this);
        }
    }
}
