using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.ViewModels;

namespace KopolArt.Mobile.Views
{
    public partial class ContactUsPage : ContentPage
    {
        public ContactUsPage(ContactUsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            viewModel.RequestShowSocialPromo += (_, _) => SocialPromo.Show();
            LandscapeSafeAreaHelper.Attach(this);
        }
    }
}
