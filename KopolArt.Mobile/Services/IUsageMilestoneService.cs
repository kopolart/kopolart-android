namespace KopolArt.Mobile.Services
{
    public interface IUsageMilestoneService
    {
        Task CheckMilestonesAsync(long totalUsageSeconds);
        List<string> GetAwardedMilestones();
        void SetAwardedMilestones(IEnumerable<string> milestoneIds);
        void ResetMilestones();
    }
}
