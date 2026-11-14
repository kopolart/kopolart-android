namespace KopolArt.Mobile.Services
{
    public interface INotificationPermissionService
    {
        Task<bool> IsNotificationPermissionGrantedAsync();
        Task<bool> RequestNotificationPermissionAsync();
    }
}
