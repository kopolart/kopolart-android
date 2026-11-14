using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IUsageAnalyticsService
    {
        Task<UsageAnalyticsSummary> BuildAnalyticsAsync(int daysCount);
    }
}
