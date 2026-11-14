namespace KopolArt.Mobile.Services
{
    public interface IOnlineUsersService
    {
        Task<int?> GetOnlineUsersCountAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    }
}
