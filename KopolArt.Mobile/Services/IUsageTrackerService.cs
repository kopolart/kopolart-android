using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IUsageTrackerService : IWelcomeTrackerService
    {
        Task InitializeAsync();
        Task<List<UsageRecord>> GetUsageSessionsAsync(string? searchQuery = null);
        Task DeleteSessionAsync(string sessionId);
        Task ClearSessionIpAsync(string sessionId);
        Task ResetAllUsageAsync();
        string CurrentSessionIp { get; }
        UsageRecord? CurrentSession { get; }
        void EndCurrentSession();
        Task ImportSessionsAsync(List<UsageRecord> incoming, long incomingTotalUsageSeconds, bool overwrite);
        void StartBrowsingTracking();
        void PauseBrowsingTracking();
        bool IsBrowsingActive { get; }
    }
}
