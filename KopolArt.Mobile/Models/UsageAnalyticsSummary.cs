namespace KopolArt.Mobile.Models
{
    public class UsageAnalyticsSummary
    {
        public List<UsageAnalyticsBarItem> Bars { get; set; } = [];
        public UsageAnalyticsBarItem? PeakBar { get; set; }
        public long TotalDurationSeconds { get; set; }
        public string FormattedTotalDuration { get; set; } = "۰ ثانیه";
        public long AverageDurationSeconds { get; set; }
        public string FormattedAverageDuration { get; set; } = "۰ ثانیه";
        public long TotalDataBytes { get; set; }
        public string FormattedTotalData { get; set; } = "۰ بایت";
        public int ActiveDaysCount { get; set; }
        public string SmartInsight { get; set; } = string.Empty;
        public bool IsEmpty { get; set; }
    }
}
