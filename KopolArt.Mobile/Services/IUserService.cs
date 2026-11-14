using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IUserService
    {
        KopolUserInfo? GetCurrentUser();
        bool IsLoggedIn { get; }
    }
}
