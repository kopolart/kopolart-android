namespace KopolArt.Mobile.Models
{
    public class UsageBackupData
    {
        public long TotalUsageSeconds { get; set; }
        public List<UsageRecord>? Sessions { get; set; }
        public List<string>? AwardedMilestones { get; set; }
    }
}
