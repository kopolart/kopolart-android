using KopolArt.Mobile.Helpers;
using System.Windows.Input;

namespace KopolArt.Mobile.ViewModels
{
    public class GesturesViewModel : BaseViewModel
    {
        public ICommand BackCommand { get; }

        public GesturesViewModel()
        {
            BackCommand = new Command(async () => await NavigationHelper.GoBackSafelyAsync());
        }
    }
}
